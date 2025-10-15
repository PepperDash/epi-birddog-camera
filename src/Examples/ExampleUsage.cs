using System;
using System.Net.Http;
using System.Threading.Tasks;
using PepperDash.Core;

namespace PepperDash.Essentials.Plugins.Birddog.Camera.Examples
{
    /// <summary>
    /// Example demonstrating how to use the CommandQueueManager in other contexts
    /// </summary>
    public class ExampleUsage
    {
        private CommandQueueManager _queueManager;

        public void BasicUsageExample()
        {
            // Initialize the queue manager
            _queueManager = new CommandQueueManager(
                deviceKey: "example-device",
                getBaseUri: () => "http://192.168.1.100:8080",
                httpClient: new HttpClient { Timeout = TimeSpan.FromSeconds(5) }
            );

            // Subscribe to events
            _queueManager.ResponseReceived += OnResponseReceived;
            _queueManager.RequestFailed += OnRequestFailed;
            _queueManager.QueueStatusChanged += OnQueueStatusChanged;

            // Start processing
            _queueManager.StartProcessing();

            // Example commands
            SendExampleCommands();
        }

        private void SendExampleCommands()
        {
            // Basic command without callbacks
            var getInfoCommand = new BirddogApiCommand("about", "GET");
            _queueManager.EnqueueRequest(getInfoCommand);

            // Command with success callback
            var getPtzCommand = new BirddogApiCommand("birddogptzsetup", "GET");
            _queueManager.EnqueueRequest(getPtzCommand,
                onSuccess: (response) => {
                    Debug.LogInformation("ExampleUsage", "PTZ info received: {0}", response);
                },
                onError: (error) => {
                    Debug.LogError("ExampleUsage", "Failed to get PTZ info: {0}", error);
                });

            // POST command with body
            var setPtzCommand = new BirddogApiCommand("birddogptzsetup", "POST", 
                "{\"panSpeed\":10,\"tiltSpeed\":8,\"zoomSpeed\":5}");
            _queueManager.EnqueueRequest(setPtzCommand,
                onSuccess: (response) => {
                    Debug.LogInformation("ExampleUsage", "PTZ settings updated successfully");
                });
        }

        private async Task HighPriorityExample()
        {
            // Send a high-priority command that bypasses the queue
            var emergencyCommand = new BirddogApiCommand("reboot", "POST");
            await _queueManager.SendHighPriorityRequest(emergencyCommand,
                onSuccess: (response) => {
                    Debug.LogInformation("ExampleUsage", "Emergency reboot initiated");
                },
                onError: (error) => {
                    Debug.LogError("ExampleUsage", "Emergency reboot failed: {0}", error);
                });
        }

        private void MonitoringExample()
        {
            // Check queue status
            var (requestCount, responseCount) = _queueManager.GetQueueStatus();
            Debug.LogInformation("ExampleUsage", "Queue Status - Requests: {0}, Responses: {1}", 
                requestCount, responseCount);

            // Log detailed status
            _queueManager.LogQueueStatus();

            // Clear queues if needed
            if (requestCount > 100)
            {
                _queueManager.ClearRequestQueue();
                Debug.LogWarning("ExampleUsage", "Request queue cleared due to high count");
            }
        }

        #region Event Handlers

        private void OnResponseReceived(object sender, CameraResponse response)
        {
            Debug.LogVerbose("ExampleUsage", "Response received: {0}", response.RequestId);
            
            // Custom response processing logic here
            if (response.IsSuccess)
            {
                ProcessSuccessfulResponse(response);
            }
            else
            {
                ProcessErrorResponse(response);
            }
        }

        private void OnRequestFailed(object sender, QueuedRequest failedRequest)
        {
            Debug.LogError("ExampleUsage", "Request failed after retries: {0}", failedRequest.RequestId);
            
            // Handle failed requests - perhaps update device status
            HandleFailedRequest(failedRequest);
        }

        private void OnQueueStatusChanged(object sender, (int RequestCount, int ResponseCount) status)
        {
            // Monitor queue health
            if (status.RequestCount > 50)
            {
                Debug.LogWarning("ExampleUsage", "High request queue count: {0}", status.RequestCount);
            }
        }

        #endregion

        #region Response Processing

        private void ProcessSuccessfulResponse(CameraResponse response)
        {
            try
            {
                // Parse response based on the original command
                var command = response.OriginalRequest?.Command;
                if (command == null) return;

                switch (command.Path.ToLower())
                {
                    case "about":
                        ParseDeviceInfo(response.Content);
                        break;
                    case "birddogptzsetup":
                        ParsePtzSettings(response.Content);
                        break;
                    default:
                        Debug.LogVerbose("ExampleUsage", "Unhandled response for path: {0}", command.Path);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("ExampleUsage", "Error processing response: {0}", ex.Message);
            }
        }

        private void ProcessErrorResponse(CameraResponse response)
        {
            Debug.LogError("ExampleUsage", "Error response: {0}", response.ErrorMessage);
            
            // Handle specific error types
            if (response.ErrorMessage?.Contains("404") == true)
            {
                Debug.LogWarning("ExampleUsage", "API endpoint not found - possible firmware version issue");
            }
            else if (response.ErrorMessage?.Contains("timeout") == true)
            {
                Debug.LogWarning("ExampleUsage", "Request timeout - device may be offline");
            }
        }

        private void HandleFailedRequest(QueuedRequest failedRequest)
        {
            // Log failed request details
            Debug.LogError("ExampleUsage", "Failed request details - Path: {0}, Method: {1}, Retries: {2}",
                failedRequest.Command.Path, failedRequest.Command.Method, failedRequest.RetryCount);
            
            // Could implement exponential backoff, circuit breaker pattern, etc.
        }

        private void ParseDeviceInfo(string responseContent)
        {
            // Parse JSON response for device information
            Debug.LogInformation("ExampleUsage", "Device info parsed: {0}", responseContent);
        }

        private void ParsePtzSettings(string responseContent)
        {
            // Parse JSON response for PTZ settings
            Debug.LogInformation("ExampleUsage", "PTZ settings parsed: {0}", responseContent);
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_queueManager != null)
            {
                _queueManager.ResponseReceived -= OnResponseReceived;
                _queueManager.RequestFailed -= OnRequestFailed;
                _queueManager.QueueStatusChanged -= OnQueueStatusChanged;
                _queueManager.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// Example of a custom device controller using the CommandQueueManager
    /// </summary>
    public class CustomDeviceController : IDisposable
    {
        private readonly CommandQueueManager _queueManager;
        private readonly string _deviceKey;

        public CustomDeviceController(string deviceKey, string ipAddress, int port)
        {
            _deviceKey = deviceKey;
            
            _queueManager = new CommandQueueManager(
                deviceKey: deviceKey,
                getBaseUri: () => $"http://{ipAddress}:{port}/api/v1",
                httpClient: new HttpClient()
            );

            // Subscribe to events
            _queueManager.ResponseReceived += OnResponseReceived;
            _queueManager.RequestFailed += OnRequestFailed;

            // Start processing
            _queueManager.StartProcessing();
        }

        public void SendCommand(string endpoint, string method = "GET", string body = null)
        {
            var command = new BirddogApiCommand(endpoint, method, body);
            _queueManager.EnqueueRequest(command);
        }

        public async Task SendUrgentCommand(string endpoint, string method = "GET", string body = null)
        {
            var command = new BirddogApiCommand(endpoint, method, body);
            await _queueManager.SendHighPriorityRequest(command);
        }

        public (int RequestCount, int ResponseCount) GetQueueStatus()
        {
            return _queueManager.GetQueueStatus();
        }

        private void OnResponseReceived(object sender, CameraResponse response)
        {
            Debug.LogInformation(_deviceKey, "Response: {0}", response.Content);
        }

        private void OnRequestFailed(object sender, QueuedRequest failedRequest)
        {
            Debug.LogError(_deviceKey, "Request failed: {0}", failedRequest.Command.Path);
        }

        public void Dispose()
        {
            _queueManager?.Dispose();
        }
    }
}