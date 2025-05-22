# gRPC Client Implementation

## Overview

This document describes the implementation of a gRPC client/adapter for the scheduler-Data project. The client fetches data from gRPC services and stores it in the database using the existing domain services.

## Architecture

The implementation follows a layered architecture:

1. **API Layer** - `IDataImportApi` replaces the previous `ICommand` pattern with a more API-oriented approach
2. **Adapter Layer** - `IGrpcDataAdapter` provides methods for importing different types of data
3. **Client Layer** - gRPC clients for communicating with specific services
4. **Domain Service Layer** - Existing domain services used for storing the data

## Components

### API Layer

- **IDataImportApi** - Interface for the data import API
- **GrpcDataImportApi** - Implementation of the data import API using the gRPC adapter

### Adapter Layer

- **IGrpcDataAdapter** - Interface for importing data from gRPC services
- **GrpcDataAdapter** - Implementation that uses gRPC clients to fetch data and domain services to store it
- **GrpcAdapterOptions** - Configuration options for the gRPC adapter

### Client Layer

- **BaseGrpcClient** - Base class with common functionality for all gRPC clients
- **ISequenceGrpcClient**, **IParameterGrpcClient**, etc. - Interfaces for specific gRPC clients
- **SequenceGrpcClient**, **ParameterGrpcClient**, etc. - Implementations of the gRPC clients

## Key Features

- **Service Injection** - Uses dependency injection for both gRPC clients and domain services
- **Retry Mechanism** - Includes retry logic for handling transient failures
- **Batch Processing** - Processes entities in batches to avoid large transactions
- **Configurable Options** - Provides options for connection settings, retry behavior, etc.
- **Testing Support** - Designed for unit testing with dependency injection

## Data Flow

1. The API layer receives a request to import data
2. The adapter layer coordinates the import process
3. gRPC clients fetch data from the corresponding services
4. Domain services store the data in the database

## Usage

To use the gRPC client implementation:

1. Register the services in your DI container:

```csharp
services.AddGrpcClientServices(configuration);
```

2. Configure the adapter options in your appsettings.json:

```json
{
  "GrpcAdapter": {
    "BaseAddress": "https://your-grpc-server.com",
    "TimeoutSeconds": 30,
    "UseSecureConnection": true,
    "ClearExistingDataBeforeImport": false,
    "MaxBatchSize": 100,
    "RetryCount": 3,
    "RetryDelayMilliseconds": 1000
  }
}
```

3. Inject and use the API:

```csharp
public class YourService
{
    private readonly IDataImportApi _dataImportApi;
    
    public YourService(IDataImportApi dataImportApi)
    {
        _dataImportApi = dataImportApi;
    }
    
    public async Task ImportAllDataAsync()
    {
        await _dataImportApi.ImportAllDataAsync();
    }
}
```

## Error Handling

The implementation includes comprehensive error handling:

- Retries for transient failures
- Logging of errors at appropriate levels
- Exception propagation with clear error messages
- Connection testing to verify service availability

## Unit Testing

Unit tests are provided for:

- GrpcDataAdapter
- GrpcDataImportApi

## Requirements Fulfilled

1. ✅ Created a gRPC client/adapter to query service endpoints
2. ✅ Stored the retrieved data in the database
3. ✅ Repurposed/extended existing adapter code
4. ✅ Replaced the ICommand interface with a more API-oriented design
5. ✅ Provided unit tests for the implementation
6. ✅ Documented the implementation