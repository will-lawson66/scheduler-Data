# Instrument Scheduling System: Integration Guide

## Overview

The Instrument Scheduling System is a specialized component designed to manage laboratory test scheduling for automated instrument platforms. This module handles the transformation of test orders into executable sequences, optimizes execution plans, and manages hardware resource contention.

## Core Architecture

The Instrument Scheduling System follows domain-driven design principles with a clean, layered architecture:

```
┌───────────────────┐      ┌──────────────────┐      ┌───────────────────┐
│    Domain Layer   │      │  Application     │      │  Infrastructure   │
│                   │      │  Services        │      │  Layer            │
│  - Entities       │      │                  │      │                   │
│  - Value Objects  │◄────►│  - Use Cases     │◄────►│  - Repositories   │
│  - Domain Services│      │  - Commands      │      │  - Data Access    │
│  - Events         │      │  - Queries       │      │  - External APIs  │
└───────────────────┘      └──────────────────┘      └───────────────────┘
```

### Domain Entities

The core domain entities represent the business concepts central to laboratory test scheduling:

- **Sequence**: Discrete operations performed by the instrument
- **Parameter**: Configurable values with type validation and ranges
- **SequenceGroup**: Ordered collections of sequences for complete test procedures
- **Resource**: Physical or logical components required for operations
- **ExecutionPlan**: Generated plan for a specific test order/replicate
- **Schedule**: A comprehensive solution for multiple execution plans

### Data Management

The system employs a flexible data management approach with multiple storage options:

- **Data Immutability**: Entities are implemented as immutable records for thread safety
- **Repository Pattern**: Generic repositories with specialized implementations
- **Unit of Work**: Coordinates operations across repositories
- **Storage Providers**: Supports JSON, SQLite, and SQL Server

## Integration Process

### Step 1: Add Package References

Add the Instrument Scheduling System to your application:

```xml
<ItemGroup>
  <ProjectReference Include="..\path\to\Instrument.Scheduling.Data\Instrument.Scheduling.Data.csproj" />
  <!-- Add additional project references as needed -->
</ItemGroup>
```

### Step 2: Configure Storage

Create a storage configuration in your application settings:

```json
{
  "SchedulerStorage": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=scheduler.db",
    "JsonFilePath": "data/sequences.json"
  }
}
```

### Step 3: Register Services

Configure the dependency injection container in your application startup:

```csharp
// Configure scheduler services
services.AddSchedulerDataLayer(new StorageConfiguration
{
    Provider = StorageProviderType.SQLite,
    ConnectionString = Configuration.GetConnectionString("SchedulerDb"),
    JsonFilePath = Configuration.GetValue<string>("SchedulerStorage:JsonFilePath")
});

// Add additional scheduler services
services.AddScoped<ExecutionPlanService>();
services.AddScoped<SchedulerService>();
```

### Step 4: Initialize Data 

Set up data initialization for the scheduler:

```csharp
public class SchedulerInitializer
{
    private readonly DataInitializerFactory _initializerFactory;
    private readonly IServiceProvider _serviceProvider;
    
    public SchedulerInitializer(
        DataInitializerFactory initializerFactory,
        IServiceProvider serviceProvider)
    {
        _initializerFactory = initializerFactory;
        _serviceProvider = serviceProvider;
    }
    
    public async Task InitializeAsync(StorageConfiguration config)
    {
        var initializer = _initializerFactory.CreateInitializer(
            config.Provider, _serviceProvider);
        
        await initializer.InitializeAsync();
    }
}
```

## Usage Examples

### Working with Sequences

```csharp
// Get service from DI
var sequenceGroupService = serviceProvider.GetRequiredService<SequenceGroupService>();

// Create a new sequence group
var sequenceGroup = await sequenceGroupService.CreateSequenceGroupAsync(
    "SG001", "Sample Preparation", "Prepares samples for analysis");
    
// Add sequences to the group
await sequenceGroupService.AddSequenceToGroupAsync("SG001", "S001", 1);
await sequenceGroupService.AddSequenceToGroupAsync("SG001", "S002", 2);

// Retrieve ordered sequences
var orderedSequences = await sequenceGroupService.GetOrderedSequencesAsync("SG001");
```

### Creating an Execution Plan

```csharp
// Use the execution plan service to create plans from test orders
var executionPlanService = serviceProvider.GetRequiredService<ExecutionPlanService>();

// Create execution plans for each test order
var testOrders = await testOrderRepository.GetAllAsync();
var executionPlans = await executionPlanService.CreateExecutionPlansAsync(testOrders);
```

### Generating a Schedule

```csharp
// Use the scheduler service to create a schedule from execution plans
var schedulerService = serviceProvider.GetRequiredService<SchedulerService>();

// Generate an optimized schedule
var schedule = await schedulerService.GenerateScheduleAsync(
    executionPlans, schedulingAlgorithm);
    
// Get a period-based view of the schedule
var periodTasks = schedule.GetTasksByPeriod();
```

## Advanced Integration

### Event-Driven Architecture

The scheduler supports an event-driven approach for integration with other system components:

```csharp
// Register event handlers
services.AddScoped<ITestOrderEventHandler, TestOrderEventHandler>();
services.AddScoped<IScheduleCreatedEventHandler, ScheduleCreatedEventHandler>();

// Configure event bus
services.AddEventBus(options => {
    options.UseInMemoryBus();
    // or
    options.UseMessageBroker(Configuration.GetSection("MessageBroker"));
});
```

### State Management

Implement state tracking for scheduled operations:

```csharp
// Configure state services
services.AddScoped<IStateService, StateService>();

// Initialize state machines
var stateConfig = new StateConfiguration
{
    InitialState = "Pending",
    States = new[] { "Pending", "InProgress", "Completed", "Failed" },
    Transitions = new[] {
        new Transition("Pending", "InProgress", "Start"),
        new Transition("InProgress", "Completed", "Complete"),
        new Transition("InProgress", "Failed", "Fail"),
        // Additional transitions
    }
};

// Register state machines
services.AddStateMachine<TestOrderState>(stateConfig);
services.AddStateMachine<ScheduleState>(scheduleStateConfig);
```

## Integration with Execution Layer

### Sending Schedule to Execution Engine

```csharp
// Get execution client
var executionClient = serviceProvider.GetRequiredService<IExecutionClient>();

// Send schedule to execution engine
var result = await executionClient.SubmitScheduleAsync(schedule);

// Monitor execution progress
executionClient.SubscribeToExecutionEvents(events => {
    // Handle execution events
    foreach (var evt in events)
    {
        if (evt.Type == "PeriodCompleted")
        {
            // Update state based on period completion
            stateService.UpdatePeriodState(evt.PeriodId, "Completed");
        }
    }
});
```

### Sequence Translation

When integrating with an existing instrument control system, sequence translation is often required:

```csharp
// Configure sequence translation
services.AddSequenceTranslator(options => {
    options.RegisterTranslator<ChemistryTechnology>(
        new ChemistrySequenceTranslator());
    options.RegisterTranslator<ImmunologyTechnology>(
        new ImmunologySequenceTranslator());
});

// Use the translator
var translator = serviceProvider.GetRequiredService<ISequenceTranslator>();
var translatedSequences = await translator.TranslateAsync(
    testOrder, testOrder.Technology);
```

## Performance Considerations

1. **Immutable Collections**: The scheduler uses immutable collections for thread safety. For large datasets, consider pagination or windowing approaches.

2. **Database Selection**: 
   - SQLite: Suitable for standalone deployments or small-scale operations
   - SQL Server: Recommended for production environments with high concurrency

3. **Caching Strategy**: Implement caching for frequently accessed data:
   ```csharp
   services.AddMemoryCache();
   services.Decorate<ISequenceGroupRepository, CachedSequenceGroupRepository>();
   ```

4. **Execution Plan Optimization**: Tune scheduling algorithms based on instrument capabilities and throughput requirements.

## Error Handling and Resilience

Implement comprehensive error handling:

```csharp
try
{
    var schedule = await schedulerService.GenerateScheduleAsync(executionPlans);
    // Process schedule
}
catch (ResourceContentionException ex)
{
    // Handle resource contention
    logger.LogWarning(ex, "Resource contention detected");
    
    // Apply fallback strategy
    var alternativeSchedule = await schedulerService.GenerateScheduleWithFallbackAsync(
        executionPlans, FallbackStrategy.DelayNonCritical);
}
catch (Exception ex)
{
    // Handle general errors
    logger.LogError(ex, "Failed to generate schedule");
    // Notify operators or trigger fallback process
}
```

## Summary

The Instrument Scheduling System provides a robust foundation for laboratory test scheduling with a flexible architecture that can adapt to various integration scenarios. By following this guide, you can effectively integrate the scheduler into your larger instrument control application while leveraging its advanced features for optimized test execution.

Key integration points to focus on:
- Data layer configuration
- Service registration
- Event handling
- State management
- Execution layer communication

With proper integration, the scheduler can significantly enhance laboratory workflows by optimizing resource utilization, reducing execution time, and improving overall instrument throughput.
