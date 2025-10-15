using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PepperDash.Core;

namespace PepperDash.Essentials.Plugins.Birddog.Camera
{
    /// <summary>
    /// Represents a queued request with callback handling
    /// </summary>
    public class QueuedRequest
    {
        public BirddogApiCommand Command { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RetryCount { get; set; }
        public Action<string> OnSuccess { get; set; }
        public Action<string> OnError { get; set; }
        public string RequestId { get; set; }

        public QueuedRequest(BirddogApiCommand command, Action<string> onSuccess = null, Action<string> onError = null)
        {
            Command = command;
            CreatedAt = DateTime.Now;
            RetryCount = 0;
            OnSuccess = onSuccess;
            OnError = onError;
            RequestId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Represents a response from the camera
    /// </summary>
    public class CameraResponse
    {
        public string RequestId { get; set; }
        public string Content { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ResponseTime { get; set; }
        public QueuedRequest OriginalRequest { get; set; }
    }

    /// <summary>
    /// Manages command queues for sending requests and processing responses
    /// </summary>
    public class CommandQueueManager : IDisposable
    {
        private readonly ConcurrentQueue<QueuedRequest> _requestQueue;
        private readonly ConcurrentQueue<CameraResponse> _responseQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _queueLock = new object();
        private readonly string _deviceKey;
        private readonly Func<string> _getBaseUri;
        private readonly HttpClient _httpClient;

        private Task _requestProcessorTask;
        private Task _responseProcessorTask;
        private bool _isProcessingEnabled;
        private bool _disposed = false;

        // Configuration constants
        private const int MaxRetryCount = 3;
        private const int RequestTimeoutMs = 2000;
        private const int ProcessingDelayMs = 50;
        private const int RetryDelayMs = 1000;
        private const int ErrorDelayMs = 1000;

        // Events for external handling
        public event EventHandler<CameraResponse> ResponseReceived;
        public event EventHandler<QueuedRequest> RequestFailed;
        public event EventHandler<(int RequestCount, int ResponseCount)> QueueStatusChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deviceKey">Device identifier for logging</param>
        /// <param name="getBaseUri">Function to get the base URI for requests</param>
        /// <param name="httpClient">HTTP client to use for requests</param>
        public CommandQueueManager(string deviceKey, Func<string> getBaseUri, HttpClient httpClient = null)
        {
            _deviceKey = deviceKey ?? throw new ArgumentNullException(nameof(deviceKey));
            _getBaseUri = getBaseUri ?? throw new ArgumentNullException(nameof(getBaseUri));
            
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMilliseconds(RequestTimeoutMs) };
            
            _requestQueue = new ConcurrentQueue<QueuedRequest>();
            _responseQueue = new ConcurrentQueue<CameraResponse>();
            _cancellationTokenSource = new CancellationTokenSource();
            _isProcessingEnabled = false;
        }

        /// <summary>
        /// Starts the queue processing tasks
        /// </summary>
        public void StartProcessing()
        {
            if (_isProcessingEnabled || _disposed) return;

            _isProcessingEnabled = true;
            
            _requestProcessorTask = Task.Run(async () => await ProcessRequestQueue(_cancellationTokenSource.Token));
            _responseProcessorTask = Task.Run(async () => await ProcessResponseQueue(_cancellationTokenSource.Token));

            Debug.LogInformation("CommandQueueManager", "Queue processing started for device {0}", _deviceKey);
        }

        /// <summary>
        /// Stops the queue processing tasks
        /// </summary>
        public void StopProcessing()
        {
            if (!_isProcessingEnabled || _disposed) return;

            _isProcessingEnabled = false;
            _cancellationTokenSource.Cancel();

            try
            {
                var tasks = new[] { _requestProcessorTask, _responseProcessorTask };
                Task.WaitAll(tasks.Where(t => t != null).ToArray(), TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Debug.LogError("CommandQueueManager", "Error stopping queue processing for device {0}: {1}", _deviceKey, ex.Message);
            }

            Debug.LogInformation("CommandQueueManager", "Queue processing stopped for device {0}", _deviceKey);
        }

        /// <summary>
        /// Enqueues a command for processing
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <param name="onSuccess">Callback for successful response</param>
        /// <param name="onError">Callback for error response</param>
        public void EnqueueRequest(BirddogApiCommand command, Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (_disposed)
            {
                Debug.LogWarning("CommandQueueManager", "Cannot enqueue request - queue manager is disposed");
                return;
            }

            var queuedRequest = new QueuedRequest(command, onSuccess, onError);
            _requestQueue.Enqueue(queuedRequest);
            
            Debug.LogVerbose("CommandQueueManager", "Request {0} enqueued for device {1}: {2} {3}", 
                queuedRequest.RequestId, _deviceKey, command.Method, command.Path);

            NotifyQueueStatusChanged();
        }

        /// <summary>
        /// Sends a high-priority request that bypasses the queue
        /// </summary>
        /// <param name="command">The command to send immediately</param>
        /// <param name="onSuccess">Optional callback for successful response</param>
        /// <param name="onError">Optional callback for error response</param>
        public async Task SendHighPriorityRequest(BirddogApiCommand command, Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (_disposed)
            {
                Debug.LogWarning("CommandQueueManager", "Cannot send high priority request - queue manager is disposed");
                return;
            }

            var queuedRequest = new QueuedRequest(command, onSuccess, onError);
            await ProcessSingleRequest(queuedRequest);
        }

        /// <summary>
        /// Gets the current queue status
        /// </summary>
        public (int RequestCount, int ResponseCount) GetQueueStatus()
        {
            return (_requestQueue.Count, _responseQueue.Count);
        }

        /// <summary>
        /// Clears all pending requests from the queue
        /// </summary>
        public void ClearRequestQueue()
        {
            if (_disposed) return;

            lock (_queueLock)
            {
                var count = 0;
                while (_requestQueue.TryDequeue(out _)) { count++; }
                Debug.LogInformation("CommandQueueManager", "Cleared {0} requests from queue for device {1}", count, _deviceKey);
                NotifyQueueStatusChanged();
            }
        }

        /// <summary>
        /// Clears all pending responses from the queue
        /// </summary>
        public void ClearResponseQueue()
        {
            if (_disposed) return;

            lock (_queueLock)
            {
                var count = 0;
                while (_responseQueue.TryDequeue(out _)) { count++; }
                Debug.LogInformation("CommandQueueManager", "Cleared {0} responses from queue for device {1}", count, _deviceKey);
                NotifyQueueStatusChanged();
            }
        }

        /// <summary>
        /// Gets whether queue processing is currently enabled
        /// </summary>
        public bool IsProcessingEnabled => _isProcessingEnabled && !_disposed;

        /// <summary>
        /// Logs current queue status for debugging
        /// </summary>
        public void LogQueueStatus()
        {
            var (requestCount, responseCount) = GetQueueStatus();
            Debug.LogInformation("CommandQueueManager", "Queue Status for device {0} - Requests: {1}, Responses: {2}, Processing: {3}", 
                _deviceKey, requestCount, responseCount, _isProcessingEnabled);
        }

        #region Private Methods

        /// <summary>
        /// Processes the request queue continuously
        /// </summary>
        private async Task ProcessRequestQueue(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_requestQueue.TryDequeue(out var queuedRequest))
                    {
                        await ProcessSingleRequest(queuedRequest);
                        NotifyQueueStatusChanged();
                    }
                    else
                    {
                        // Wait briefly if no requests in queue
                        await Task.Delay(ProcessingDelayMs, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("CommandQueueManager", "Error in request queue processor for device {0}: {1}", _deviceKey, ex.Message);
                    await Task.Delay(ErrorDelayMs, cancellationToken); // Wait before retrying
                }
            }
        }

        /// <summary>
        /// Processes the response queue continuously
        /// </summary>
        private async Task ProcessResponseQueue(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_responseQueue.TryDequeue(out var response))
                    {
                        ProcessSingleResponse(response);
                        NotifyQueueStatusChanged();
                    }
                    else
                    {
                        // Wait briefly if no responses in queue
                        await Task.Delay(ProcessingDelayMs, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("CommandQueueManager", "Error in response queue processor for device {0}: {1}", _deviceKey, ex.Message);
                    await Task.Delay(ErrorDelayMs, cancellationToken); // Wait before retrying
                }
            }
        }

        /// <summary>
        /// Processes a single request from the queue
        /// </summary>
        private async Task ProcessSingleRequest(QueuedRequest queuedRequest)
        {
            try
            {
                var baseUri = _getBaseUri();
                if (string.IsNullOrEmpty(baseUri))
                {
                    throw new InvalidOperationException("Base URI is not available");
                }

                var requestUri = new Uri(new Uri(baseUri), queuedRequest.Command.Path);

                using (var request = new HttpRequestMessage())
                {
                    request.RequestUri = requestUri;
                    request.Method = queuedRequest.Command.Method.ToUpper() == "POST" ? HttpMethod.Post : HttpMethod.Get;

                    // Add headers to the request
                    foreach (var header in queuedRequest.Command.Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

                    // Add content for POST requests
                    if (queuedRequest.Command.Method.ToUpper() == "POST")
                    {
                        request.Content = !string.IsNullOrEmpty(queuedRequest.Command.Body)
                            ? new StringContent(queuedRequest.Command.Body, System.Text.Encoding.UTF8, "application/json")
                            : new StringContent("");
                    }

                    var response = await _httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    var cameraResponse = new CameraResponse
                    {
                        RequestId = queuedRequest.RequestId,
                        Content = responseContent,
                        IsSuccess = response.IsSuccessStatusCode,
                        ErrorMessage = response.IsSuccessStatusCode ? null : $"{response.StatusCode} - {response.ReasonPhrase}",
                        ResponseTime = DateTime.Now,
                        OriginalRequest = queuedRequest
                    };

                    // Enqueue response for processing
                    _responseQueue.Enqueue(cameraResponse);

                    Debug.LogVerbose("CommandQueueManager", "Request {0} completed for device {1} with status: {2}", 
                        queuedRequest.RequestId, _deviceKey, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                await HandleRequestError(queuedRequest, ex);
            }
        }

        /// <summary>
        /// Handles request errors with retry logic
        /// </summary>
        private async Task HandleRequestError(QueuedRequest queuedRequest, Exception ex)
        {
            queuedRequest.RetryCount++;
            
            if (queuedRequest.RetryCount < MaxRetryCount)
            {
                // Re-queue for retry after a delay
                Debug.LogWarning("CommandQueueManager", "Request {0} failed for device {1}, retrying ({2}/{3}): {4}", 
                    queuedRequest.RequestId, _deviceKey, queuedRequest.RetryCount, MaxRetryCount, ex.Message);
                
                await Task.Delay(RetryDelayMs);
                _requestQueue.Enqueue(queuedRequest);
            }
            else
            {
                // Max retries reached, send error response
                var errorResponse = new CameraResponse
                {
                    RequestId = queuedRequest.RequestId,
                    Content = null,
                    IsSuccess = false,
                    ErrorMessage = $"Max retries reached: {ex.Message}",
                    ResponseTime = DateTime.Now,
                    OriginalRequest = queuedRequest
                };

                _responseQueue.Enqueue(errorResponse);
                RequestFailed?.Invoke(this, queuedRequest);
                
                Debug.LogError("CommandQueueManager", "Request {0} failed for device {1} after {2} retries: {3}", 
                    queuedRequest.RequestId, _deviceKey, MaxRetryCount, ex.Message);
            }
        }

        /// <summary>
        /// Processes a single response from the queue
        /// </summary>
        private void ProcessSingleResponse(CameraResponse response)
        {
            try
            {
                // Execute callbacks if available
                if (response.IsSuccess)
                {
                    Debug.LogVerbose("CommandQueueManager", "Response received for device {0} request {1}: {2}", 
                        _deviceKey, response.RequestId, response.Content);
                    
                    response.OriginalRequest?.OnSuccess?.Invoke(response.Content);
                }
                else
                {
                    Debug.LogError("CommandQueueManager", "Error response for device {0} request {1}: {2}", 
                        _deviceKey, response.RequestId, response.ErrorMessage);
                    
                    response.OriginalRequest?.OnError?.Invoke(response.ErrorMessage);
                }

                // Notify external handlers
                ResponseReceived?.Invoke(this, response);
            }
            catch (Exception ex)
            {
                Debug.LogError("CommandQueueManager", "Error processing response {0} for device {1}: {2}", 
                    response.RequestId, _deviceKey, ex.Message);
            }
        }

        /// <summary>
        /// Notifies subscribers of queue status changes
        /// </summary>
        private void NotifyQueueStatusChanged()
        {
            try
            {
                var status = GetQueueStatus();
                QueueStatusChanged?.Invoke(this, status);
            }
            catch (Exception ex)
            {
                Debug.LogError("CommandQueueManager", "Error notifying queue status change for device {0}: {1}", _deviceKey, ex.Message);
            }
        }

        #endregion

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop queue processing
                    StopProcessing();
                    
                    // Dispose cancellation token source
                    _cancellationTokenSource?.Dispose();
                    
                    // Clear queues
                    ClearRequestQueue();
                    ClearResponseQueue();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}