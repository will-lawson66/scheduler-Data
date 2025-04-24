# Scheduler Data Layer

A flexible, maintainable data access layer for managing sequence definitions and parameters in the Instrument Scheduler system. This library provides a repository pattern implementation with multiple storage provider options and support for complex entity relationships.

## Architecture

The Scheduler Data Layer follows a clean, layered architecture based on domain-driven design principles:

- **Entities**: Core domain models representing business concepts
- **Repositories**: Abstractions over data access operations
- **Storage Providers**: Implementations for different storage mechanisms
- **Services**: Application services that implement business logic

### Key Design Principles

- **Repository Pattern**: Abstracts data storage from business logic
- **Unit of Work**: Coordinates operations across repositories
- **Dependency Injection**: Provides flexible configuration and testability
- **Provider Abstraction**: Supports multiple storage mechanisms
- **Domain-Driven Design**: Focuses on core domain concepts and relationships

## Entity Model

### Sequence

Represents a defined sequence that can be scheduled and executed:

```csharp
public record Sequence
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public TimeSpan WorstCaseTime { get; init; } = TimeSpan.FromMilliseconds(30000);
    
    // Navigation property for parameters
    public List<SequenceParameter> SequenceParameters { get; init; } = new();
}
```

### Parameter

Represents a configurable parameter that can be associated with sequences:

```csharp
public record Parameter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ParameterType { get; init; }
    public string? DefaultValue { get; init; }
    public string? MinValue { get; init; }
    public string? MaxValue { get; init; }
    public bool Required { get; init; }
    public string? Description { get; init; }
    
    // Navigation property
    public List<SequenceParameter> SequenceParameters { get; init; } = new();
}
```

### SequenceParameter

Junction entity for the many-to-many relationship between Sequence and Parameter:

```csharp
public record SequenceParameter
{
    public required string SequenceId { get; init; }
    public required string ParameterId { get; init; }
    public string? OverrideValue { get; init; }
    
    // Navigation properties
    public Sequence Sequence { get; init; } = null!;
    public Parameter Parameter { get; init; } = null!;
}
```

## Repository Pattern

The repository pattern provides a consistent abstraction over data access:

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

### IParameterRepository

```csharp
public interface IParameterRepository
{
    Task<IEnumerable<Parameter>> GetAllAsync();
    Task<Parameter?> GetByIdAsync(string id);
    Task<IQueryable<Parameter>> GetQueryableAsync();
    Task AddAsync(Parameter parameter);
    Task UpdateAsync(Parameter parameter);
    Task DeleteAsync(string id);
    
    // Relationship management methods
    Task<IEnumerable<Parameter>> GetParametersForSequenceAsync(string sequenceId);
    Task AddParameterToSequenceAsync(string sequenceId, string parameterId, string? overrideValue = null);
    Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId);
    Task UpdateParameterOverrideValueAsync(string sequenceId, string parameterId, string? overrideValue);
    
    Task SaveChangesAsync();
}
```

### Unit of Work

Coordinates operations across multiple repositories:

```csharp
public interface IUnitOfWork : IDisposable
{
    ISequenceRepository SequenceDefinitions { get; }
    IParameterRepository Parameters { get; }
    Task<int> SaveChangesAsync();
}
```

## Storage Providers

The data layer supports multiple storage mechanisms through the `IStorageProvider<T>` interface:

```csharp
public interface IStorageProvider<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
    Task SaveChangesAsync();
}
```

### Implemented Providers

1. **JSON File Storage**
   - Simple file-based storage using System.Text.Json
   - Good for development, testing, and lightweight applications
   - Stores each entity type in a separate JSON file

2. **SQLite Storage**
   - Embedded relational database using Entity Framework Core
   - Supports complex queries and relationships
   - Ideal for applications that need relational features without a separate server

3. **SQL Server Storage**
   - Full-featured database storage using Entity Framework Core
   - Supports enterprise-level features and performance
   - Configured for use in production environments

## Database Schema

The database schema includes the following tables and relationships:

- **Sequences**: Stores sequence definitions
- **Parameters**: Stores parameter definitions with validation rules
- **SequenceParameters**: Junction table for the many-to-many relationship

The schema also includes support for future extensions:
- **Range**: Defines valid value sets for parameters
- **RangeValue**: Stores individual valid values within a range
- **Resource**: Represents system resources that parameters may reference

## Dependency Injection

Services can be registered with the dependency injection container using the extension methods provided:

```csharp
// Configure for JSON storage
services.AddSchedulerDataLayer(new StorageConfiguration
{
    Provider = StorageProviderType.Json,
    JsonFilePath = "sequence_definitions.json"
});

// Configure for SQLite
services.AddSchedulerDataLayer(new StorageConfiguration
{
    Provider = StorageProviderType.SQLite,
    ConnectionString = "Data Source=scheduler.db"
});

// Configure for SQL Server
services.AddSchedulerDataLayer(new StorageConfiguration
{
    Provider = StorageProviderType.SqlServer,
    ConnectionString = "Server=myserver;Database=Scheduler;Trusted_Connection=True;"
});
```

## Service Layer

The service layer provides business logic for working with sequences and parameters:

### SequenceService

```csharp
public class SequenceService
{
    private readonly IUnitOfWork _unitOfWork;

    // Methods for sequence CRUD operations
    public async Task<Sequence?> GetSequenceAsync(string id);
    public async Task CreateSequenceAsync(Sequence sequence);
    public async Task UpdateSequenceAsync(Sequence sequence);
    public async Task DeleteSequenceAsync(string id);
    public async Task<IEnumerable<Sequence>> GetAllSequencesAsync();
    public async Task<IEnumerable<Sequence>> SearchSequencesAsync(Func<Sequence, bool> predicate);
}
```

### ParameterService

```csharp
public class ParameterService
{
    private readonly IUnitOfWork _unitOfWork;

    // Parameter CRUD operations
    public async Task<Parameter?> GetParameterAsync(string id);
    public async Task CreateParameterAsync(Parameter parameter);
    public async Task UpdateParameterAsync(Parameter parameter);
    public async Task DeleteParameterAsync(string id);
    public async Task<IEnumerable<Parameter>> GetAllParametersAsync();
    
    // Relationship management
    public async Task<IEnumerable<Parameter>> GetParametersForSequenceAsync(string sequenceId);
    public async Task AddParameterToSequenceAsync(string sequenceId, string parameterId, string? overrideValue = null);
    public async Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId);
    public async Task UpdateParameterOverrideValueAsync(string sequenceId, string parameterId, string? overrideValue);
    
    // Parameter validation
    public bool ValidateParameterValue(Parameter parameter, string value);
}
```

## Usage Examples

### Creating and Managing Sequences

```csharp
// Get services from dependency injection
var sequenceService = serviceProvider.GetRequiredService<SequenceService>();

// Create a new sequence
var sequence = new Sequence
{
    Id = Guid.NewGuid().ToString(),
    Name = "Sample Processing",
    Description = "Basic sample processing sequence",
    WorstCaseTime = TimeSpan.FromSeconds(120)
};

await sequenceService.CreateSequenceAsync(sequence);

// Retrieve a sequence
var retrievedSequence = await sequenceService.GetSequenceAsync(sequence.Id);

// Update a sequence
var updatedSequence = retrievedSequence with { Description = "Updated description" };
await sequenceService.UpdateSequenceAsync(updatedSequence);
```

### Working with Parameters

```csharp
// Get services from dependency injection
var parameterService = serviceProvider.GetRequiredService<ParameterService>();

// Create a parameter
var parameter = new Parameter
{
    Id = Guid.NewGuid().ToString(),
    Name = "Temperature",
    ParameterType = "number",
    DefaultValue = "37.0",
    MinValue = "20.0",
    MaxValue = "60.0",
    Required = true,
    Description = "Operating temperature in degrees Celsius"
};

await parameterService.CreateParameterAsync(parameter);

// Associate parameter with a sequence
await parameterService.AddParameterToSequenceAsync(sequenceId, parameter.Id, "42.5");

// Get parameters for a sequence
var parameters = await parameterService.GetParametersForSequenceAsync(sequenceId);

// Validate a parameter value
bool isValid = parameterService.ValidateParameterValue(parameter, "45.0");
```

## Extending the Data Layer

The data layer is designed to be extensible. To add support for new entities:

1. Define the entity class
2. Create a repository interface and implementation
3. Add the repository to the Unit of Work
4. Update the DbContext for database providers
5. Register the new repository in the service collection extension

## Handling Complex Queries

For complex queries that span multiple entities, use one of these approaches:

1. **Application Service Layer**: Create a service that composes operations from multiple repositories
2. **Specialized Repository Methods**: Add domain-specific query methods to repositories
3. **Query Objects**: Create dedicated classes for complex queries

## Best Practices

- Use the Unit of Work pattern for coordinating operations across repositories
- Leverage the repository's GetQueryableAsync() for complex filtering and sorting
- Use the ParameterService's validation methods to ensure data integrity
- Consider performance implications when choosing storage providers
- Use dependency injection to easily switch between storage mechanisms

## Contributing

Contributions to the Scheduler Data Layer are welcome. Please follow the existing code style and patterns when implementing new features or fixing bugs.
