# Integrating Presentation Layer with Service Layer: A Comprehensive Guide

## Introduction

Properly integrating the presentation layer (UI) with the service layer is critical for maintaining separation of concerns while ensuring efficient data flow in a WPF MVVM application. This document provides a detailed exploration of how to connect these architectural layers in the Instrument.Data.UI project, focusing on best practices, dependency injection patterns, and practical implementation strategies.

## Table of Contents

1. [Architectural Overview](#architectural-overview)
2. [Dependency Injection Framework](#dependency-injection-framework)
3. [Service Layer Design](#service-layer-design)
4. [ViewModel Integration Patterns](#viewmodel-integration-patterns)
5. [Async Operations and Threading](#async-operations-and-threading)
6. [Error Handling Across Layers](#error-handling-across-layers)
7. [Unit Testing Integration Points](#unit-testing-integration-points)
8. [Practical Implementation Examples](#practical-implementation-examples)
9. [Performance Considerations](#performance-considerations)
10. [Integration Checklist and Best Practices](#integration-checklist-and-best-practices)

## Architectural Overview

In the Instrument.Data application, we have a clear separation between the presentation and data layers:

1. **Presentation Layer** (Instrument.Data.UI)
   - Views (XAML)
   - ViewModels (C# classes)
   - UI-specific services (DialogService, NavigationService)

2. **Service Layer** (Instrument.Data)
   - Domain services (SequenceService, ParameterService, etc.)
   - Repository interfaces and implementations
   - Entity Framework contexts and configurations
   - Domain models and entities

The separation between these layers follows a clean architecture approach, with dependencies flowing inward from presentation to data. The service layer should never depend on the presentation layer, ensuring proper decoupling.

```
Presentation Layer (UI) → Service Layer → Repository Layer → Data Storage
```

## Dependency Injection Framework

### Container Setup

The Microsoft.Extensions.DependencyInjection framework provides the foundation for connecting the layers. Configure this in Program.cs:

```csharp
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 1. Register data context
                services.AddDbContext<SchedulerDbContext>(options =>
                    options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));
                
                // 2. Register repositories
                services.AddScoped<ISequenceRepository, SequenceRepository>();
                services.AddScoped<IParameterRepository, ParameterRepository>();
                services.AddScoped<IRangeRepository, RangeRepository>();
                services.AddScoped<IResourceRepository, ResourceRepository>();
                
                // 3. Register service layer
                services.AddScoped<ISequenceService, SequenceService>();
                services.AddScoped<IParameterService, ParameterService>();
                services.AddScoped<IRangeService, RangeService>();
                services.AddScoped<IResourceService, ResourceService>();
                
                // 4. Register UI services
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                
                // 5. Register ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<SequencesViewModel>();
                services.AddTransient<SequenceDetailViewModel>();
                services.AddTransient<ParametersViewModel>();
                services.AddTransient<ParameterDetailViewModel>();
                // ... other ViewModels
            })
            .Build();
        
        var app = new App();
        app.InitializeComponent();
        
        // Set startup window with MainViewModel
        var mainWindow = new MainWindow
        {
            DataContext = host.Services.GetRequiredService<MainViewModel>()
        };
        
        app.MainWindow = mainWindow;
        app.MainWindow.Show();
        
        app.Run();
    }
}
```

### Service Registration Order and Lifetimes

The registration order is important for understanding the dependencies:

1. **DbContext**: Registered first as it's needed by repositories
2. **Repositories**: Depend on DbContext
3. **Services**: Depend on repositories
4. **UI Services**: Support the presentation layer
5. **ViewModels**: Consume domain services

Service lifetimes should be chosen based on their nature:

- **Transient** (`AddTransient<T>`): Created each time they're requested
  - Use for ViewModels to ensure fresh instances each time a view is opened
  - Example: `services.AddTransient<SequenceDetailViewModel>();`

- **Scoped** (`AddScoped<T>`): Created once per scope (typically per HTTP request in web apps, but in WPF this effectively means one instance during a logical operation)
  - Use for services with database/stateful dependencies
  - Example: `services.AddScoped<ISequenceService, SequenceService>();`

- **Singleton** (`AddSingleton<T>`): Created once for the application lifetime
  - Use for stateless services or those that must maintain state application-wide
  - Example: `services.AddSingleton<INavigationService, NavigationService>();`

## Service Layer Design

### Service Interfaces

Service interfaces should define clear contracts that ViewModels can depend on:

```csharp
public interface ISequenceService
{
    Task<IEnumerable<Sequence>> GetAllAsync();
    Task<Sequence> GetByIdAsync(string id);
    Task<Sequence> CreateAsync(Sequence sequence);
    Task UpdateAsync(Sequence sequence);
    Task DeleteAsync(string id);
}
```

Key principles for service interfaces:
- Focus on business operations, not technical details
- Use async methods for database operations
- Return domain entities, not data transfer objects
- Handle validation and business rules

### Service Implementation

A typical service implementation connects to the repository layer while implementing business logic:

```csharp
public class SequenceService : ISequenceService
{
    private readonly ISequenceRepository _repository;
    private readonly ISequenceParameterRepository _parameterRepository;
    
    public SequenceService(
        ISequenceRepository repository,
        ISequenceParameterRepository parameterRepository)
    {
        _repository = repository;
        _parameterRepository = parameterRepository;
    }
    
    public async Task<IEnumerable<Sequence>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
    
    public async Task<Sequence> GetByIdAsync(string id)
    {
        var sequence = await _repository.GetByIdAsync(id);
        if (sequence == null)
        {
            throw new NotFoundException($"Sequence with ID {id} not found");
        }
        
        // Load related parameters
        sequence.SequenceParameters = await _parameterRepository
            .GetBySequenceIdAsync(id);
            
        return sequence;
    }
    
    public async Task<Sequence> CreateAsync(Sequence sequence)
    {
        // Validate
        if (string.IsNullOrEmpty(sequence.Name))
        {
            throw new ValidationException("Sequence name is required");
        }
        
        // Generate new ID if not provided
        if (string.IsNullOrEmpty(sequence.Id))
        {
            sequence.Id = Guid.NewGuid().ToString();
        }
        
        return await _repository.AddAsync(sequence);
    }
    
    public async Task UpdateAsync(Sequence sequence)
    {
        // Validate
        if (string.IsNullOrEmpty(sequence.Name))
        {
            throw new ValidationException("Sequence name is required");
        }
        
        // Check existence
        var existing = await _repository.GetByIdAsync(sequence.Id);
        if (existing == null)
        {
            throw new NotFoundException($"Sequence with ID {sequence.Id} not found");
        }
        
        await _repository.UpdateAsync(sequence);
    }
    
    public async Task DeleteAsync(string id)
    {
        var sequence = await _repository.GetByIdAsync(id);
        if (sequence == null)
        {
            throw new NotFoundException($"Sequence with ID {id} not found");
        }
        
        await _repository.DeleteAsync(id);
    }
}
```

Service implementations should:
- Validate input data before processing
- Check for existence when updating or deleting
- Maintain entity relationships when needed
- Handle complex operations that span multiple repositories
- Throw domain-specific exceptions for error conditions

## ViewModel Integration Patterns

### Constructor Injection

The preferred pattern for connecting ViewModels to services is constructor injection:

```csharp
public class SequencesViewModel : ViewModelBase
{
    private readonly ISequenceService _sequenceService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    
    private ObservableCollection<Sequence> _sequences;
    private bool _isLoading;
    
    public ObservableCollection<Sequence> Sequences
    {
        get => _sequences;
        set => SetProperty(ref _sequences, value);
    }
    
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
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
    
    // Commands and methods that use the injected services
}
```

Benefits of constructor injection:
- Makes dependencies explicit
- Facilitates unit testing
- Works seamlessly with DI containers
- Enforces proper initialization

### ViewModel Service Consumption Pattern

ViewModels should follow a consistent pattern for consuming services:

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    try
    {
        IsLoading = true;
        
        var sequences = await _sequenceService.GetAllAsync();
        
        Sequences.Clear();
        foreach (var sequence in sequences)
        {
            Sequences.Add(sequence);
        }
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("Error loading sequences", ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}

[RelayCommand]
private async Task SaveAsync()
{
    try
    {
        IsLoading = true;
        
        if (IsNewSequence)
        {
            await _sequenceService.CreateAsync(CurrentSequence);
            await _dialogService.ShowInformationAsync("Success", "Sequence created successfully");
        }
        else
        {
            await _sequenceService.UpdateAsync(CurrentSequence);
            await _dialogService.ShowInformationAsync("Success", "Sequence updated successfully");
        }
        
        // Navigate back to list
        _navigationService.NavigateTo<SequencesViewModel>();
    }
    catch (ValidationException ex)
    {
        await _dialogService.ShowWarningAsync("Validation Error", ex.Message);
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

This pattern:
- Uses try/catch/finally blocks for robust error handling
- Sets loading indicators during service operations
- Displays appropriate messages for different error types
- Updates UI collections safely
- Handles navigation after successful operations

### Data Loading Strategy

Consider these two approaches for loading data in ViewModels:

1. **Eager Loading**: Load all data when ViewModel is constructed

```csharp
public SequencesViewModel(ISequenceService sequenceService)
{
    _sequenceService = sequenceService;
    Sequences = new ObservableCollection<Sequence>();
    
    // Load data immediately
    _ = LoadDataAsync();
}

private async Task LoadDataAsync()
{
    // Implementation
}
```

2. **Lazy Loading**: Load data only when explicitly requested

```csharp
public SequencesViewModel(ISequenceService sequenceService)
{
    _sequenceService = sequenceService;
    Sequences = new ObservableCollection<Sequence>();
    
    // Define command but don't execute
    LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
}

// Execute this command when the View is loaded
public IAsyncRelayCommand LoadDataCommand { get; }

private async Task LoadDataAsync()
{
    // Implementation
}
```

The choice depends on:
- Performance requirements (lazy loading is better for heavy data)
- User experience (eager loading shows data faster)
- Navigation patterns (eager loading works well with cache-first approaches)

## Async Operations and Threading

### Dispatcher Integration

Since service operations run asynchronously but UI updates must occur on the UI thread, use the dispatcher when updating ObservableCollections:

```csharp
private async Task LoadDataAsync()
{
    try
    {
        IsLoading = true;
        
        var sequences = await _sequenceService.GetAllAsync();
        
        // Use dispatcher to update UI collection
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

### Domain-Specific Exceptions

Define a set of domain-specific exceptions in the service layer:

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
```

### Exception Handling Strategy

Implement a consistent exception handling strategy across layers:

1. **Repository Layer**: Catch data access exceptions, translate to domain exceptions
2. **Service Layer**: Validate input, handle business rules, throw domain exceptions
3. **ViewModel Layer**: Catch domain exceptions, translate to user-friendly messages

Example service method with proper exception handling:

```csharp
public async Task<Sequence> GetByIdAsync(string id)
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
        throw new DataAccessException("A database error occurred", ex);
    }
}
```

Example ViewModel method with error handling:

```csharp
private async Task LoadSequenceAsync(string id)
{
    try
    {
        IsLoading = true;
        
        CurrentSequence = await _sequenceService.GetByIdAsync(id);
    }
    catch (NotFoundException)
    {
        await _dialogService.ShowWarningAsync("Not Found", $"Sequence with ID {id} could not be found");
        _navigationService.GoBack();
    }
    catch (DataAccessException ex)
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

## Unit Testing Integration Points

### Testing Service Consumption in ViewModels

Unit tests for ViewModels should verify proper service integration:

```csharp
[Fact]
public async Task LoadDataCommand_ShouldPopulateSequencesCollection()
{
    // Arrange
    var mockSequenceService = new Mock<ISequenceService>();
    mockSequenceService.Setup(s => s.GetAllAsync())
        .ReturnsAsync(new List<Sequence>
        {
            new Sequence { Id = "1", Name = "Sequence 1" },
            new Sequence { Id = "2", Name = "Sequence 2" }
        });
    
    var mockNavigationService = new Mock<INavigationService>();
    var mockDialogService = new Mock<IDialogService>();
    
    var viewModel = new SequencesViewModel(
        mockSequenceService.Object,
        mockNavigationService.Object,
        mockDialogService.Object);
    
    // Act
    await viewModel.LoadDataCommand.ExecuteAsync(null);
    
    // Assert
    Assert.Equal(2, viewModel.Sequences.Count);
    Assert.Equal("Sequence 1", viewModel.Sequences[0].Name);
    Assert.Equal("Sequence 2", viewModel.Sequences[1].Name);
    Assert.False(viewModel.IsLoading);
}

[Fact]
public async Task SaveCommand_WhenValidationFails_ShouldShowWarningDialog()
{
    // Arrange
    var mockSequenceService = new Mock<ISequenceService>();
    mockSequenceService.Setup(s => s.UpdateAsync(It.IsAny<Sequence>()))
        .ThrowsAsync(new ValidationException("Name is required"));
    
    var mockNavigationService = new Mock<INavigationService>();
    var mockDialogService = new Mock<IDialogService>();
    
    var viewModel = new SequenceDetailViewModel(
        mockSequenceService.Object,
        mockNavigationService.Object,
        mockDialogService.Object);
    
    viewModel.CurrentSequence = new Sequence { Id = "1" };
    viewModel.IsNewSequence = false;
    
    // Act
    await viewModel.SaveCommand.ExecuteAsync(null);
    
    // Assert
    mockDialogService.Verify(d => d.ShowWarningAsync(
        It.Is<string>(s => s.Contains("Validation")),
        It.Is<string>(s => s.Contains("Name is required"))),
        Times.Once);
    
    mockNavigationService.Verify(n => n.NavigateTo<SequencesViewModel>(),
        Times.Never);
}
```

## Practical Implementation Examples

### Sequence Management

Here's a complete integration example for Sequence management:

**1. Service Interface (Instrument.Data)**

```csharp
public interface ISequenceService
{
    Task<IEnumerable<Sequence>> GetAllAsync();
    Task<Sequence> GetByIdAsync(string id);
    Task<Sequence> CreateAsync(Sequence sequence);
    Task UpdateAsync(Sequence sequence);
    Task DeleteAsync(string id);
    Task<IEnumerable<Parameter>> GetAvailableParametersAsync();
    Task AddParameterToSequenceAsync(string sequenceId, string parameterId);
    Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId);
}
```

**2. Service Implementation (Instrument.Data)**

```csharp
public class SequenceService : ISequenceService
{
    private readonly ISequenceRepository _sequenceRepository;
    private readonly IParameterRepository _parameterRepository;
    private readonly ISequenceParameterRepository _sequenceParameterRepository;
    
    public SequenceService(
        ISequenceRepository sequenceRepository,
        IParameterRepository parameterRepository,
        ISequenceParameterRepository sequenceParameterRepository)
    {
        _sequenceRepository = sequenceRepository;
        _parameterRepository = parameterRepository;
        _sequenceParameterRepository = sequenceParameterRepository;
    }
    
    // Implementation of all methods with proper validation and error handling
    
    public async Task<IEnumerable<Parameter>> GetAvailableParametersAsync()
    {
        return await _parameterRepository.GetAllAsync();
    }
    
    public async Task AddParameterToSequenceAsync(string sequenceId, string parameterId)
    {
        var sequence = await _sequenceRepository.GetByIdAsync(sequenceId);
        if (sequence == null)
        {
            throw new NotFoundException($"Sequence with ID {sequenceId} not found");
        }
        
        var parameter = await _parameterRepository.GetByIdAsync(parameterId);
        if (parameter == null)
        {
            throw new NotFoundException($"Parameter with ID {parameterId} not found");
        }
        
        var sequenceParameter = new SequenceParameter
        {
            SequenceId = sequenceId,
            ParameterId = parameterId
        };
        
        await _sequenceParameterRepository.AddAsync(sequenceParameter);
    }
    
    public async Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId)
    {
        var sequenceParameter = await _sequenceParameterRepository
            .GetBySequenceAndParameterIdAsync(sequenceId, parameterId);
            
        if (sequenceParameter == null)
        {
            throw new NotFoundException(
                $"Parameter {parameterId} not found in sequence {sequenceId}");
        }
        
        await _sequenceParameterRepository.DeleteAsync(
            sequenceParameter.SequenceId, sequenceParameter.ParameterId);
    }
}
```

**3. ViewModel Integration (Instrument.Data.UI)**

```csharp
public class SequenceDetailViewModel : ViewModelBase
{
    private readonly ISequenceService _sequenceService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    
    private Sequence _currentSequence;
    private ObservableCollection<Parameter> _availableParameters;
    private ObservableCollection<Parameter> _sequenceParameters;
    private Parameter _selectedAvailableParameter;
    private Parameter _selectedSequenceParameter;
    private bool _isNewSequence;
    
    public Sequence CurrentSequence
    {
        get => _currentSequence;
        set => SetProperty(ref _currentSequence, value);
    }
    
    public ObservableCollection<Parameter> AvailableParameters
    {
        get => _availableParameters;
        set => SetProperty(ref _availableParameters, value);
    }
    
    public ObservableCollection<Parameter> SequenceParameters
    {
        get => _sequenceParameters;
        set => SetProperty(ref _sequenceParameters, value);
    }
    
    public Parameter SelectedAvailableParameter
    {
        get => _selectedAvailableParameter;
        set => SetProperty(ref _selectedAvailableParameter, value);
    }
    
    public Parameter SelectedSequenceParameter
    {
        get => _selectedSequenceParameter;
        set => SetProperty(ref _selectedSequenceParameter, value);
    }
    
    public bool IsNewSequence
    {
        get => _isNewSequence;
        set => SetProperty(ref _isNewSequence, value);
    }
    
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand AddParameterCommand { get; }
    public IAsyncRelayCommand RemoveParameterCommand { get; }
    
    public SequenceDetailViewModel(
        ISequenceService sequenceService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _sequenceService = sequenceService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        
        AvailableParameters = new ObservableCollection<Parameter>();
        SequenceParameters = new ObservableCollection<Parameter>();
        
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        AddParameterCommand = new AsyncRelayCommand(AddParameterAsync, CanAddParameter);
        RemoveParameterCommand = new AsyncRelayCommand(RemoveParameterAsync, CanRemoveParameter);
    }
    
    public async Task InitializeAsync(string sequenceId = null)
    {
        try
        {
            IsLoading = true;
            
            if (string.IsNullOrEmpty(sequenceId))
            {
                // New sequence
                IsNewSequence = true;
                CurrentSequence = new Sequence
                {
                    Id = Guid.NewGuid().ToString(),
                    WorstCaseTime = TimeSpan.FromMinutes(1)
                };
            }
            else
            {
                // Existing sequence
                IsNewSequence = false;
                CurrentSequence = await _sequenceService.GetByIdAsync(sequenceId);
                
                // Load sequence parameters
                if (CurrentSequence.SequenceParameters != null)
                {
                    foreach (var sp in CurrentSequence.SequenceParameters)
                    {
                        SequenceParameters.Add(sp.Parameter);
                    }
                }
            }
            
            // Load available parameters
            var allParameters = await _sequenceService.GetAvailableParametersAsync();
            foreach (var parameter in allParameters)
            {
                // Only add parameters not already in the sequence
                if (!SequenceParameters.Any(p => p.Id == parameter.Id))
                {
                    AvailableParameters.Add(parameter);
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", ex.Message);
            _navigationService.GoBack();
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;
            
            if (IsNewSequence)
            {
                await _sequenceService.CreateAsync(CurrentSequence);
            }
            else
            {
                await _sequenceService.UpdateAsync(CurrentSequence);
            }
            
            _navigationService.NavigateTo<SequencesViewModel>();
        }
        catch (ValidationException ex)
        {
            await _dialogService.ShowWarningAsync("Validation Error", ex.Message);
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
    
    private async Task CancelAsync()
    {
        _navigationService.GoBack();
    }
    
    private async Task AddParameterAsync()
    {
        if (SelectedAvailableParameter == null) return;
        
        try
        {
            IsLoading = true;
            
            await _sequenceService.AddParameterToSequenceAsync(
                CurrentSequence.Id, SelectedAvailableParameter.Id);
            
            // Update collections
            SequenceParameters.Add(SelectedAvailableParameter);
            AvailableParameters.Remove(SelectedAvailableParameter);
            SelectedAvailableParameter = null;
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
    
    private bool CanAddParameter() => SelectedAvailableParameter != null;
    
    private async Task RemoveParameterAsync()
    {
        if (SelectedSequenceParameter == null) return;
        
        try
        {
            IsLoading = true;
            
            await _sequenceService.RemoveParameterFromSequenceAsync(
                CurrentSequence.Id, SelectedSequenceParameter.Id);
            
            // Update collections
            AvailableParameters.Add(SelectedSequenceParameter);
            SequenceParameters.Remove(SelectedSequenceParameter);
            SelectedSequenceParameter = null;
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
    
    private bool CanRemoveParameter() => SelectedSequenceParameter != null;
}
```

**4. Navigation Integration**

```csharp
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
        
        // Define the mapping between ViewModels and Views
        _viewModelToViewMap = new Dictionary<Type, Type>
        {
            { typeof(SequencesViewModel), typeof(SequencesView) },
            { typeof(SequenceDetailViewModel), typeof(SequenceDetailView) },
            // Other mappings
        };
    }
    
    public void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
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
}

// Interface for ViewModel initialization
public interface IInitializable
{
    void Initialize(object parameter);
}

// Implementation in SequenceDetailViewModel
public class SequenceDetailViewModel : ViewModelBase, IInitializable
{
    // Other members
    
    public void Initialize(object parameter)
    {
        if (parameter is string sequenceId)
        {
            // Start asynchronous initialization
            _ = InitializeAsync(sequenceId);
        }
        else
        {
            // Initialize as new sequence
            _ = InitializeAsync();
        }
    }
}
```

## Performance Considerations

### Data Loading Strategies

Consider these strategies to optimize performance when loading data:

1. **Paging**: Load data in chunks rather than all at once

```csharp
public interface ISequenceService
{
    Task<PagedResult<Sequence>> GetPagedAsync(int page, int pageSize);
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageCount { get; set; }
}

// In ViewModel:
public int CurrentPage { get; set; } = 1;
public int PageSize { get; set; } = 20;
public int TotalPages { get; set; }

[RelayCommand]
private async Task LoadPageAsync(int page)
{
    try
    {
        IsLoading = true;
        
        var result = await _sequenceService.GetPagedAsync(page, PageSize);
        
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

2. **Caching**: Cache frequently accessed data

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
    
    public async Task<IEnumerable<Sequence>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync("AllSequences", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            return await _innerService.GetAllAsync();
        });
    }
    
    public async Task<Sequence> GetByIdAsync(string id)
    {
        return await _cache.GetOrCreateAsync($"Sequence_{id}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            return await _innerService.GetByIdAsync(id);
        });
    }
    
    // Other methods with cache invalidation on writes
    public async Task<Sequence> CreateAsync(Sequence sequence)
    {
        var result = await _innerService.CreateAsync(sequence);
        _cache.Remove("AllSequences");
        return result;
    }
}
```

3. **Lazy Loading Properties**: Only load related entities when needed

```csharp
public class SequenceWithLazyProperties
{
    private readonly IParameterService _parameterService;
    private IEnumerable<Parameter> _parameters;
    
    public string Id { get; set; }
    public string Name { get; set; }
    
    public async Task<IEnumerable<Parameter>> GetParametersAsync()
    {
        if (_parameters == null)
        {
            _parameters = await _parameterService.GetBySequenceIdAsync(Id);
        }
        return _parameters;
    }
}
```

### Memory Management

To avoid memory leaks when integrating presentation and service layers:

1. **Dispose Service Resources**: Implement IDisposable for services with disposable resources

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

2. **Unsubscribe from Events**: Ensure ViewModels unsubscribe from service events

```csharp
public class MessageService : IMessageService
{
    public event EventHandler<string> MessageReceived;
    
    public void StartListening()
    {
        // Start background thread
    }
    
    public void StopListening()
    {
        // Stop background thread
    }
    
    protected virtual void OnMessageReceived(string message)
    {
        MessageReceived?.Invoke(this, message);
    }
}

public class ChatViewModel : ViewModelBase, IDisposable
{
    private readonly IMessageService _messageService;
    private bool _disposed;
    
    public ChatViewModel(IMessageService messageService)
    {
        _messageService = messageService;
        _messageService.MessageReceived += OnMessageReceived;
        _messageService.StartListening();
    }
    
    private void OnMessageReceived(object sender, string message)
    {
        // Update UI
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
                _messageService.MessageReceived -= OnMessageReceived;
                _messageService.StopListening();
            }
            
            _disposed = true;
        }
    }
}
```

## Integration Checklist and Best Practices

### Integration Checklist

When integrating the presentation and service layers, follow this checklist:

✅ **Dependency Injection Setup**
- [ ] Services registered with appropriate lifetimes
- [ ] ViewModels registered as transient
- [ ] Navigation and dialog services registered as singletons
- [ ] Configuration values injected properly

✅ **Service Layer Design**
- [ ] Services defined with clear interfaces
- [ ] Services handle validation and business logic
- [ ] Services throw domain-specific exceptions
- [ ] Services manage entity relationships appropriately

✅ **ViewModel Integration**
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

✅ **Testing**
- [ ] Services tested independently
- [ ] ViewModels tested with mocked services
- [ ] Integration tests verify layer communication
- [ ] Error paths tested

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

10. **Performance Monitoring**: Include performance monitoring in service implementations for critical operations

## Conclusion

Properly integrating the presentation layer with the service layer is critical for creating maintainable, testable, and robust WPF applications. By following the patterns and practices outlined in this guide, you can create a clean separation between UI concerns and business logic while ensuring efficient data flow and error handling.

The key principles to remember are:
- Maintain proper separation of concerns
- Use dependency injection for loose coupling
- Design clear service interfaces
- Implement consistent error handling
- Manage UI updates carefully with respect to threading
- Test integration points thoroughly

By adhering to these principles, your application will be more maintainable, easier to test, and better suited to accommodate changing requirements.
