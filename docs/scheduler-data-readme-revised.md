# Instrument.Scheduling.Data

A data access layer for laboratory instrument scheduling operations.

## Overview

The `Instrument.Scheduling.Data` project is a .NET 8.0 library implementing a data persistence layer for laboratory test scheduling. It provides entity definitions, repository abstractions, and domain services for managing sequences, parameters, sequence groups, and resources.

## Project Structure

```
Instrument.Scheduling.Data/
├── Configuration/          # Storage configuration
├── DataContext/            # EF Core DbContext
├── Entities/               # Domain model entities
│   ├── Enums/              # Enumeration types
├── Exceptions/             # Data-specific exceptions
├── Initialization/         # Data initialization
├── Interfaces/             # Repository interfaces
├── Migrations/             # EF Core migrations
├── Providers/              # Storage providers
├── Repository/             # Repository implementations
├── Services/               # Domain services
│   ├── Cleanup/            # Data cleanup services
├── Validation/             # Parameter validation
└── UnitOfWork.cs           # Unit of Work implementation
```

## Domain Model

### Core Entities

The project defines these primary entities:

#### Sequence

```csharp
public record Sequence
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required TimeSpan WorstCaseTime { get; init; }
    public bool CanBeParallel { get; init; }
    
    // Navigation property
    public List<SequenceParameter> SequenceParameters { get; init; } = [];
}
```

#### Parameter

```csharp
public record Parameter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Min { get; init; }
    public string? Max { get; init; }
    public string? DefaultValue { get; init; }
    public string? Format { get; init; }
    public string? RangeId { get; init; }
    public string? ResourceId { get; init; }
    
    // Navigation properties
    public Range? Range { get; init; }
    public Resource? Resource { get; init; }
    public List<SequenceParameter> SequenceParameters { get; init; } = [];
}
```

#### SequenceGroup

```csharp
public class SequenceGroup : SequenceGroupBase
{
    public List<SequenceGroupSequences> SequenceGroupSequences { get; init; } = [];
}

public abstract class SequenceGroupBase
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}
```

#### Resource

```csharp
public record Resource
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    
    // Navigation property
    public List<Parameter> Parameters { get; init; } = [];
}
```

#### Range & RangeValue

```csharp
public record Range
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    
    // Navigation properties
    public List<Parameter> Parameters { get; init; } = [];
    public List<RangeValue> Values { get; init; } = [];
}

public record RangeValue
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Value { get; init; }
    public required string RangeId { get; init; }
    
    // Navigation property
    public Range? Range { get; init; }
}
```

### Entity Relationships

- **Sequence-Parameter**: Many-to-many through `SequenceParameter`
- **Parameter-Range**: Many-to-one
- **Parameter-Resource**: Many-to-one
- **SequenceGroup-Sequence**: Many-to-many through `SequenceGroupSequences` with ordering

## Repository Interfaces

### ISequenceRepository

```csharp
public interface ISequenceRepository
{
    Task<IEnumerable<Sequence>> GetAllAsync();
    Task<Sequence?> GetByIdAsync(string id);
    Task<IQueryable<Sequence>> GetQueryableAsync();
    Task AddAsync(Sequence sequence);
    Task UpdateAsync(Sequence sequence);
    Task DeleteAsync(string id);
    Task SaveChangesAsync();
}
```

### ISequenceGroupRepository

```csharp
public interface ISequenceGroupRepository : ISequenceRepository
{
    Task<SequenceGroup?> GetWithSequencesAsync(string id);
    Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(string sequenceGroupId);
}
```

Similar interfaces exist for `IParameterRepository`, `IRangeRepository`, `IRangeValueRepository`, and `IResourceRepository`.

## Unit of Work

```csharp
public interface IUnitOfWork : IDisposable
{
    ISequenceRepository SequenceDefinitions { get; }
    IParameterRepository Parameters { get; }
    IRangeRepository Ranges { get; }
    IRangeValueRepository RangeValues { get; }
    IResourceRepository Resources { get; }
    ISequenceGroupRepository SequenceGroups { get; }
    SequenceGroupService SequenceGroupService { get; }
    Task<int> SaveChangesAsync();
}
```

## Storage Providers

The library supports three storage backends:

### JSON Storage

```csharp
public class JsonStorageProvider<T> : IStorageProvider<T> where T : class
{
    private readonly string _filePath;
    
    public JsonStorageProvider(string filePath)
    {
        _filePath = filePath;
    }
    
    // Implements IStorageProvider<T> methods
}
```

### SQLite Storage

```csharp
public class SqliteStorageProvider<T> : IStorageProvider<T> where T : class
{
    private readonly SchedulerDbContext _dbContext;
    
    public SqliteStorageProvider(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    // Implements IStorageProvider<T> methods
}
```

### SQL Server Storage

```csharp
public class SqlServerStorageProvider<T> : IStorageProvider<T> where T : class
{
    private readonly SchedulerDbContext _dbContext;
    
    public SqlServerStorageProvider(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    // Implements IStorageProvider<T> methods
}
```

## Domain Services

### SequenceGroupService

```csharp
public class SequenceGroupService
{
    public SequenceGroupService(
        ISequenceGroupRepository sequenceGroupRepository,
        ISequenceRepository sequenceRepository,
        SchedulerDbContext dbContext,
        ILogger<SequenceGroupService> logger)
    {
        // Initialize service
    }
    
    // Core methods
    public async Task<SequenceGroup> CreateSequenceGroupAsync(string id, string name, string? description = null);
    public async Task<bool> AddSequenceToGroupAsync(string sequenceGroupId, string sequenceId, int order);
    public async Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(string sequenceGroupId);
    public async Task<IEnumerable<SequenceGroup>> GetAllSequenceGroupsAsync();
    public async Task<SequenceGroup?> GetSequenceGroupByIdAsync(string id);
    public async Task<SequenceGroup?> GetSequenceGroupWithSequencesAsync(string id);
    public async Task<bool> DeleteSequenceGroupAsync(string id);
    public async Task<bool> RemoveSequenceFromGroupAsync(string sequenceGroupId, string sequenceId);
    public async Task<bool> ReorderSequenceInGroupAsync(string sequenceGroupId, string sequenceId, int newOrder);
    public async Task<bool> ValidateSequenceGroupAsync(string sequenceGroupId);
}
```

## Configuration

```csharp
public class StorageConfiguration
{
    public StorageProviderType Provider { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string JsonFilePath { get; set; } = string.Empty;
}

public enum StorageProviderType
{
    Json,
    SQLite,
    SqlServer
}
```

## Service Registration

```csharp
// Service Collection Extensions
public static class ServiceCollectionExtensions
{
    // Register the data layer
    public static IServiceCollection AddSchedulerDataLayer(
        this IServiceCollection services, 
        StorageConfiguration config)
    {
        // Register providers, repositories, and services
        return services;
    }
    
    // Register with data initialization
    public static IServiceCollection AddSchedulerDataWithInitialization(
        this IServiceCollection services,
        StorageConfiguration config)
    {
        // Add data services and initialization
        return services;
    }
    
    // Register data initialization
    public static IServiceCollection AddDataInitialization(
        this IServiceCollection services)
    {
        // Register factory
        return services;
    }
    
    // Register cleanup services
    public static IServiceCollection AddCleanupServices(
        this IServiceCollection services)
    {
        // Register cleanup services
        return services;
    }
}
```

## Data Initialization

```csharp
public interface IDataInitializer
{
    Task InitializeAsync();
}

public class DataInitializerFactory
{
    public IDataInitializer CreateInitializer(
        StorageProviderType providerType,
        IServiceProvider serviceProvider)
    {
        // Create the appropriate initializer based on provider type
    }
}
```

## Basic Usage Example

```csharp
// Register services
services.AddSchedulerDataLayer(new StorageConfiguration
{
    Provider = StorageProviderType.SQLite,
    ConnectionString = "Data Source=scheduler.db"
});

// Inject and use the Unit of Work
public class SequenceManager
{
    private readonly IUnitOfWork _unitOfWork;
    
    public SequenceManager(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IEnumerable<Sequence>> GetAllSequencesAsync()
    {
        return await _unitOfWork.SequenceDefinitions.GetAllAsync();
    }
}

// Use the SequenceGroupService
public class SequenceGroupManager
{
    private readonly SequenceGroupService _sequenceGroupService;
    
    public SequenceGroupManager(SequenceGroupService sequenceGroupService)
    {
        _sequenceGroupService = sequenceGroupService;
    }
    
    public async Task CreateSampleSequenceGroupAsync()
    {
        // Create a sequence group
        var group = await _sequenceGroupService.CreateSequenceGroupAsync(
            "SG001", "Sample Preparation", "Prepares samples for testing");
            
        // Add sequences to the group
        await _sequenceGroupService.AddSequenceToGroupAsync("SG001", "S001", 1);
        await _sequenceGroupService.AddSequenceToGroupAsync("SG001", "S002", 2);
        
        // Get ordered sequences
        var orderedSequences = await _sequenceGroupService.GetOrderedSequencesAsync("SG001");
    }
}
```

## Dependencies

- .NET 8.0
- Entity Framework Core 9.0.4
- Microsoft.EntityFrameworkCore.Sqlite 9.0.4
- Microsoft.EntityFrameworkCore.SqlServer 9.0.4
- Microsoft.Extensions.Hosting 9.0.4
