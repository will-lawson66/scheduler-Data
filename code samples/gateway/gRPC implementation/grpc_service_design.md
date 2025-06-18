# Fake gRPC ExecutionConfigurationService Implementation Design

## Overview
Design for implementing a fake/mock gRPC service that implements `IExecutionConfigurationService` for integration and end-to-end testing of the scheduler-Data system.

## Architecture Design

### 1. Project Structure
```
Instrument.Execution.Grpc.FakeService/
├── FakeExecutionConfigurationService.cs      # Main service implementation
├── Data/
│   ├── FakeDataProvider.cs                   # Manages sample data
│   ├── SampleData.json                       # JSON test data
│   └── ConfigurationBuilder.cs               # Builds complex configurations
├── Configuration/
│   ├── FakeServiceOptions.cs                 # Configuration options
│   └── ServiceCollectionExtensions.cs        # DI registration
└── Utilities/
    ├── ResponseBuilder.cs                     # Helper for building responses
    └── DelaySimulator.cs                      # Simulates network delays
```

### 2. Core Components

#### FakeExecutionConfigurationService
- Implements `IExecutionConfigurationService`
- Returns realistic test data in JSON format as if from gRPC gateway
- Supports configurable delays and error simulation
- Thread-safe operation with concurrent request handling

#### FakeDataProvider
- Manages sample execution configurations
- Loads data from JSON files or generates programmatically
- Supports multiple test scenarios (minimal, complex, error cases)

### 3. Key Features

#### Data Generation
- **Realistic Sample Data**: Pre-built configurations with sequences, resources, and parameters
- **Multiple Scenarios**: Different data sets for various test cases
- **JSON Persistence**: Data served as JSON (simulating gRPC gateway persistence)

#### Configuration Options
```csharp
public class FakeServiceOptions
{
    public TimeSpan ResponseDelay { get; set; } = TimeSpan.Zero;
    public double FailureRate { get; set; } = 0.0;
    public string DataSourcePath { get; set; } = "SampleData.json";
    public bool EnableConcurrencyTesting { get; set; } = true;
    public int MaxConcurrentRequests { get; set; } = 10;
}
```

#### Error Simulation
- Configurable failure rates for testing error handling
- Specific error scenarios (timeouts, not found, validation errors)
- Realistic gRPC error responses with proper status codes

## Implementation Strategy

### Phase 1: Basic Service Implementation
1. Create fake service class implementing `IExecutionConfigurationService`
2. Implement all 4 required methods with basic responses
3. Add sample JSON data representing typical configurations

### Phase 2: Advanced Features
1. Add configurable delays and error simulation
2. Implement multiple data scenarios
3. Add concurrency testing support
4. Include comprehensive logging

### Phase 3: Integration
1. Create service registration extensions
2. Add configuration options
3. Create unit tests for the fake service
4. Documentation and usage examples

## Sample Data Structure

### ExecutionConfiguration Sample
```json
{
  "startingPeriod": 1,
  "rolloverPeriod": 1000,
  "periodSpan": "00:00:30",
  "periodAcceleration": 1.0,
  "sequences": [
    {
      "key": "SEQUENCE_001",
      "worstCaseTime": "00:02:00",
      "executionMethod": "Native",
      "scriptKey": "",
      "resources": [
        {
          "key": "RESOURCE_001",
          "hasScriptingInterface": true,
          "scriptingInterface": "IInstrumentResource"
        }
      ],
      "parameters": [
        {
          "parameterName": "TargetValue",
          "parameterType": "DecimalType"
        }
      ]
    }
  ]
}
```

## Testing Strategy

### Unit Tests
- Test each service method with various inputs
- Verify error handling and edge cases
- Test concurrent access scenarios

### Integration Tests
- Test with actual gRPC clients
- Verify JSON serialization/deserialization
- Test service registration and dependency injection

### Performance Tests
- Measure response times under load
- Test concurrent request handling
- Verify memory usage with large datasets

## Benefits

1. **Realistic Testing**: Provides realistic data structures and responses
2. **Flexible Configuration**: Supports multiple test scenarios and configurations
3. **Error Simulation**: Tests error handling and resilience
4. **Performance Testing**: Enables load and concurrent testing
5. **Easy Integration**: Simple service registration and configuration
6. **JSON Persistence**: Simulates real gRPC gateway data persistence

## Usage Examples

### Basic Registration
```csharp
services.AddFakeExecutionConfigurationService(options =>
{
    options.ResponseDelay = TimeSpan.FromMilliseconds(100);
    options.DataSourcePath = "test-data.json";
});
```

### Advanced Configuration
```csharp
services.AddFakeExecutionConfigurationService(options =>
{
    options.ResponseDelay = TimeSpan.FromSeconds(1);
    options.FailureRate = 0.1; // 10% failure rate
    options.EnableConcurrencyTesting = true;
    options.MaxConcurrentRequests = 50;
});
```

This design provides a comprehensive fake gRPC service that can be used for integration testing, load testing, and development scenarios where the actual execution engine is not available.