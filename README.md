# Scheduler Data Layer

This data layer implementation provides a flexible storage solution for sequence definitions that can work with different storage providers (JSON files, SQLite, SQL Server).

## Architecture Overview

The data layer follows a repository pattern with the following key components:

- **Storage Providers**: Abstract interface with implementations for different storage backends
- **Repository Pattern**: Abstraction over data access operations
- **Unit of Work**: Coordinates operations across repositories
- **Services**: Higher-level business logic for working with data

## Key Features

1. **Multiple Storage Providers**
   - JSON file storage for simple persistence
   - SQLite for lightweight database storage
   - SQL Server for enterprise scenarios

2. **Dependency Injection Support**
   - Easy configuration through extension methods
   - Runtime switching between storage providers

3. **Entity Framework Core Integration**
   - Database schema management
   - Migrations support
   - JSON serialization for complex properties

## Usage

### Configuration

```csharp
// Configure for SQLite
var config = new StorageConfiguration
{
    Provider = StorageProviderType.SQLite,
    ConnectionString = "Data Source=scheduler.db"
};

// Configure for JSON
var config = new StorageConfiguration
{
    Provider = StorageProviderType.Json,
    JsonFilePath = "sequence_definitions.json"
};

services.AddSchedulerDataLayer(config);
```

### Creating Sequences

```csharp
var sequence = new Sequence
{
    Id = Guid.NewGuid().ToString(),
    Name = "Test Sequence",
    Description = "A test sequence",
    Steps = new List<SequenceStep>(),
    Parameters = new Dictionary<string, object>()
};

await sequenceService.CreateSequenceAsync(sequence);
```

### Querying Data

```csharp
// Get all sequences
var sequences = await sequenceService.GetAllSequencesAsync();

// Search sequences
var results = await sequenceService.SearchSequencesAsync(
    s => s.Name.Contains("Test"));
```

## Files

- **Interfaces**: Define contracts for data access
- **Entities**: Domain models (Sequence, SequenceStep)
- **Providers**: Storage implementations
- **Repositories**: Data access logic
- **Services**: Business logic layer
- **Extensions**: DI configuration

## Dependencies

- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.EntityFrameworkCore.SqlServer
- System.Text.Json

## Migration Support

For SQLite and SQL Server implementations, you can use Entity Framework Core migrations:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
