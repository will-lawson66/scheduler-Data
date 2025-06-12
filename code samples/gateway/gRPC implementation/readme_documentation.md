# Fake gRPC ExecutionConfigurationService Implementation

This implementation provides a comprehensive fake/mock gRPC service for the `IExecutionConfigurationService` interface, designed for integration testing, end-to-end testing, and development scenarios where the actual execution engine is not available.

## Features

- **Realistic Data**: Returns JSON-serialized data that mimics real gRPC gateway responses
- **Configurable Behavior**: Supports delays, error simulation, and different test scenarios
- **Thread-Safe**: Handles concurrent requests correctly
- **Comprehensive Logging**: Detailed logging for debugging and monitoring
- **Easy Integration**: Simple service registration with dependency injection
- **Multiple Test Scenarios**: Pre-built configurations for different testing needs

## Quick Start

### 1. Basic Registration

```csharp
// In Startup.cs or Program.cs
services.AddFakeExecutionConfigurationService();
```

### 2. With Custom Configuration

```csharp
services.AddFakeExecutionConfigurationService(options =>
{
    options.ResponseDelay = TimeSpan.FromMilliseconds(100);
    options.DataSourcePath = "custom-test-data.json";
    options.EnableDetailedLogging = true;
});
```

### 3. For Testing with Error Simulation

```csharp
services.AddFakeExecutionConfigurationServiceWithErrors(
    failureRate: 0.1, // 10% failure rate
    responseDelay: TimeSpan.FromMilliseconds(50)
);
```

### 4. For Load Testing

```csharp
services.AddFakeExecutionConfigurationServiceForLoadTesting(
    responseDelay: TimeSpan.FromMilliseconds(500),
    maxConcurrentRequests: 100
);
```

## Configuration Options

### FakeServiceOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ResponseDelay` | `TimeSpan` | `TimeSpan.Zero` | Artificial delay to simulate network latency |
| `FailureRate` | `double` | `0.0` | Probability of simulating failures (0.0 to 1.0) |
| `DataSourcePath` | `string` | `"SampleData.json"` | Path to JSON file with custom test data |
| `EnableConcurrencyTesting` | `bool` | `true` | Enable features for testing concurrent access |
| `MaxConcurrentRequests` | `int` | `10` | Maximum concurrent requests to handle |
| `EnableDetailedLogging` | `bool` | `true` | Enable detailed logging for debugging |
| `TestScenario` | `string` | `"Default"` | Test scenario to use |

## Usage Examples

### Basic Service Usage

```csharp
public class ExecutionConfigurationTests
{
    private readonly IExecutionConfigurationService _service;

    public ExecutionConfigurationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFakeExecutionConfigurationService();
        
        var provider = services.BuildServiceProvider();
        _service = provider.GetRequiredService<IExecutionConfigurationService>();
    }

    [Fact]
    public async Task GetConfiguration_ReturnsValidData()
    {
        // Arrange
        var request = new FetchExecutionConfigurationRequest(IncludeSequences: true);

        // Act
        var response = await _service.GetCurrentConfigurationAsync(request);

        // Assert
        Assert.NotNull(response.Configuration);
        Assert.Empty(response.Errors);
        Assert.NotEmpty(response.Configuration.Sequences);
    }
}
```

### Testing Error Scenarios

```csharp
[Fact]
public async Task HandleServiceFailures_GracefulDegradation()
{
    // Arrange - Service with 100% failure rate
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddFakeExecutionConfigurationServiceWithErrors(failureRate: 1.0);
    
    using var provider = services.BuildServiceProvider();
    var service = provider.GetRequiredService<IExecutionConfigurationService>();

    // Act
    var response = await service.GetCurrentConfigurationAsync(
        new FetchExecutionConfigurationRequest(true));

    // Assert
    Assert.Null(response.Configuration);
    Assert.NotEmpty(response.Errors);
    Assert.Equal(500, response.Errors.First().ErrorCode);
}
```

### Load Testing

```csharp
[Fact]
public async Task ConcurrentRequests_HandleCorrectly()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddFakeExecutionConfigurationServiceForLoadTesting(
        responseDelay: TimeSpan.FromMilliseconds(100),
        maxConcurrentRequests: 50
    );
    
    using var provider = services.BuildServiceProvider();
    var service = provider.GetRequiredService<IExecutionConfigurationService>();

    var request = new FetchExecutionConfigurationRequest(true);
    var tasks = new Task<FetchExecutionConfigurationResponse>[20];

    // Act
    for (int i = 0; i < tasks.Length; i++)
    {
        tasks[i] = service.GetCurrentConfigurationAsync(request);
    }

    var responses = await Task.WhenAll(tasks);

    // Assert
    Assert.All(responses, r => Assert.NotNull(r.Configuration));
    
    // Verify unique request IDs
    var requestIds = responses.Select(r => r.RequestId).ToArray();
    Assert.Equal(requestIds.Length, requestIds.Distinct().Count());
}
```

## Custom Test Data

### JSON Data Format

Create a JSON file with the following structure:

```json
{
  "startingPeriod": 1,
  "rolloverPeriod": 1000,
  "periodSpan": "00:00:30",
  "periodAcceleration": 1.0,
  "sequences": [
    {
      "key": "YOUR_SEQUENCE_KEY",
      "worstCaseTime": "00:02:00",
      "executionMethod": "Native",
      "scriptKey": "",
      "resources": [
        {
          "key": "YOUR_RESOURCE_KEY",
          "hasScriptingInterface": true,
          "scriptingInterface": "IYourInterface"
        }
      ],
      "parameters": [
        {
          "parameterName": "YourParameter",
          "parameterType": "DecimalType"
        }
      ]
    }
  ]
}
```

### Loading Custom Data

```csharp
services.AddFakeExecutionConfigurationService(options =>
{
    options.DataSourcePath = "path/to/your-test-data.json";
});
```

## API Coverage

The fake service implements all methods of `IExecutionConfigurationService`:

### GetCurrentConfigurationAsync
- Returns full execution configuration
- Supports `IncludeSequences` parameter
- Returns empty sequences list when `IncludeSequences` is false

### GetSequenceConfigurationAsync
- Returns specific sequence by key
- Returns 404 error if sequence not found
- Returns 400 error if key is empty/null

### GetResourceConfigurationAsync
- Returns specific resource by key
- Searches across all sequences for the resource
- Returns 404 error if resource not found

### ReloadExecutionConfigurationAsync
- Simulates configuration reload
- Clears cached data and reloads from source
- Always returns success (unless failure simulation is enabled)

## Integration with Existing Tests

### Unit Tests

```csharp
public class YourServiceTests
{
    [Fact]
    public async Task YourMethod_WithFakeConfigService_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFakeExecutionConfigurationService();
        services.AddScoped<YourService>(); // Your service that depends on IExecutionConfigurationService
        
        using var provider = services.BuildServiceProvider();
        var yourService = provider.GetRequiredService<YourService>();

        // Act & Assert
        // Test your service with the fake configuration service
    }
}
```

### Integration Tests

```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real service with fake
                services.RemoveAll<IExecutionConfigurationService>();
                services.AddFakeExecutionConfigurationService();
            });
        });
    }

    [Fact]
    public async Task ApiEndpoint_WithFakeConfigService_ReturnsExpectedData()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/configuration");

        // Assert
        response.EnsureSuccessStatusCode();
        // Additional assertions...
    }
}
```

## Performance Considerations

- **Memory Usage**: Sample data is cached in memory for fast access
- **Thread Safety**: All operations are thread-safe using proper locking
- **Scalability**: Designed to handle moderate concurrent load (configurable)
- **Resource Cleanup**: Implements proper disposal patterns

## Troubleshooting

### Common Issues

1. **Service not registered**: Ensure you call one of the `AddFakeExecutionConfigurationService*` methods
2. **JSON parsing errors**: Verify your custom JSON file format matches the expected structure
3. **Concurrency issues**: Check the `MaxConcurrentRequests` setting if experiencing timeouts
4. **Missing logs**: Enable detailed logging with `EnableDetailedLogging = true`

### Debugging Tips

- Enable detailed logging to see request processing flow
- Use unique test scenarios to isolate different test cases
- Monitor response times to verify delay simulation is working
- Check error rates in logs when using failure simulation

## Architecture

```
FakeExecutionConfigurationService
├── FakeDataProvider (manages test data)
├── DelaySimulator (simulates network delays)
├── ResponseBuilder (constructs error responses)
└── ConfigurationBuilder (generates default configurations)
```

This implementation provides a robust foundation for testing gRPC services without requiring actual execution engine infrastructure.