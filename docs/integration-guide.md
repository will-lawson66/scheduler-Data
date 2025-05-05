# Instrument.Data Integration Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Architectural Overview](#architectural-overview)
3. [Integration Process](#integration-process)
4. [Service Layer Integration](#service-layer-integration)
5. [Presentation Layer Integration](#presentation-layer-integration)
6. [Async Operations and Threading](#async-operations-and-threading)
7. [Error Handling Across Layers](#error-handling-across-layers)
8. [Advanced Integration](#advanced-integration)
9. [Performance Considerations](#performance-considerations)
10. [Integration Checklist](#integration-checklist)

## Introduction

Properly integrating the Instrument.Data library into your application requires understanding how the different architectural layers interact. This guide provides comprehensive instructions for integrating both the data access layer and the presentation layer, ensuring clean separation of concerns and efficient data flow.

## Architectural Overview

The Instrument.Data system follows a clean architecture approach with clear separation between layers:

```
┌───────────────────┐      ┌──────────────────┐      ┌───────────────────┐
│    Domain Layer   │      │  Application     │      │  Presentation     │
│                   │      │  Services        │      │  Layer            │
│  - Entities       │      │                  │      │                   │
│  - Value Objects  │◄────►│  - Use Cases     │◄────►│  - ViewModels     │
│  - Domain Services│      │  - Commands      │      │  - Views          │
│  - Events         │      │  - Queries       │      │  - UI Services    │
└───────────────────┘      └──────────────────┘      └───────────────────┘
```

The dependencies flow inward, with the presentation layer depending on services, and services depending on the domain model. This ensures that the inner layers remain independent of the outer layers, facilitating maintainability and testability.

## Integration Process

### Step 1: Add Package References

Add the Instrument.Data library to your application:

```xml
<ItemGroup>
  <ProjectReference Include="..\path\to\Instrument.Data\Instrument.Data.csproj" />
  <!-- Add additional project references as needed -->
</ItemGroup>
```

### Step 2: Configure Storage

Create a storage configuration in your application settings (e.g., appsettings.json):

```json
{
  "Storage": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=Instrument.db",
    "JsonFilePath": "data/instrument.json"
  }
}
```

### Step 3: Register Services

Configure the dependency injection container in your application startup:

```csharp
// Program.cs or Startup.cs
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 1. Configure storage
                var storageConfig = new StorageConfiguration
                {
                    Provider = Enum.Parse<StorageProviderType>(
                        context.Configuration["Storage:Provider"] ?? "SQLite"),
                    ConnectionString = context.Configuration["Storage:ConnectionString"] 
                        ?? "Data Source=Instrument.db",
                    JsonFilePath = context.Configuration["Storage:JsonFilePath"] 
                        ?? "data/instrument.json"
                };
                
                // 2. Register data layer
                services.AddInstrumentData(storageConfig);
                
                // 3. Register additional services
                services.AddDataInitialization();
                
                // 4. Register UI services (if applicable)
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                
                // 5. Register ViewModels (if applicable)
                services.AddTransient<MainViewModel>();
                services.AddTransient<SequencesViewModel>();
                services.AddTransient<SequenceDetailViewModel>();
                // ... other ViewModels
            })
            .Build();
            
        // Initialize application
        using (var scope = host.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<IDataInitializer>();
            initializer.InitializeAsync().Wait();
        }
        
        // Run application
        // ...
    }
}
```

### Step 4: Initialize Data

Set up data initialization for first-time usage:

```csharp
public class ApplicationInitializer
{
    private readonly IDataInitializer _dataInitializer;
    private readonly ILogger<ApplicationInitializer> _logger;
    
    public ApplicationInitializer(
        IDataInitializer dataInitializer,
        ILogger<ApplicationInitializer> logger)
    {
        _dataInitializer = dataInitializer;
        _logger = logger;
    }
    
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing application data...");
            
            if (!await _dataInitializer.ExistsAsync())
            {
                _logger.LogInformation("Database does not exist. Creating...");
                await _dataInitializer.InitializeAsync();
            }
            
            bool migrationsApplied = await _dataInitializer.MigrateAsync();
            if (migrationsApplied)
            {
                _logger.LogInformation("Database migrations applied successfully");
            }
            
            bool dataSeeded = await _dataInitializer.SeedDefaultDataAsync();
            if (dataSeeded)
            {
                _logger.LogInformation("Default data seeded successfully");
            }
            
            var status = await _dataInitializer.GetStatusMessageAsync();
            _logger.LogInformation("Database Status: {Status}", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize application data");
            throw;
        }
    }
}
```

## Service Layer Integration

The service layer provides the bridge between your application logic and the data layer. Here's how to integrate with the service layer:

### Service Interface Consumption

```csharp
public class SequenceManager
{
    private readonly ISequenceService _sequenceService;
    private readonly ILogger<SequenceManager> _logger;
    
    public SequenceManager(
        ISequenceService sequenceService,
        ILogger<SequenceManager> logger)
    {
        _sequenceService = sequenceService;
        _logger = logger;
    }
    
    public async Task<IEnumerable<Sequence>> GetSequencesAsync()
    {
        try
        {
            return await _sequenceService.GetAllSequencesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sequences");
            throw;
        }
    }
    
    public async Task<Sequence?> GetSequenceByIdAsync(string id)
    {
        try
        {
            return await _sequenceService.GetSequenceByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sequence {Id}", id);
            throw;
        }
    }
}
```

### Transaction Management

For operations involving multiple entities, implement a transaction management strategy:

```csharp
public class SequenceGroupManager
{
    private readonly SequenceGroupService _sequenceGroupService;
    private readonly ISequenceService _sequenceService;
    private readonly ILogger<SequenceGroupManager> _logger;
    
    // Constructor with dependency injection
    
    public async Task<bool> CreateCompleteSequenceGroupAsync(
        string name, string description, List<string> sequenceIds)
    {
        try
        {
            // Create the sequence group
            var group = await _sequenceGroupService.CreateSequenceGroupAsync(name, description);
            
            // Add all sequences to the group
            for (int i = 0; i < sequenceIds.Count; i++)
            {
                bool success = await _sequenceGroupService.AddSequenceToGroupAsync(
                    group.Id, sequenceIds[i], i + 1);
                
                if (!success)
                {
                    _logger.LogWarning("Failed to add sequence {SequenceId} to group {GroupId}",
                        sequenceIds[i], group.Id);
                    
                    // Rollback by removing the group
                    await _sequenceGroupService.DeleteSequenceGroupAsync(group.Id);
                    return false;
                }
            }
            
            // Validate the group
            return await _sequenceGroupService.ValidateSequenceGroupAsync(group.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sequence group with name {Name}", name);
            throw;
        }
    }
}
```

## Presentation Layer Integration

When integrating the data layer with a presentation layer (e.g., WPF with MVVM), follow these patterns:

### ViewModel Integration

ViewModels should use constructor injection to access services:

```csharp
public class SequencesViewModel : ViewModelBase
{
    private readonly ISequenceService _sequenceService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    
    private ObservableCollection<Sequence> _sequences;
    private Sequence? _selectedSequence;
    private bool _isLoading;
    
    public ObservableCollection<Sequence> Sequences
    {
        get => _sequences;
        set => SetProperty(ref _sequences, value);
    }
    
    public Sequence? SelectedSequence
    {
        get => _selectedSequence;
        set => SetProperty(ref _selectedSequence, value);
    }
    
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    public bool HasNoSequences => !Sequences.Any();
    
    public SequencesViewModel(
        ISequenceService sequenceService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _sequenceService = sequenceService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        
        Sequences = new ObservableCollection<Sequence>();
    }
    
    // Commands and methods to interact with services
}
```

### Command Implementation

Implement commands to invoke service methods:

```csharp
[RelayCommand]
private async Task LoadSequencesAsync()
{
    try
    {
        IsLoading = true;
        
        var sequences = await _sequenceService.GetAllSequencesAsync();
        
        Sequences.Clear();
        foreach (var sequence in sequences)
        {
            Sequences.Add(sequence);
        }
        
        OnPropertyChanged(nameof(HasNoSequences));
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("Error", $"Failed to load sequences: {ex.Message}");
    }
    finally
    {
        IsLoading = false;
    }
}

[RelayCommand]
private async Task SaveSequenceAsync()
{
    try
    {
        IsLoading = true;
        
        if (SelectedSequence == null)
            return;
        
        await _sequenceService.UpdateSequenceAsync(SelectedSequence);
        
        await _dialogService.ShowInformationAsync("Success", "Sequence updated successfully");
        _navigationService.GoBack();
    }
    catch (ValidationException ex)
    {
        await _dialogService.ShowWarningAsync("Validation Error", ex.Message);
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("Error", $"Failed to save sequence: {ex.Message}");
    }
    finally
    {
        IsLoading = false;
    }
}
```

### Navigation Integration

Implement navigation between views using a navigation service:

```csharp
public interface INavigationService
{
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    void GoBack();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _viewModelToViewMap;
    private readonly ContentControl _contentRegion;
    
    public NavigationService(
        IServiceProvider serviceProvider, 
        ContentControl contentRegion)
    {
        _serviceProvider = serviceProvider;
        _contentRegion = contentRegion;
        
        // Define mappings between ViewModels and Views
        _viewModelToViewMap = new Dictionary<Type, Type>
        {
            { typeof(SequencesViewModel), typeof(SequencesView) },
            { typeof(SequenceDetailViewModel), typeof(SequenceDetailView) },
            // Other mappings
        };
    }
    
    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        // Resolve the ViewModel from DI
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        
        // If ViewModel needs initialization with a parameter
        if (parameter != null && viewModel is IInitializable initializable)
        {
            initializable.Initialize(parameter);
        }
        
        // Create the View
        var viewType = _viewModelToViewMap[typeof(TViewModel)];
        var view = (UserControl)Activator.CreateInstance(viewType);
        
        // Set DataContext
        view.DataContext = viewModel;
        
        // Update the content region
        _contentRegion.Content = view;
    }
    
    public void GoBack()
    {
        // Navigation history management
    }
}
```

## Async Operations and Threading

When working with async operations across layers, manage threading properly, especially for UI updates:

### Dispatcher Integration

```csharp
private async Task LoadDataAsync()
{
    try
    {
        IsLoading = true;
        
        var sequences = await _sequenceService.GetAllSequencesAsync();
        
        // Use dispatcher to update UI collection from a background thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            Sequences.Clear();
            foreach (var sequence in sequences)
            {
                Sequences.Add(sequence);
            }
        });
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("Error", ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}
```

### Progress Reporting

For long-running operations, implement progress reporting:

```csharp
public interface IDataImportService
{
    Task ImportDataAsync(string filePath, IProgress<int> progress);
}

// In ViewModel:
[RelayCommand]
private async Task ImportDataAsync()
{
    var filePath = await _dialogService.ShowOpenFileDialogAsync("Select import file", "CSV files|*.csv");
    if (string.IsNullOrEmpty(filePath)) return;
    
    try
    {
        IsLoading = true;
        Progress = 0;
        IsProgressVisible = true;
        
        var progress = new Progress<int>(value => 
        {
            Progress = value;
        });
        
        await _dataImportService.ImportDataAsync(filePath, progress);
        
        await _dialogService.ShowInformationAsync("Success", "Data imported successfully");
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("Import Error", ex.Message);
    }
    finally
    {
        IsLoading = false;
        IsProgressVisible = false;
    }
}
```

## Error Handling Across Layers

Implement a consistent error handling strategy across layers:

### Domain-Specific Exceptions

Define domain-specific exceptions for common error cases:

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}

public class StorageProviderException : Exception
{
    public StorageProviderException(string message) : base(message) { }
    public StorageProviderException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

### Service Layer Error Handling

```csharp
public async Task<Sequence> GetSequenceByIdAsync(string id)
{
    try
    {
        var sequence = await _repository.GetByIdAsync(id);
        if (sequence == null)
        {
            throw new NotFoundException($"Sequence with ID {id} not found");
        }
        return sequence;
    }
    catch (DbUpdateException ex)
    {
        // Log the exception
        _logger.LogError(ex, "Database error retrieving sequence {Id}", id);
        
        // Translate to domain exception
        throw new StorageProviderException("A database error occurred", ex);
    }
}
```

### Presentation Layer Error Handling

```csharp
private async Task LoadSequenceAsync(string id)
{
    try
    {
        IsLoading = true;
        
        CurrentSequence = await _sequenceService.GetSequenceByIdAsync(id);
        
        if (CurrentSequence == null)
        {
            await _dialogService.ShowWarningAsync("Not Found", 
                $"Sequence with ID {id} could not be found");
            _navigationService.GoBack();
            return;
        }
    }
    catch (NotFoundException)
    {
        await _dialogService.ShowWarningAsync("Not Found", 
            $"Sequence with ID {id} could not be found");
        _navigationService.GoBack();
    }
    catch (StorageProviderException ex)
    {
        await _dialogService.ShowErrorAsync("Data Error", ex.Message);
        _navigationService.GoBack();
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("Unexpected Error", ex.Message);
        _navigationService.GoBack();
    }
    finally
    {
        IsLoading = false;
    }
}
```

## Advanced Integration

### Event-Driven Architecture

Implement an event-driven architecture for loose coupling between components:

```csharp
// Define domain events
public record SequenceCreatedEvent(Sequence Sequence);
public record SequenceUpdatedEvent(Sequence Sequence);
public record SequenceDeletedEvent(string SequenceId);

// Event publisher
public interface IEventPublisher
{
    void Publish<TEvent>(TEvent @event);
}

// Event subscriber
public interface IEventSubscriber<TEvent>
{
    void Handle(TEvent @event);
}

// In-memory implementation
public class InMemoryEventBus : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    
    public InMemoryEventBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public void Publish<TEvent>(TEvent @event)
    {
        var subscribers = _serviceProvider.GetServices<IEventSubscriber<TEvent>>();
        foreach (var subscriber in subscribers)
        {
            subscriber.Handle(@event);
        }
    }
}

// Integration in service
public class EventPublishingSequenceService : ISequenceService
{
    private readonly ISequenceService _innerService;
    private readonly IEventPublisher _eventPublisher;
    
    public EventPublishingSequenceService(
        ISequenceService innerService,
        IEventPublisher eventPublisher)
    {
        _innerService = innerService;
        _eventPublisher = eventPublisher;
    }
    
    public async Task<Sequence> CreateSequenceAsync(Sequence sequence)
    {
        var result = await _innerService.CreateSequenceAsync(sequence);
        _eventPublisher.Publish(new SequenceCreatedEvent(result));
        return result;
    }
    
    // Other methods with event publishing
}
```

### Caching Strategy

Implement caching for frequently accessed data:

```csharp
public class CachingSequenceService : ISequenceService
{
    private readonly ISequenceService _innerService;
    private readonly IMemoryCache _cache;
    
    public CachingSequenceService(
        ISequenceService innerService,
        IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
    }
    
    public async Task<IEnumerable<Sequence>> GetAllSequencesAsync()
    {
        return await _cache.GetOrCreateAsync("AllSequences", async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            return await _innerService.GetAllSequencesAsync();
        });
    }
    
    public async Task<Sequence?> GetSequenceByIdAsync(string id)
    {
        return await _cache.GetOrCreateAsync($"Sequence_{id}", async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            return await _innerService.GetSequenceByIdAsync(id);
        });
    }
    
    // Implement other methods with cache invalidation
    public async Task<Sequence> CreateSequenceAsync(Sequence sequence)
    {
        var result = await _innerService.CreateSequenceAsync(sequence);
        _cache.Remove("AllSequences");
        return result;
    }
}
```

## Performance Considerations

### Data Loading Strategies

Consider these strategies to optimize performance when loading data:

#### 1. Paging

Load data in chunks rather than all at once:

```csharp
public interface ISequenceService
{
    Task<PagedResult<Sequence>> GetPagedSequencesAsync(int page, int pageSize);
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageCount { get; set; }
}

// In ViewModel:
[RelayCommand]
private async Task LoadPageAsync(int page)
{
    try
    {
        IsLoading = true;
        
        var result = await _sequenceService.GetPagedSequencesAsync(page, PageSize);
        
        Sequences.Clear();
        foreach (var sequence in result.Items)
        {
            Sequences.Add(sequence);
        }
        
        TotalPages = result.PageCount;
        CurrentPage = page;
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("Error", ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}
```

#### 2. Lazy Loading

Only load details when needed:

```csharp
public class LazyLoadingViewModel : ViewModelBase
{
    private readonly ISequenceService _sequenceService;
    private readonly IParameterService _parameterService;
    
    private ObservableCollection<Sequence> _sequences;
    private ObservableCollection<Parameter> _parameters;
    private bool _parametersLoaded;
    
    // Properties and commands
    
    [RelayCommand]
    private async Task LoadParametersAsync()
    {
        if (_parametersLoaded)
            return;
            
        try
        {
            IsLoading = true;
            
            if (SelectedSequence == null)
                return;
                
            var parameters = await _parameterService.GetParametersBySequenceIdAsync(
                SelectedSequence.Id);
                
            Parameters.Clear();
            foreach (var parameter in parameters)
            {
                Parameters.Add(parameter);
            }
            
            _parametersLoaded = true;
        }
        catch (Exception ex)
        {
            // Error handling
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### Memory Management

To avoid memory leaks when integrating presentation and service layers:

```csharp
public class DataService : IDataService, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;
    
    public DataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
            
            _disposed = true;
        }
    }
}
```

## Integration Checklist

### Core Integration Checklist

✅ **Dependency Injection Setup**
- [ ] Services registered with appropriate lifetimes
- [ ] ViewModels registered as transient
- [ ] Navigation and dialog services registered as singletons
- [ ] Configuration values injected properly

✅ **Service Layer Integration**
- [ ] Services defined with clear interfaces
- [ ] Services handle validation and business logic
- [ ] Services throw domain-specific exceptions
- [ ] Services manage entity relationships appropriately

✅ **Presentation Layer Integration**
- [ ] ViewModels use constructor injection
- [ ] Commands properly call service methods
- [ ] Error handling consistent across ViewModels
- [ ] Loading indicators properly managed
- [ ] UI thread managed for collection updates

✅ **Async Operations**
- [ ] All service calls use async/await pattern
- [ ] UI updates respect the UI thread
- [ ] Progress reporting for long operations
- [ ] Cancellation support for user interruption

✅ **Error Handling**
- [ ] Domain-specific exceptions defined
- [ ] Exceptions handled at appropriate layers
- [ ] User-friendly error messages
- [ ] Error logging

### Best Practices

1. **Keep Services Focused**: Each service should address a specific domain concern
2. **Use Interfaces**: Always depend on interfaces, not concrete implementations
3. **Validate Early**: Validate data as early as possible, preferably in the service layer
4. **Handle Errors Gracefully**: Present user-friendly error messages, never raw exceptions
5. **Design for Testability**: Use DI and interfaces to make components testable in isolation
6. **Watch Threading**: Be mindful of thread context when updating UI from service calls
7. **Document Contracts**: Document service interfaces thoroughly for other developers
8. **Consider API Evolution**: Design services to be backward compatible as they evolve
9. **Maintain Separation of Concerns**: Don't let presentation logic leak into services
10. **Performance Monitoring**: Include performance monitoring in service implementations

## See Also

- [Core Data Layer](./core-data-layer.md) - Detailed documentation of the data layer architecture
- [Presentation Layer Structure](./presentation-layer-structure.md) - Guidelines for UI layer structure
- [WPF Material Design Guide](./wpf-material-design-guide.md) - Implementation details for UI with Material Design
