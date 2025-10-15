# Command Queue System

This document describes the command queue system implemented for the Birddog Camera plugin, which provides reliable, asynchronous processing of API requests and responses.

## Overview

The queue system has been extracted into a reusable `CommandQueueManager` class that can be used by any device controller that needs to send HTTP requests asynchronously with retry logic and response handling.

## Architecture

### Core Components

1. **CommandQueueManager** - Main queue management class
2. **QueuedRequest** - Represents a request with metadata and callbacks
3. **CameraResponse** - Represents a response with correlation information
4. **BirddogApiCommand** - Command structure (existing)

### Key Features

- ✅ **Asynchronous Processing** - Non-blocking request/response handling
- ✅ **Automatic Retry Logic** - Failed requests retry up to 3 times with delays
- ✅ **Request/Response Correlation** - Each request gets a unique ID for tracking
- ✅ **Callback Support** - Success and error callbacks for each request
- ✅ **Queue Management** - Methods to clear queues, check status, and monitor processing
- ✅ **Event-Driven Architecture** - Events for response handling and failure notifications
- ✅ **High Priority Bypass** - Option to send urgent requests immediately
- ✅ **Thread-Safe Operations** - Uses ConcurrentQueue for safe multi-threading
- ✅ **Proper Resource Management** - IDisposable implementation with cleanup

## Usage

### Basic Usage

```csharp
// Initialize the queue manager
var queueManager = new CommandQueueManager(
    deviceKey: "my-device",
    getBaseUri: () => "http://192.168.1.100:8080",
    httpClient: new HttpClient()
);

// Start processing
queueManager.StartProcessing();

// Send a command
var command = new BirddogApiCommand("about", "GET");
queueManager.EnqueueRequest(command);

// Cleanup when done
queueManager.Dispose();
```

### With Callbacks

```csharp
var command = new BirddogApiCommand("birddogptzsetup", "GET");
queueManager.EnqueueRequest(command,
    onSuccess: (response) => {
        Console.WriteLine($"Success: {response}");
    },
    onError: (error) => {
        Console.WriteLine($"Error: {error}");
    });
```

### High Priority Requests

```csharp
// Bypass the queue for urgent requests
var urgentCommand = new BirddogApiCommand("reboot", "POST");
await queueManager.SendHighPriorityRequest(urgentCommand);
```

### Event Handling

```csharp
// Subscribe to events
queueManager.ResponseReceived += (sender, response) => {
    Console.WriteLine($"Received response for {response.RequestId}");
};

queueManager.RequestFailed += (sender, request) => {
    Console.WriteLine($"Request failed: {request.RequestId}");
};

queueManager.QueueStatusChanged += (sender, status) => {
    Console.WriteLine($"Queue status: {status.RequestCount} requests, {status.ResponseCount} responses");
};
```

### Monitoring

```csharp
// Check queue status
var (requestCount, responseCount) = queueManager.GetQueueStatus();

// Log detailed status
queueManager.LogQueueStatus();

// Clear queues if needed
queueManager.ClearRequestQueue();
queueManager.ClearResponseQueue();
```

## Integration with BirddogCameraController

The `BirddogCameraController` now uses the `CommandQueueManager` internally:

```csharp
// All existing methods now use the queue system
CameraOn();  // Queued automatically
PresetSelect(1);  // Queued automatically

// Direct access to queue features
var status = GetQueueStatus();
LogQueueStatus();
await SendHighPriorityRequest(command);
```

## Configuration

The queue manager can be configured through constructor parameters:

```csharp
public CommandQueueManager(
    string deviceKey,           // Device identifier for logging
    Func<string> getBaseUri,    // Function to get base URI
    HttpClient httpClient = null // Optional HTTP client
)
```

### Constants (can be modified in the class)

- `MaxRetryCount = 3` - Maximum number of retries for failed requests
- `RequestTimeoutMs = 2000` - HTTP request timeout
- `ProcessingDelayMs = 50` - Delay between queue processing iterations
- `RetryDelayMs = 1000` - Delay before retrying failed requests
- `ErrorDelayMs = 1000` - Delay after processing errors

## Error Handling

The system provides comprehensive error handling:

1. **Network Errors** - Automatic retry with exponential backoff
2. **HTTP Errors** - Captured and passed to error callbacks
3. **Timeout Errors** - Handled with retry logic
4. **Processing Errors** - Logged and system continues running

## Thread Safety

The queue system is fully thread-safe:

- Uses `ConcurrentQueue<T>` for queue operations
- Proper cancellation token handling
- Safe event raising and subscription
- Resource cleanup with dispose pattern

## Performance Considerations

- **Memory Usage** - Queues are bounded by natural flow, but can be cleared if needed
- **CPU Usage** - Background tasks use delays to prevent busy waiting
- **Network Usage** - Requests are processed sequentially to avoid overwhelming the device
- **Responsiveness** - High-priority bypass available for urgent requests

## Migration from Direct HTTP Calls

Old code:
```csharp
// Direct HTTP call (blocking)
SendRequestDirect(command);
```

New code:
```csharp
// Queued call (non-blocking)
SendRequest(command, 
    onSuccess: (response) => { /* handle success */ },
    onError: (error) => { /* handle error */ }
);
```

## Examples

See `Examples/ExampleUsage.cs` for comprehensive usage examples including:

- Basic queue operations
- Event handling
- Response processing
- Error handling
- Custom device controller implementation

## Future Enhancements

Potential improvements that could be added:

1. **Priority Levels** - Multiple priority queues
2. **Circuit Breaker** - Automatic failure handling
3. **Rate Limiting** - Configurable request rate limits
4. **Metrics Collection** - Performance and reliability metrics
5. **Persistent Queues** - Queue persistence across restarts
6. **Request Batching** - Combine multiple requests when possible
7. **Response Caching** - Cache responses for repeated requests