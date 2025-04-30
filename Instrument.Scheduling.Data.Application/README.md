# Scheduler Data Layer Demo Application

This application demonstrates the functionality of the Instrument.Scheduling.Data library, including the new SequenceGroup features.

## Features

This demo application provides an interactive menu to test various aspects of the data layer:

1. **Sequence Operations** - Create, read, update, and delete sequences
2. **Parameter Operations** - Create, read, update, and delete parameters
3. **Range Operations** - Create, read, update, and delete ranges and range values
4. **SequenceGroup Operations** - Create, manage, and manipulate sequence groups with ordered sequences
5. **Full Workflow Demo** - Demonstrates a complete workflow integrating all features
6. **Clear All Data** - Cleans up the database or JSON files

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQLite (for database mode) or file system access (for JSON mode)

### Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "DataStorage": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=./data/scheduler.db",
    "JsonFilePath": "./data/sequence_definitions.json"
  }
}
```

You can switch between SQLite and JSON storage by changing the `Provider` value to either `Sqlite` or `Json`.

### Running the Demo

1. Open a command prompt or terminal
2. Navigate to the project directory
3. Run `dotnet run`
4. Follow the on-screen menu options

## Demo Scenarios

### Sequence Group Features

The demo showcases the new SequenceGroup functionality:

- Creating sequence groups
- Adding sequences to groups in a specified order
- Retrieving ordered sequences from a group
- Reordering sequences within a group
- Removing sequences from a group
- Validating sequence groups according to business rules

### Full Workflow Demo

The full workflow demo demonstrates:

1. Creating sequences with parameters
2. Creating a sequence group template
3. Creating a "real" sequence group based on the template
4. Validating the sequence group
5. Displaying the final structure

## Architecture

This demo application interacts with the data layer using:

- **UnitOfWork Pattern** - For coordinated data access
- **Repository Pattern** - For entity-specific operations
- **Domain Services** - For business logic (like SequenceGroupService)
- **Entity Framework Core** - For SQLite/SQL Server persistence (configurable)

The data layer follows a domain-driven design approach with:

- **Entities** - Sequences, Parameters, Ranges, etc.
- **Value Objects** - Immutable objects with no identity
- **Repositories** - For data access
- **Domain Services** - For business logic

## Extension Points

The demo application can be extended to demonstrate additional features:

- Execution Plan generation from SequenceGroups
- Scheduling algorithms
- State management
- Event and message handling
