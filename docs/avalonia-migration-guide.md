# WPF to Avalonia Migration and Best Practices Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Architectural Overview](#architectural-overview)
3. [Core Project Structure](#core-project-structure)
4. [ReactiveUI Integration](#reactiveui-integration)
5. [MVVM Implementation](#mvvm-implementation)
6. [Navigation System](#navigation-system)
7. [Dialog and Services](#dialog-and-services)
8. [Styling and Theming](#styling-and-theming)
9. [Performance Considerations](#performance-considerations)
10. [Testing Recommendations](#testing-recommendations)
11. [Common Pitfalls](#common-pitfalls)
12. [Next Steps](#next-steps)

## Introduction

This guide documents the process of migrating the Instrument.Data application from Windows Presentation Foundation (WPF) with Material Design to Avalonia UI. The migration focuses on preserving the existing architecture while leveraging Avalonia's cross-platform capabilities and adopting best practices for both Avalonia and ReactiveUI.

### Key Benefits of Migration

1. **Cross-Platform Support**: The application now runs on Windows, macOS, and Linux
2. **Modern Reactive Architecture**: Enhanced implementation of the MVVM pattern with ReactiveUI
3. **Improved Performance**: Better handling of collections and UI virtualization
4. **Enhanced UI Responsiveness**: Reactive programming model for more fluid user interactions
5. **Future-Proof Design**: Architecture that supports modern development patterns

### Migration Strategy

We employed a systematic approach to migrate the code:

1. Create a new Avalonia project structure
2. Establish the core architectural patterns (ViewModelBase, Navigation, etc.)
3. Implement key services (Navigation, Dialog, etc.)
4. Migrate views and view models one by one
5. Enhance the implementation with ReactiveUI features

## Architectural Overview

The application follows a clean architecture with clear separation of concerns:

```
┌───────────────────┐      ┌────────────────────┐      ┌───────────────────┐
│    Domain Layer   │      │  Application Layer  │      │  Presentation     │
│                   │      │                     │      │  Layer            │
│  - Entities       │      │  - Services         │      │                   │
│  - Value Objects  │◄────►│  - Repositories     │◄────►│  - ViewModels     │
│  - Domain Logic   │      │  - Application Logic│      │  - Views          │
└───────────────────┘      └────────────────────┘      └───────────────────┘
```

Key architectural principles maintained:

- **Dependency Injection**: Used throughout for loose coupling
- **MVVM**: Clear separation between UI and logic
- **Reactive Programming**: Observable properties and commands
- **Single Responsibility**: Each class has a defined purpose
- **Interface-Based Design**: Services defined through interfaces

## Core Project Structure

The Avalonia project maintains a clear structure:

```
Instrument.Data.Avalonia/
├── App.axaml / App.axaml.cs           # Application entry point
├── Assets/                            # Static resources
│   └── Icons/                         # Application icons
├── Controls/                          # Custom controls
├── Converters/                        # Value converters
├── DependencyInjection/               # DI container setup
├── Helpers/                           # UI utility classes
├── MainWindow.axaml                   # Main application window
├── Program.cs                         # .NET Core entry point
├── Services/                          # UI-specific services
│   ├── Interfaces/                    # Service interfaces
│   ├── Dialog/                        # Dialog service
│   └── Navigation/                    # Navigation service
├── Styles/                            # Resource dictionaries
│   ├── Colors.axaml                   # Color definitions
│   └── Themes.axaml                   # UI element styles
├── ViewLocator.cs                     # ViewModel->View locator
├── ViewModels/                        # ViewModels
│   └── Base/                          # Base ViewModel classes
└── Views/                             # UI Views
    └── Base/                          # Base View classes
```

## ReactiveUI Integration

ReactiveUI provides a powerful reactive programming model for Avalonia. We've fully integrated ReactiveUI throughout the application.

### Key ReactiveUI Features Implemented

#### 1. ViewModelActivator

The ViewModelActivator manages ViewModel lifecycle:

```csharp
public class ViewModelBase : ReactiveObject, IActivatableViewModel, IDisposable
{
    // ViewModelActivator for ReactiveUI activation
    public ViewModelActivator Activator { get; } = new ViewModelActivator();
    
    protected ViewModelBase()
    {
        this.WhenActivated(disposables =>
        {
            // Set up activation handlers
            HandleActivation();
            
            // Register cleanup
            Disposable
                .Create(HandleDeactivation)
                .DisposeWith(disposables);
                
            // Additional setup
            SetupActivationHandlers(disposables);
        });
    }
}
```

#### 2. Reactive Commands

ReactiveCommands provide type-safe command handling with automatic enabling/disabling:

```csharp
// Command without parameters returning a collection of sequences
public ReactiveCommand<Unit, IEnumerable<Sequence>> LoadSequencesCommand { get; }

// Command taking a Sequence parameter
public ReactiveCommand<Sequence, Unit> ViewSequenceCommand { get; }

// Initializing commands
LoadSequencesCommand = ReactiveCommand.CreateFromTask(
    LoadSequencesAsync,    // The method to execute
    this.IsNotLoading);    // When the command can execute

// Command with parameter
ViewSequenceCommand = ReactiveCommand.Create<Sequence, Unit>(
    sequence => 
    {
        _navigationService.NavigateTo<SequenceDetailViewModel>(sequence.Id);
        return Unit.Default;
    });
```

#### 3. WhenActivated Pattern

WhenActivated handles setup and cleanup when views/viewmodels are attached/detached:

```csharp
this.WhenActivated(disposables =>
{
    // Trigger loading data when activated
    ViewModel.LoadSequencesCommand.Execute().Subscribe();
    
    // Register property bindings that need disposal
    this.WhenAnyValue(x => x.ViewModel.SelectedSequence)
        .Subscribe(sequence => { /* Handle selection changes */ })
        .DisposeWith(disposables);  // Will be disposed when view is detached
});
```

#### 4. DynamicData

DynamicData provides reactive collections:

```csharp
// Source list for better ReactiveUI integration
private readonly SourceList<Sequence> _sequencesSource = new SourceList<Sequence>();
private readonly ReadOnlyObservableCollection<Sequence> _sequences;

// Connect the source list to the observable collection
_sequencesSource.Connect()
    .Bind(out _sequences)  // Creates a read-only collection
    .Subscribe();

// Update the list by editing the source
_sequencesSource.Edit(list => 
{
    list.Clear();
    list.AddRange(sequences);
});
```

#### 5. Routing

Implemented ReactiveUI routing for navigation:

```csharp
// NavigationService implements IScreen
public class NavigationService : ReactiveObject, INavigationService, IScreen
{
    // ReactiveUI routing state
    public RoutingState Router { get; }
    
    public NavigationService(IServiceProvider serviceProvider)
    {
        // Initialize routing
        Router = new RoutingState();
        Locator.CurrentMutable.RegisterConstant(this, typeof(IScreen));
    }
    
    // Navigate using ReactiveUI router
    public void NavigateToRoute<TViewModel>(object parameter = null) 
        where TViewModel : class, IRoutableViewModel
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        Router.Navigate.Execute(viewModel);
    }
}
```

## MVVM Implementation

The MVVM pattern is implemented using ReactiveUI's enhanced capabilities:

### Base Classes

#### 1. ViewModelBase

```csharp
public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel, IDisposable
{
    private bool _isLoading;
    private string _title = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    
    public ViewModelActivator Activator { get; } = new ViewModelActivator();
    
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    
    // Observable indicating whether the ViewModel is busy
    public IObservable<bool> IsNotLoading => 
        this.WhenAnyValue(x => x.IsLoading).Select(x => !x);
    
    // Error handling properties
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _errorMessage, value);
            HasError = !string.IsNullOrEmpty(value);
        }
    }
    
    public bool HasError
    {
        get => _hasError;
        set => this.RaiseAndSetIfChanged(ref _hasError, value);
    }
    
    // Other properties and methods
}
```

#### 2. ReactiveViewBase

```csharp
public class ReactiveViewBase<TViewModel> : ReactiveUserControl<TViewModel> 
    where TViewModel : ViewModelBase
{
    protected ReactiveViewBase()
    {
        this.WhenActivated(disposables =>
        {
            HandleActivation();
            
            Disposable.Create(HandleDeactivation)
                .DisposeWith(disposables);
            
            SetupViewActivationHandlers(disposables);
        });
    }
    
    // Override methods for activation handling
    protected virtual void HandleActivation() { }
    protected virtual void HandleDeactivation() { }
    protected virtual void SetupViewActivationHandlers(CompositeDisposable disposables) { }
}
```

### ViewModel Implementation

```csharp
public class SequencesViewModel : ViewModelBase, INavigationAware
{
    private readonly SequenceService _sequenceService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    
    // Using SourceList for better ReactiveUI integration
    private readonly SourceList<Sequence> _sequencesSource = new SourceList<Sequence>();
    private readonly ReadOnlyObservableCollection<Sequence> _sequences;
    private Sequence _selectedSequence;
    
    // Observable collection for UI binding
    public ReadOnlyObservableCollection<Sequence> Sequences => _sequences;
    
    // Commands
    public ReactiveCommand<Unit, IEnumerable<Sequence>> LoadSequencesCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateSequenceCommand { get; }
    public ReactiveCommand<Sequence, Unit> ViewSequenceCommand { get; }
    public ReactiveCommand<Sequence, Unit> DeleteSequenceCommand { get; }
    
    // Constructor with dependency injection
    public SequencesViewModel(
        SequenceService sequenceService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _sequenceService = sequenceService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        
        // Connect reactive collections
        _sequencesSource.Connect()
            .Bind(out _sequences)
            .Subscribe();
        
        // Create commands
        LoadSequencesCommand = ReactiveCommand.CreateFromTask(
            LoadSequencesAsync,
            this.IsNotLoading);
            
        // Other commands
    }
    
    // Setup activation handlers
    protected override void SetupActivationHandlers(CompositeDisposable disposables)
    {
        // Update HasNoSequences when collection changes
        _sequencesSource.Connect()
            .WhenValueChanged(x => x.Count())
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasNoSequences)))
            .DisposeWith(disposables);
        
        // Handle command exceptions
        LoadSequencesCommand.ThrownExceptions
            .Subscribe(async ex => 
            {
                // Log and show error
            })
            .DisposeWith(disposables);
    }
    
    // Async command methods
    private async Task<IEnumerable<Sequence>> LoadSequencesAsync()
    {
        IsLoading = true;
        
        try
        {
            var sequences = await _sequenceService.GetAllSequencesAsync();
            
            // Update the source list
            _sequencesSource.Edit(list => 
            {
                list.Clear();
                list.AddRange(sequences);
            });
            
            return sequences;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### View Implementation

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Instrument.Data.Avalonia.ViewModels"
             xmlns:material="using:Material.Styles"
             xmlns:rxui="using:Avalonia.ReactiveUI"
             x:Class="Instrument.Data.Avalonia.Views.SequencesView"
             x:DataType="vm:SequencesViewModel">
    <Grid>
        <!-- UI content with data bindings -->
        <ListBox Name="SequencesList"
                ItemsSource="{Binding Sequences}"
                SelectedItem="{Binding SelectedSequence}">
            <!-- Item template -->
        </ListBox>
        
        <!-- Other UI elements -->
    </Grid>
</UserControl>
```

```csharp
public partial class SequencesView : ReactiveViewBase<SequencesViewModel>
{
    private ListBox _sequencesList;
    
    public SequencesView()
    {
        InitializeComponent();
        
        // Find controls by name
        _sequencesList = this.FindControl<ListBox>("SequencesList");
    }
    
    protected override void HandleActivation()
    {
        // Load data when activated
        if (ViewModel != null)
        {
            ViewModel.LoadSequencesCommand.Execute().Subscribe();
        }
    }
    
    protected override void SetupViewActivationHandlers(CompositeDisposable disposables)
    {
        // Setup additional bindings
        this.WhenAnyValue(x => x.ViewModel.SelectedSequence)
            .Subscribe(sequence => { /* Handle selection changes */ })
            .DisposeWith(disposables);
    }
}
```

## Navigation System

The navigation system combines direct content control with ReactiveUI routing.

### INavigationService Interface

```csharp
public interface INavigationService
{
    void Initialize(object owner, Action<object> contentSetter);
    void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase;
    void NavigateToRoute<TViewModel>(object parameter = null) where TViewModel : class, IRoutableViewModel;
    void GoBack();
    void NavigateAndReset<TViewModel>(object parameter = null) where TViewModel : ViewModelBase;
}
```

### NavigationService Implementation

```csharp
public class NavigationService : ReactiveObject, INavigationService, IScreen
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<(Type ViewModel, object Parameter)> _navigationStack = new();
    
    private object _navigationOwner;
    private Action<object> _contentSetter;
    
    // ReactiveUI routing state
    public RoutingState Router { get; }
    
    // Initialize with hosting environment
    public void Initialize(object owner, Action<object> contentSetter)
    {
        _navigationOwner = owner;
        _contentSetter = contentSetter;
    }
    
    // Navigate to a view model
    public void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
    {
        // Save current view to navigation stack
        
        // Resolve the ViewModel from DI
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        
        // Initialize ViewModel with parameter
        if (parameter != null && viewModel is IInitializable initializable)
        {
            initializable.Initialize(parameter);
        }
        
        // Notify ViewModel about navigation
        if (viewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(parameter);
        }
        
        // Update the content
        _contentSetter?.Invoke(viewModel);
        
        // If view model is routable, also update the routing state
        if (viewModel is IRoutableViewModel routableViewModel)
        {
            Router.Navigate.Execute(routableViewModel);
        }
    }
    
    // Other navigation methods...
}
```

### Integration in MainWindow

```csharp
public partial class MainWindow : ReactiveWindowBase<MainWindowViewModel>
{
    // UI Components that need programmatic binding
    private ContentControl _contentRegion;
    
    protected override void HandleActivation()
    {
        // Initialize navigation with this window as the content host
        if (ViewModel != null && _contentRegion != null)
        {
            // Get navigation service from ViewModel
            var navigationService = ViewModel.NavigationService;
            
            // Initialize the navigation service with this window
            navigationService.Initialize(ViewModel, content => _contentRegion.Content = content);
        }
    }
}
```

### ViewLocator for ReactiveUI

```csharp
public class ViewLocator : IDataTemplate
{
    private readonly IServiceProvider _serviceProvider;

    public ViewLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Control Build(object data)
    {
        if (data is null)
            return new TextBlock { Text = "No data" };

        var viewModelType = data.GetType();
        var viewTypeName = viewModelType.FullName.Replace("ViewModel", "View");
        var viewType = Type.GetType(viewTypeName);

        if (viewType != null)
        {
            // Try to resolve from DI or create new instance
            var view = _serviceProvider.GetService(viewType) as Control 
                ?? (Control)Activator.CreateInstance(viewType);
                
            // Set ViewModel or DataContext
            if (view is IViewFor viewFor)
            {
                viewFor.ViewModel = data;
            }
            else
            {
                view.DataContext = data;
            }

            return view;
        }

        return new TextBlock { Text = $"No view found for {viewModelType.Name}" };
    }

    public bool Match(object data)
    {
        return data is ViewModelBase;
    }
}
```

## Dialog and Services

### IDialogService Interface

```csharp
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowInformationAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<string> ShowOpenFileDialogAsync(string title, string filter);
    Task<string> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName);
}
```

### DialogService Implementation

The DialogService uses both ReactiveUI Interactions and Material.Avalonia dialogs:

```csharp
public class DialogService : ReactiveObject, IDialogService
{
    // ReactiveUI interactions for showing dialogs
    public Interaction<MessageBoxParams, bool> ShowConfirmationInteraction { get; }
    public Interaction<MessageBoxParams, Unit> ShowInformationInteraction { get; }
    // Other interactions...
    
    public DialogService()
    {
        // Initialize interactions
        ShowConfirmationInteraction = new Interaction<MessageBoxParams, bool>();
        ShowInformationInteraction = new Interaction<MessageBoxParams, Unit>();
        // Other initializations...
    }
    
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return false;
            
            // First try ReactiveUI interaction if registered handlers exist
            try
            {
                return await ShowConfirmationInteraction.Handle(new MessageBoxParams
                {
                    Title = title,
                    Message = message
                });
            }
            catch (UnhandledInteractionException)
            {
                // Fall back to Material.Avalonia dialog
                var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                {
                    ContentHeader = title,
                    SupportingText = message,
                    StartupLocation = WindowStartupLocation.CenterOwner,
                    Width = 400,
                    NegativeButton = "Cancel",
                    PositiveButton = "OK",
                    DialogIcon = Material.Icons.MaterialIconKind.QuestionMark
                });
                
                var result = await dialog.ShowDialog(mainWindow);
                return result == "OK";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing confirmation dialog");
            return false;
        }
    }
    
    // Other dialog methods...
}
```

### Theme Service

```csharp
public class ThemeService : IThemeService
{
    private bool _isDarkTheme;
    
    public bool IsDarkTheme => _isDarkTheme;
    
    public void SetLightTheme()
    {
        if (_isDarkTheme)
        {
            var materialTheme = Application.Current.Styles.OfType<MaterialTheme>().FirstOrDefault();
            if (materialTheme != null)
            {
                materialTheme.BaseTheme = BaseThemeMode.Light;
                _isDarkTheme = false;
            }
        }
    }
    
    public void SetDarkTheme()
    {
        // Implementation...
    }
    
    public void ToggleTheme()
    {
        if (_isDarkTheme)
            SetLightTheme();
        else
            SetDarkTheme();
    }
}
```

## Styling and Theming

### Colors.axaml

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Primary colors -->
    <Color x:Key="PrimaryColor">#1976D2</Color>
    <!-- Other colors... -->
    
    <!-- Dark theme colors -->
    <Color x:Key="DarkPrimaryColor">#1565C0</Color>
    <!-- Other dark theme colors... -->
    
    <!-- Brushes -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <!-- Other brushes... -->
</ResourceDictionary>
```

### Themes.axaml

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:material="using:Material.Styles">
    <!-- Button Styles with proper Avalonia selectors -->
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="{DynamicResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="CornerRadius" Value="4"/>
        <Setter Property="material:ElevationEffect.Elevation" Value="Dp2"/>
    </Style>
    
    <Style Selector="Button.primary:pointerover">
        <Setter Property="Background" Value="{DynamicResource PrimaryDarkBrush}"/>
    </Style>
    
    <!-- Other styles... -->
</Styles>
```

### Key Style Improvements

1. **DynamicResource instead of StaticResource**
   - Enables theme switching at runtime
   - Resources update when themes change

2. **Pseudoclass Selectors**
   - `Button.primary:pointerover` for hover states
   - `ListBoxItem:selected` for selected states
   - `TextBox.form:focus` for focus states

3. **Visual State Management**
   - Proper handling of visual states through styles
   - No need for triggers like in WPF

4. **Material Design Integration**
   - Using Material.Avalonia for consistent look and feel
   - Integration with Avalonia's built-in styling system

5. **Theme Switching Support**
   - Light/dark theme implementation
   - Runtime theme switching

## Performance Considerations

### Collection Handling

1. **SourceList and DynamicData**
   - Use `SourceList<T>` instead of `ObservableCollection<T>`
   - More efficient change notification
   - Supports complex transformations

```csharp
private readonly SourceList<Sequence> _sequencesSource = new();
private readonly ReadOnlyObservableCollection<Sequence> _sequences;

// Setting up the connection
_sequencesSource.Connect()
    .Bind(out _sequences)
    .Subscribe();

// Updating the collection
_sequencesSource.Edit(list => 
{
    list.Clear();
    list.AddRange(sequences);
});
```

2. **UI Virtualization**
   - Ensure listboxes use virtualization for large collections
   - Set appropriate properties:

```xml
<ListBox ItemsSource="{Binding Sequences}"
         VirtualizingStackPanel.IsVirtualizing="True"
         VirtualizingStackPanel.VirtualizationMode="Recycling"
         ScrollViewer.IsDeferredScrollingEnabled="True">
```

### Memory Management

1. **Proper Disposal**
   - Use CompositeDisposable to manage resources
   - Dispose subscriptions when no longer needed

```csharp
// Register disposables properly
this.WhenAnyValue(x => x.SelectedSequence)
    .Subscribe(HandleSelectedSequenceChanged)
    .DisposeWith(disposables);
```

2. **Weak Event Patterns**
   - Use ReactiveUI's WhenActivated pattern to avoid memory leaks
   - Subscribe in WhenActivated blocks for automatic cleanup

### Async/Await

1. **ConfigureAwait**
   - Use ConfigureAwait(false) for non-UI operations
   - Keep ConfigureAwait(true) or omit for UI updates

2. **Command Execution**
   - Use ReactiveCommand.CreateFromTask for async commands
   - Handle exceptions with ThrownExceptions observable

```csharp
LoadSequencesCommand.ThrownExceptions
    .Subscribe(ex => HandleError(ex))
    .DisposeWith(disposables);
```

## Testing Recommendations

### ViewModel Testing

1. **ReactiveUI.Testing**
   - Use TestScheduler to control time in reactive sequences
   - Test command execution and property changes

```csharp
[Fact]
public void LoadSequencesCommand_ShouldPopulateSequences()
{
    // Arrange
    var scheduler = new TestScheduler();
    var mockSequenceService = new Mock<ISequenceService>();
    mockSequenceService.Setup(s => s.GetAllSequencesAsync())
        .ReturnsAsync(new List<Sequence>
        {
            new() { Id = "1", Name = "Test Sequence" }
        });
    
    // Create VM with mocked services
    var vm = new SequencesViewModel(
        mockSequenceService.Object,
        Mock.Of<INavigationService>(),
        Mock.Of<IDialogService>());
    
    // Act
    vm.LoadSequencesCommand.Execute().Subscribe();
    
    // Advance the scheduler to complete async operations
    scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
    
    // Assert
    Assert.Single(vm.Sequences);
    Assert.Equal("Test Sequence", vm.Sequences[0].Name);
}
```

2. **Mocking Services**
   - Mock navigation and dialog services
   - Test interaction with services

### View Testing

1. **Avalonia.Headless**
   - Use headless testing for UI testing without UI
   - Test UI interactions

```csharp
[Fact]
public async Task SequencesView_ClickingAddButton_ShouldInvokeCreateCommand()
{
    // Arrange
    var vm = new SequencesViewModel();
    var commandExecuted = false;
    vm.CreateSequenceCommand = ReactiveCommand.Create(() => commandExecuted = true);
    
    var view = new SequencesView { DataContext = vm };
    
    // Render the view
    var host = new TestHost(view);
    
    // Act - find the button and click it
    var addButton = host.FindControl<Button>("AddButton");
    addButton.Command.Execute(null);
    
    // Assert
    Assert.True(commandExecuted);
}
```

## Common Pitfalls

### 1. Resource Dictionary Issues

**Problem**: Resource not found exceptions

**Solution**: Ensure proper resource dictionary loading order:
1. First load MaterialDesignTheme.Light.xaml
2. Then load primary and accent color resources
3. Then load MaterialDesignTheme.Defaults.xaml
4. Finally load application-specific resources

### 2. Style Inheritance Issues

**Problem**: Custom styles not inheriting Material Design properties

**Solution**: Base custom styles on Material Design styles:

```xml
<!-- Incorrect -->
<Style x:Key="CustomButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
</Style>

<!-- Correct -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{DynamicResource PrimaryBrush}"/>
</Style>
```

### 3. Navigation Issues

**Problem**: Views not displaying or incorrect view displayed

**Solution**: Ensure proper registration of views and view models in DI container

```csharp
// Register views for dependency injection
services.AddTransient<Views.SequencesView>();
// Other views
```

### 4. ReactiveCommand Issues

**Problem**: Commands not executing or not enabling/disabling properly

**Solution**: Check CanExecute observables and exception handling:

```csharp
// Properly create command with CanExecute observable
MyCommand = ReactiveCommand.Create(
    ExecuteMethod,
    this.WhenAnyValue(x => x.CanExecute));
    
// Handle exceptions
MyCommand.ThrownExceptions
    .Subscribe(ex => HandleError(ex))
    .DisposeWith(disposables);
```

## Next Steps

### 1. Complete View Migration

Migrate all remaining views following the same pattern:
- Convert to ReactiveViewBase
- Implement WhenActivated
- Set up proper bindings

### 2. Enhanced UI Controls

Develop custom controls for common UI patterns:
- Form fields with validation
- Search boxes
- Data entry grids

### 3. Add Automated Testing

Implement extensive test coverage:
- Unit tests for view models
- UI tests for critical workflows
- Integration tests for service interactions

### 4. Performance Optimization

Optimize performance for large datasets:
- Implement pagination for large collections
- Use virtualization for all list controls
- Add caching where appropriate

### 5. Cross-Platform Enhancements

Add platform-specific enhancements:
- Native dialogs on each platform
- Platform-specific file system access
- High-DPI support for all platforms

### 6. Advanced ReactiveUI Features

Implement advanced ReactiveUI capabilities:
- Message Bus for cross-view communication
- Suspension for app state persistence
- More complex transformations with DynamicData
