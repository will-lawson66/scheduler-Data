# Instrument.Data Core Layer

A comprehensive data access layer for laboratory instrument scheduling operations.

## Table of Contents

1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Domain Model](#domain-model)
4. [Repository Pattern](#repository-pattern)
5. [Service Layer](#service-layer)
6. [Storage Providers](#storage-providers)
7. [Configuration](#configuration)
8. [Dependency Injection](#dependency-injection)
9. [Initialization and Seeding](#initialization-and-seeding)
10. [Usage Examples](#usage-examples)

## Overview

The `Instrument.Data` project is a .NET 8.0 library implementing a data persistence layer for laboratory test scheduling. It provides entity definitions, repository abstractions, and domain services for managing sequences, parameters, sequence groups, and resources.

## Project Structure

```
Instrument.Data/
├── Configuration/          # Storage configuration
├── DataContext/            # EF Core DbContext
├── Entities/               # Domain model entities
│   ├── Enums/              # Enumeration types
├── Exceptions/             # Data-specific exceptions
├── Initialization/         # Data initialization
├── Interfaces/             # Repository interfaces
├── Migrations/             # EF Core migrations
├── Repository/             # Repository implementations
├── Services/               # Domain services
│   ├── Cleanup/            # Data cleanup services
└── ServiceCollectionExtensions.cs # DI registration
```

## Domain Model

### Core Entities

The project defines these primary entities:

#### Sequence

Represents a discrete operation performed by an instrument.

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
    
    // Update method - returns a new instance with the specified changes
    public Sequence Update(string? name = null, TimeSpan? worstCaseTime = null, 
                          string? description = null, bool? canBeParallel = null)
    {
        // Validate updates if needed
        if (name != null && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
            
        // Use record's "with" expression for creating a modified copy
        return this with 
        {
            Name = name ?? Name,
            WorstCaseTime = worstCaseTime ?? WorstCaseTime,
            Description = description ?? Description,
            CanBeParallel = canBeParallel ?? CanBeParallel
        };
    }
}
```

#### Parameter

Represents a configurable value with type validation and ranges.

```csharp
public record Parameter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ParameterType Type { get; init; }
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

Represents an ordered collection of sequences for complete test procedures.

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

Represents physical or logical components required for operations.

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

Defines sets of discrete values that parameters can take.

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

- **Sequence-Parameter**: Many-to-many relationship through `SequenceParameter`
- **Parameter-Range**: Many-to-one relationship
- **Parameter-Resource**: Many-to-one relationship
- **SequenceGroup-Sequence**: Many-to-many relationship through `SequenceGroupSequences` with ordering

## Repository Pattern

The library uses the repository pattern to abstract data access operations:

### Core Repository Interface

```csharp
public interface IRepository<TEntity> where TEntity : class
{
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity?> GetByIdAsync(string id);
    Task<IQueryable<TEntity>> GetQueryableAsync();
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(string id);
    Task SaveChangesAsync();
}
```

### Entity-Specific Repositories

```csharp
public interface ISequenceRepository : IRepository<Sequence>
{
    Task<IEnumerable<Sequence>> GetByNameAsync(string namePattern);
    Task<IEnumerable<Sequence>> GetByParameterIdAsync(string parameterId);
}

public interface IParameterRepository : IRepository<Parameter>
{
    Task<IEnumerable<Parameter>> GetBySequenceIdAsync(string sequenceId);
    Task<IEnumerable<Parameter>> GetByRangeIdAsync(string rangeId);
    Task<IEnumerable<Parameter>> GetByResourceIdAsync(string resourceId);
}
```

Similar interfaces exist for `IRangeRepository`, `IRangeValueRepository`, `IResourceRepository`, and `ISequenceGroupRepository`.

### Repository Implementations

```csharp
public class SequenceRepository : Repository<Sequence>, ISequenceRepository
{
    public SequenceRepository(SchedulerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Sequence>> GetByNameAsync(string namePattern)
    {
        return await _dbContext.Sequences
            .Where(s => EF.Functions.Like(s.Name, $"%{namePattern}%"))
            .ToListAsync();
    }

    public async Task<IEnumerable<Sequence>> GetByParameterIdAsync(string parameterId)
    {
        return await _dbContext.Sequences
            .Include(s => s.SequenceParameters)
            .Where(s => s.SequenceParameters.Any(sp => sp.ParameterId == parameterId))
            .ToListAsync();
    }
}
```

## Service Layer

The service layer implements business logic and coordinates operations across repositories:

### Service Interfaces

```csharp
public interface ISequenceService
{
    Task<IEnumerable<Sequence>> GetAllSequencesAsync();
    Task<Sequence?> GetSequenceByIdAsync(string id);
    Task<Sequence> CreateSequenceAsync(Sequence sequence);
    Task UpdateSequenceAsync(Sequence sequence);
    Task DeleteSequenceAsync(string id);
    Task<IEnumerable<Parameter>> GetSequenceParametersAsync(string sequenceId);
    Task AddParameterToSequenceAsync(string sequenceId, string parameterId);
    Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId);
}
```

### Service Implementations

```csharp
public class SequenceService : ISequenceService
{
    private readonly ISequenceRepository _sequenceRepository;
    private readonly IParameterRepository _parameterRepository;
    private readonly ILogger<SequenceService> _logger;
    
    public SequenceService(
        ISequenceRepository sequenceRepository,
        IParameterRepository parameterRepository,
        ILogger<SequenceService> logger)
    {
        _sequenceRepository = sequenceRepository;
        _parameterRepository = parameterRepository;
        _logger = logger;
    }
    
    public async Task<IEnumerable<Sequence>> GetAllSequencesAsync()
    {
        try
        {
            return await _sequenceRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all sequences");
            throw new StorageProviderException("Failed to retrieve sequences", ex);
        }
    }
    
    public async Task<Sequence?> GetSequenceByIdAsync(string id)
    {
        try
        {
            var sequence = await _sequenceRepository.GetByIdAsync(id);
            
            if (sequence == null)
            {
                _logger.LogWarning("Sequence with ID {Id} not found", id);
                return null;
            }
            
            return sequence;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sequence {Id}", id);
            throw new StorageProviderException($"Failed to retrieve sequence {id}", ex);
        }
    }
    
    // Additional implementation methods
}
```

### Specialized Services

#### SequenceGroupService

```csharp
public class SequenceGroupService
{
    private readonly ISequenceGroupRepository _sequenceGroupRepository;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly ILogger<SequenceGroupService> _logger;
    
    // Core methods
    public async Task<SequenceGroup> CreateSequenceGroupAsync(string name, string? description = null);
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

## Storage Providers

The library supports multiple storage backends through a provider abstraction:

```csharp
public interface IStorageProvider
{
    Task<bool> ExistsAsync();
    Task InitializeAsync();
    Task<string> GetStatusAsync();
}

public interface IStorageProvider<T> : IStorageProvider where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
}
```

### Implementations

#### Entity Framework Provider

```csharp
public class EfCoreStorageProvider<T> : IStorageProvider<T> where T : class
{
    protected readonly SchedulerDbContext _dbContext;
    
    public EfCoreStorageProvider(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<bool> ExistsAsync()
    {
        return await _dbContext.Database.CanConnectAsync();
    }
    
    public async Task InitializeAsync()
    {
        await _dbContext.Database.MigrateAsync();
    }
    
    // Implementation of CRUD operations
}
```

#### JSON Provider

```csharp
public class JsonStorageProvider<T> : IStorageProvider<T> where T : class
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    
    public JsonStorageProvider(string filePath)
    {
        _filePath = filePath;
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    // Implementation of CRUD operations
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

## Dependency Injection

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInstrumentData(
        this IServiceCollection services, 
        StorageConfiguration config)
    {
        // Register DbContext based on provider type
        switch (config.Provider)
        {
            case StorageProviderType.SQLite:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options.UseSqlite(config.ConnectionString));
                break;
                
            case StorageProviderType.SqlServer:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options.UseSqlServer(config.ConnectionString));
                break;
                
            case StorageProviderType.Json:
                // Register JSON-based repositories
                services.AddSingleton<JsonDataAdapter>(provider =>
                    new JsonDataAdapter(config.JsonFilePath));
                break;
        }
        
        // Register repositories
        services.AddScoped<ISequenceRepository, SequenceRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
        services.AddScoped<IRangeRepository, RangeRepository>();
        services.AddScoped<IRangeValueRepository, RangeValueRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<ISequenceGroupRepository, SequenceGroupRepository>();
        
        // Register services
        services.AddScoped<ISequenceService, SequenceService>();
        services.AddScoped<IParameterService, ParameterService>();
        services.AddScoped<IRangeService, RangeService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<SequenceGroupService>();
        
        return services;
    }
    
    // Other extension methods
}
```

## Initialization and Seeding

```csharp
public interface IDataInitializer
{
    Task<bool> ExistsAsync();
    Task InitializeAsync();
    Task<bool> MigrateAsync();
    Task<bool> SeedDefaultDataAsync();
    Task<string> GetStatusMessageAsync();
}

public class SqliteDatabaseInitializer : IDataInitializer
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ILogger<SqliteDatabaseInitializer> _logger;
    
    public SqliteDatabaseInitializer(
        SchedulerDbContext dbContext,
        ILogger<SqliteDatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<bool> ExistsAsync()
    {
        return await _dbContext.Database.CanConnectAsync();
    }
    
    public async Task InitializeAsync()
    {
        await _dbContext.Database.MigrateAsync();
    }
    
    // Implementation of other methods
}
```

## Usage Examples

### Basic Repository Usage

```csharp
// Get all sequences
public async Task<IEnumerable<Sequence>> GetAllSequencesAsync(ISequenceRepository repository)
{
    return await repository.GetAllAsync();
}

// Create a new sequence
public async Task<Sequence> CreateSequenceAsync(ISequenceRepository repository, string name)
{
    var sequence = new Sequence
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        WorstCaseTime = TimeSpan.FromSeconds(30),
        CanBeParallel = false
    };
    
    return await repository.AddAsync(sequence);
}
```

### Service Layer Usage

```csharp
// Using sequence service
public class SequenceManager
{
    private readonly ISequenceService _sequenceService;
    
    public SequenceManager(ISequenceService sequenceService)
    {
        _sequenceService = sequenceService;
    }
    
    public async Task<IEnumerable<Sequence>> GetAllSequencesAsync()
    {
        return await _sequenceService.GetAllSequencesAsync();
    }
    
    public async Task<Sequence> CreateSequenceAsync(string name, string description, bool canBeParallel)
    {
        var sequence = new Sequence
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            WorstCaseTime = TimeSpan.FromSeconds(30),
            CanBeParallel = canBeParallel
        };
        
        return await _sequenceService.CreateSequenceAsync(sequence);
    }
}
```

### Sequence Group Management

```csharp
// Using sequence group service
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
            "Sample Preparation", "Prepares samples for testing");
            
        // Add sequences to the group in a specific order
        await _sequenceGroupService.AddSequenceToGroupAsync(group.Id, "S001", 1);
        await _sequenceGroupService.AddSequenceToGroupAsync(group.Id, "S002", 2);
        
        // Get ordered sequences
        var orderedSequences = await _sequenceGroupService.GetOrderedSequencesAsync(group.Id);
    }
}
```

## See Also

- [Integration Guide](./integration-guide.md) - How to integrate with other application components
- [Presentation Layer Structure](./presentation-layer-structure.md) - UI layer design guidelines
