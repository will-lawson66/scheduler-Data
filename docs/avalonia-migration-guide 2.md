# Migrating from WPF to Avalonia: Implementation Guide & Best Practices

## Table of Contents

1. [Introduction](#introduction)
2. [Architectural Overview](#architectural-overview)
3. [Project Structure](#project-structure)
4. [ReactiveUI Integration](#reactiveui-integration)
5. [MVVM Implementation](#mvvm-implementation)
6. [Navigation System](#navigation-system)
7. [Dialog System](#dialog-system)
8. [Styling and Theming](#styling-and-theming)
9. [Performance Considerations](#performance-considerations)
10. [Migration Strategy](#migration-strategy)
11. [Common Patterns and Best Practices](#common-patterns-and-best-practices)
12. [Troubleshooting](#troubleshooting)

## Introduction

This guide documents the process of migrating the Instrument.Data UI application from Windows Presentation Foundation (WPF) with Material Design to Avalonia UI. Avalonia is a cross-platform UI framework that enables running the same UI code on Windows, macOS, Linux, iOS, Android, and WebAssembly.

### Migration Objectives

- Create a cross-platform implementation of the UI to extend beyond Windows
- Maintain the existing architectural patterns (MVVM)
- Improve reactivity and user experience with modern reactive patterns
- Resolve styling and resource issues encountered in the WPF implementation
- Follow Avalonia best practices for maintainable, testable code

### Benefits of Avalonia

- **Cross-Platform:** Run on Windows, macOS, Linux, iOS, Android, and WebAssembly
- **Familiar Syntax:** XAML-based UI definition similar to WPF
- **Modern Architecture:** MVVM-friendly design with ReactiveUI support
- **Styling System:** CSS-like styling with powerful selectors
- **Active Community:** Growing ecosystem with regular updates

## Architectural Overview

The application follows a clean architecture approach with clear separation between layers:

```
┌───────────────────┐     ┌──────────────────┐     ┌───────────────────┐
│    Domain Layer   │     │    Service Layer  │     │  Presentation     │
│  (Instrument.Data)│     │                   │     │  Layer            │
│                   │     │                   │     │  (Avalonia UI)    │
│  - Entities       │◄───►│  - Services      │◄───►│  - ViewModels     │
│  - Repositories   │     │  - Use Cases     │     │  - Views          │
│  - Interfaces     │     │                   │     │  - UI Services    │
└───────────────────┘     └──────────────────┘     └───────────────────┘
```

- **Domain Layer:** Contains business entities and core interfaces
- **Service Layer:** Implements business logic and operations
- **Presentation Layer:** Handles UI rendering and user interaction

The Avalonia implementation focuses on the Presentation Layer while maintaining compatibility with the existing Domain and Service layers.

## Project Structure

The Avalonia project follows this structure to maintain clear separation of concerns:

```
Instrument.Data.Avalonia/
├── App.axaml                 # Application entry point
├── App.axaml.cs              # Application logic
├── Assets/                   # Static resources
│   └── Icons/                # Application icons
├── Controls/                 # Custom controls
├── Converters/               # Value converters
├── DependencyInjection/      # DI container setup
├── Helpers/                  # UI utility classes
├── MainWindow.axaml          # Main window
├── MainWindow.axaml.cs       # Main window code-behind
├── Models/                   # UI-specific models
├── Program.cs                # .NET Core entry point
├── Services/                 # UI-specific services
│   ├── Dialog/               # Dialog service
│   ├── Interfaces/           # Service interfaces
│   └── Navigation/           # Navigation service
├── Styles/                   # Resource dictionaries
│   ├── Colors.axaml          # Color definitions
│   └── Themes.axaml          # Control styles
├── ViewModels/               # ViewModels
│   └── Base/                 # Base ViewModel classes
├── Views/                    # UI Views
│   └── Base/                 # Base View classes
└── ViewLocator.cs            # ReactiveUI view locator
```

This structure separates UI components from business logic and services, making the code more maintainable and testable.

## ReactiveUI Integration

A key improvement in the migration is the integration with ReactiveUI, a functional reactive MVVM framework that enhances Avalonia's capabilities.

### Configuration in Program.cs

```csharp
public static AppBuilder BuildAvaloniaApp(IHost host)
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()
        .UseReactiveUI() // Enable ReactiveUI integration
        .With(new AvaloniaAppBuilderSettings { ServiceProvider = host.Services });
```

### Key ReactiveUI Features Implemented

1. **ViewModelActivator:** For proper view activation lifecycle
2. **WhenActivated:** For handling initialization and cleanup
3. **ReactiveCommand:** For enhanced command pattern with observables
4. **Interactions:** For dialog and service interactions
5. **DynamicData:** For advanced collection handling

### ViewLocator for ReactiveUI

The ViewLocator maps ViewModels to Views using a naming convention:

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
            // Try to resolve from DI or create instance
            var view = _serviceProvider.GetService(viewType) as Control 
                ?? (Control)Activator.CreateInstance(viewType);

            // Set up IViewFor or DataContext
            if (view is IViewFor viewFor)
                viewFor.ViewModel = data;
            else
                view.DataContext = data;

            return view;
        }

        return new TextBlock { Text = $"No view found for {viewModelType.Name}" };
    }

    public bool Match(object data) => data is ViewModelBase;
}
```

### Registration in App.xaml.cs

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Get the service provider
        var serviceProvider = (IServiceProvider)AvaloniaLocator.Current.GetService(typeof(IServiceProvider));
        
        // Register the ViewLocator
        AvaloniaLocator.CurrentMutable.Bind<IDataTemplate>().ToConstant(new ViewLocator(serviceProvider));
        
        // Register with Splat (ReactiveUI DI)
        Locator.CurrentMutable.RegisterConstant(serviceProvider, typeof(IServiceProvider));
        
        // Create main window
        desktop.MainWindow = new MainWindow
        {
            DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

## MVVM Implementation

### Enhanced ViewModelBase

The ViewModelBase class serves as the foundation for all ViewModels, providing common functionality:

```csharp
public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel, IDisposable
{
    private bool _isLoading;
    private string _title = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private readonly CompositeDisposable _disposables = new();
    
    // ReactiveUI Activator
    public ViewModelActivator Activator { get; } = new ViewModelActivator();
    
    // Common properties
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    
    // Observable for command canExecute
    public IObservable<bool> IsNotLoading => 
        this.WhenAnyValue(x => x.IsLoading).Select(x => !x);
    
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
    
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
    
    protected ViewModelBase()
    {
        // WhenActivated is called when view is attached to visual tree
        this.WhenActivated(disposables =>
        {
            HandleActivation();
            
            // Register cleanup action
            Disposable
                .Create(HandleDeactivation)
                .DisposeWith(disposables);
                
            // Register additional handlers
            SetupActivationHandlers(disposables);
        });
    }
    
    // Virtual methods for derived classes
    protected virtual void HandleActivation() { }
    protected virtual void HandleDeactivation() { }
    protected virtual void SetupActivationHandlers(CompositeDisposable disposables) { }
    
    // Helper for async operations
    protected async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        try
        {
            IsLoading = true;
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    // Error handling
    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
    
    // IDisposable implementation
    public virtual void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### Reactive View Base Classes

Reactive view base classes simplify the implementation of views:

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
    
    protected virtual void HandleActivation() { }
    protected virtual void HandleDeactivation() { }
    protected virtual void SetupViewActivationHandlers(CompositeDisposable disposables) { }
}
```

### ViewModel Implementation Pattern

ViewModels follow this pattern for consistency:

```csharp
public class SequencesViewModel : ViewModelBase, INavigationAware
{
    private readonly SequenceService _sequenceService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SequencesViewModel> _logger;
    
    // Using SourceList for better ReactiveUI integration
    private readonly SourceList<Sequence> _sequencesSource = new();
    private readonly ReadOnlyObservableCollection<Sequence> _sequences;
    private Sequence _selectedSequence;
    
    // Properties
    public ReadOnlyObservableCollection<Sequence> Sequences => _sequences;
    
    public Sequence SelectedSequence
    {
        get => _selectedSequence;
        set => this.RaiseAndSetIfChanged(ref _selectedSequence, value);
    }
    
    public bool HasNoSequences => !Sequences.Any();
    
    // Commands
    public ReactiveCommand<Unit, IEnumerable<Sequence>> LoadSequencesCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateSequenceCommand { get; }
    public ReactiveCommand<Sequence, Unit> ViewSequenceCommand { get; }
    public ReactiveCommand<Sequence, Unit> DeleteSequenceCommand { get; }
    
    // Constructor with dependency injection
    public SequencesViewModel(
        SequenceService sequenceService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger<SequencesViewModel> logger)
    {
        _sequenceService = sequenceService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _logger = logger;
        
        Title = "Sequences";
        
        // Connect the source list to the observable collection
        _sequencesSource.Connect()
            .Bind(out _sequences)
            .Subscribe();
        
        // Create commands with proper typing and error handling
        LoadSequencesCommand = ReactiveCommand.CreateFromTask(
            LoadSequencesAsync,
            this.IsNotLoading);
        
        CreateSequenceCommand = ReactiveCommand.Create(() => 
            _navigationService.NavigateTo<SequenceDetailViewModel>("new"));
        
        ViewSequenceCommand = ReactiveCommand.Create<Sequence, Unit>(
            sequence => 
            {
                _navigationService.NavigateTo<SequenceDetailViewModel>(sequence.Id);
                return Unit.Default;
            });
        
        DeleteSequenceCommand = ReactiveCommand.CreateFromTask<Sequence, Unit>(
            DeleteSequenceAsync);
    }
    
    // Setup activation handlers
    protected override void SetupActivationHandlers(CompositeDisposable disposables)
    {
        // Handle collection changes
        _sequencesSource.Connect()
            .WhenValueChanged(x => x.Count())
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasNoSequences)))
            .DisposeWith(disposables);
        
        // Handle command exceptions
        LoadSequencesCommand.ThrownExceptions
            .Subscribe(async ex => 
            {
                _logger.LogError(ex, "Failed to load sequences");
                await _dialogService.ShowErrorAsync("Error", 
                    $"Failed to load sequences: {ex.Message}");
            })
            .DisposeWith(disposables);
            
        DeleteSequenceCommand.ThrownExceptions
            .Subscribe(async ex => 
            {
                _logger.LogError(ex, "Failed to delete sequence");
                await _dialogService.ShowErrorAsync("Error", 
                    $"Failed to delete sequence: {ex.Message}");
            })
            .DisposeWith(disposables);
    }
    
    // Navigation handling
    public void OnNavigatedTo(object parameter)
    {
        LoadSequencesCommand.Execute().Subscribe(sequences => 
        {
            // If coming back from a detail view, try to reselect the previous sequence
            if (parameter is string sequenceId && !string.IsNullOrEmpty(sequenceId))
            {
                SelectedSequence = Sequences.FirstOrDefault(s => s.Id == sequenceId);
            }
        });
    }
    
    // Load data
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
    
    // Delete sequence
    private async Task<Unit> DeleteSequenceAsync(Sequence sequence)
    {
        if (sequence == null) return Unit.Default;
        
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Confirm Delete", 
            $"Are you sure you want to delete the sequence '{sequence.Name}'?");
            
        if (!confirmed) return Unit.Default;
        
        IsLoading = true;
        
        try
        {
            await _sequenceService.DeleteSequenceAsync(sequence.Id);
            
            // Remove from the source list
            _sequencesSource.Edit(list => list.Remove(sequence));
            
            // Update selection
            if (SelectedSequence == sequence)
            {
                SelectedSequence = null;
            }
            
            await _dialogService.ShowInformationAsync("Success", 
                "Sequence deleted successfully");
            return Unit.Default;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### View Implementation Pattern

Views follow this consistent pattern:

```csharp
public partial class SequencesView : ReactiveViewBase<SequencesViewModel>
{
    private ListBox _sequencesList;
    
    public SequencesView()
    {
        InitializeComponent();
        
        // Find controls for programmatic binding
        _sequencesList = this.FindControl<ListBox>("SequencesList");
    }
    
    protected override void HandleActivation()
    {
        // Called when view is attached to visual tree
        if (ViewModel != null)
        {
            // Load data on activation
            ViewModel.LoadSequencesCommand.Execute().Subscribe();
        }
    }
    
    protected override void SetupViewActivationHandlers(CompositeDisposable disposables)
    {
        // Setup additional bindings that should be cleaned up on deactivation
        this.WhenAnyValue(x => x.ViewModel.SelectedSequence)
            .Subscribe(sequence => 
            {
                // Handle selection changes
            })
            .DisposeWith(disposables);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
```

### View XAML Implementation

The XAML for views follows a consistent pattern:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="using:Instrument.Data.Avalonia.ViewModels"
             xmlns:material="using:Material.Styles"
             xmlns:rxui="using:Avalonia.ReactiveUI"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
             x:Class="Instrument.Data.Avalonia.Views.SequencesView"
             x:DataType="vm:SequencesViewModel">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <Grid Grid.Row="0" Margin="0,0,0,16">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <TextBlock Grid.Column="0" 
                      Text="{Binding Title}" 
                      FontSize="24"
                      FontWeight="Medium"
                      VerticalAlignment="Center"/>
            
            <Button Grid.Column="1"
                   Content="Add Sequence"
                   Command="{Binding CreateSequenceCommand}"
                   Classes="primary"/>
        </Grid>
        
        <!-- Content -->
        <material:Card Grid.Row="1">
            <Grid>
                <!-- Loading indicator -->
                <ProgressBar IsIndeterminate="True"
                           IsVisible="{Binding IsLoading}"
                           VerticalAlignment="Center"
                           HorizontalAlignment="Center"/>
                
                <!-- No data message -->
                <TextBlock Text="No sequences found. Click 'Add Sequence' to create one."
                         HorizontalAlignment="Center"
                         VerticalAlignment="Center"
                         IsVisible="{Binding HasNoSequences}"/>
                
                <!-- Data list with ReactiveUI binding -->
                <ListBox Name="SequencesList"
                       ItemsSource="{Binding Sequences}"
                       SelectedItem="{Binding SelectedSequence}"
                       IsVisible="{Binding !HasNoSequences}">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <!-- Item template content -->
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </Grid>
        </material:Card>
    </Grid>
</UserControl>
```

## Navigation System

A key component of the application is the navigation system, which has been enhanced to support both direct content setting and ReactiveUI routing.

### Navigation Service Interface

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

### Navigation Service Implementation

```csharp
public class NavigationService : ReactiveObject, INavigationService, IScreen
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private readonly Stack<(Type ViewModel, object Parameter)> _navigationStack = new();
    
    private Action<object> _contentSetter;
    
    // ReactiveUI routing state
    public RoutingState Router { get; }
    
    public NavigationService(
        IServiceProvider serviceProvider,
        ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Initialize ReactiveUI router
        Router = new RoutingState();
        
        // Register with Splat
        Locator.CurrentMutable.RegisterConstant(this, typeof(IScreen));
    }
    
    public void Initialize(object owner, Action<object> contentSetter)
    {
        _contentSetter = contentSetter;
    }
    
    // Standard navigation
    public void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
    {
        try
        {
            // Save current state
            var currentViewModel = _contentSetter?.Target as ViewModelBase;
            if (currentViewModel != null)
            {
                _navigationStack.Push((currentViewModel.GetType(), null));
            }
            
            // Resolve the ViewModel from DI
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            
            // Initialize if needed
            if (parameter != null && viewModel is IInitializable initializable)
            {
                initializable.Initialize(parameter);
            }
            
            if (viewModel is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(parameter);
            }
            
            // Update the content
            _contentSetter?.Invoke(viewModel);
            
            // Update ReactiveUI router if applicable
            if (viewModel is IRoutableViewModel routableViewModel)
            {
                Router.Navigate.Execute(routableViewModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation to {ViewModelType} failed", typeof(TViewModel).Name);
            throw;
        }
    }
    
    // ReactiveUI routing
    public void NavigateToRoute<TViewModel>(object parameter = null) 
        where TViewModel : class, IRoutableViewModel
    {
        try
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            
            if (parameter != null && viewModel is IInitializable initializable)
            {
                initializable.Initialize(parameter);
            }
            
            Router.Navigate.Execute(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation to route {ViewModelType} failed", typeof(TViewModel).Name);
            throw;
        }
    }
    
    // Combination of ReactiveUI and standard navigation
    public void GoBack()
    {
        // Try ReactiveUI first
        if (Router.NavigateBack.CanExecute.FirstAsync().Wait())
        {
            Router.NavigateBack.Execute().Subscribe();
            return;
        }
        
        // Fall back to custom navigation
        if (_navigationStack.Count > 0)
        {
            var (viewModelType, parameter) = _navigationStack.Pop();
            
            var navigateToMethod = typeof(NavigationService)
                .GetMethod(nameof(NavigateTo))
                .MakeGenericMethod(viewModelType);
            
            navigateToMethod.Invoke(this, new[] { parameter });
        }
    }
    
    // Reset navigation and navigate
    public void NavigateAndReset<TViewModel>(object parameter = null) 
        where TViewModel : ViewModelBase
    {
        _navigationStack.Clear();
        Router.NavigationStack.Clear();
        NavigateTo<TViewModel>(parameter);
    }
}
```

### View Navigation Configuration

In the MainWindow class, the navigation service is initialized with the content region:

```csharp
public partial class MainWindow : ReactiveWindowBase<MainWindowViewModel>
{
    private ContentControl _contentRegion;
    
    public MainWindow()
    {
        InitializeComponent();
        
        _contentRegion = this.FindControl<ContentControl>("ContentRegion");
    }
    
    protected override void HandleActivation()
    {
        if (ViewModel != null && _contentRegion != null)
        {
            var navigationService = ViewModel.NavigationService;
            
            navigationService.Initialize(ViewModel, content => _contentRegion.Content = content);
        }
    }
}
```

## Dialog System

The dialog system has been enhanced to support both Material.Avalonia dialogs and ReactiveUI interactions.

### Dialog Service Interface

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

### Dialog Service Implementation

```csharp
public class DialogService : ReactiveObject, IDialogService
{
    private readonly ILogger<DialogService> _logger;
    
    // ReactiveUI interactions
    public Interaction<MessageBoxParams, bool> ShowConfirmationInteraction { get; }
    public Interaction<MessageBoxParams, Unit> ShowInformationInteraction { get; }
    public Interaction<MessageBoxParams, Unit> ShowWarningInteraction { get; }
    public Interaction<MessageBoxParams, Unit> ShowErrorInteraction { get; }
    
    public DialogService(ILogger<DialogService> logger)
    {
        _logger = logger;
        
        // Initialize interactions
        ShowConfirmationInteraction = new Interaction<MessageBoxParams, bool>();
        ShowInformationInteraction = new Interaction<MessageBoxParams, Unit>();
        ShowWarningInteraction = new Interaction<MessageBoxParams, Unit>();
        ShowErrorInteraction = new Interaction<MessageBoxParams, Unit>();
    }
    
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return false;
            
            // Try ReactiveUI interaction first
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
    
    // Similar implementations for other dialog methods
    
    private Window GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is 
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        
        return null;
    }
}
```

## Styling and Theming

The styling system has been updated to follow Avalonia's CSS-like approach with proper pseudoclasses and selectors.

### Colors Definition

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Primary colors -->
    <Color x:Key="PrimaryColor">#1976D2</Color>
    <Color x:Key="PrimaryLightColor">#42A5F5</Color>
    <Color x:Key="PrimaryDarkColor">#0D47A1</Color>
    
    <!-- More color definitions... -->
    
    <!-- Dark theme colors -->
    <Color x:Key="DarkPrimaryColor">#1565C0</Color>
    <Color x:Key="DarkPrimaryLightColor">#1E88E5</Color>
    <Color x:Key="DarkPrimaryDarkColor">#0D47A1</Color>
    <Color x:Key="DarkBackgroundColor">#121212</Color>
    <Color x:Key="DarkSurfaceColor">#1E1E1E</Color>
    <Color x:Key="DarkTextPrimaryColor">#FFFFFF</Color>
    <Color x:Key="DarkTextSecondaryColor">#B3FFFFFF</Color>
    
    <!-- Brushes -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <!-- More brush definitions... -->
</ResourceDictionary>
```

### Styles Definition

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:material="using:Material.Styles">
    <!-- Button Styles with proper selectors -->
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
    
    <Style Selector="Button.primary:pressed">
        <Setter Property="Background" Value="{DynamicResource PrimaryDarkBrush}"/>
        <Setter Property="Opacity" Value="0.8"/>
    </Style>
    
    <!-- More style definitions... -->
</Styles>
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
        if (!_isDarkTheme)
        {
            var materialTheme = Application.Current.Styles.OfType<MaterialTheme>().FirstOrDefault();
            if (materialTheme != null)
            {
                materialTheme.BaseTheme = BaseThemeMode.Dark;
                _isDarkTheme = true;
            }
        }
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

### Converters

Value converters help bind data to the UI:

```csharp
public class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool shouldInvert = Invert;
        
        if (parameter is string paramString && 
            paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            shouldInvert = !shouldInvert;
        }
        
        bool boolValue = value is bool b ? b : false;
        
        if (shouldInvert)
        {
            boolValue = !boolValue;
        }
        
        return boolValue ? Avalonia.Controls.Avalonia.VisualTree.IsVisible : 
            Avalonia.Controls.Avalonia.VisualTree.IsCollapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Implementation details
    }
}
```

### Registering Converters

```xml
<Application.Resources>
    <!-- Global resources -->
    <converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
    <converters:InverseBooleanToVisibilityConverter x:Key="InverseBooleanToVisibilityConverter"/>
</Application.Resources>
```

## Performance Considerations

### Collection Handling with DynamicData

DynamicData is used for more efficient collection handling:

```csharp
// Initialize source list
private readonly SourceList<Sequence> _sequencesSource = new SourceList<Sequence>();
private readonly ReadOnlyObservableCollection<Sequence> _sequences;

// Connect the source list to the observable collection
_sequencesSource.Connect()
    .Bind(out _sequences)
    .Subscribe();

// Update collection
_sequencesSource.Edit(list => 
{
    list.Clear();
    list.AddRange(sequences);
});
```

### UI Virtualization

Avalonia provides UI virtualization for better performance with large collections:

```xml
<ListBox ItemsSource="{Binding Items}"
         VirtualizationMode="Simple">
    <!-- ListBox content -->
</ListBox>
```

### Command Implementation

ReactiveCommand provides better handling of command execution state:

```csharp
// Create command with canExecute
LoadSequencesCommand = ReactiveCommand.CreateFromTask(
    LoadSequencesAsync,
    this.IsNotLoading);

// Subscribe to exceptions
LoadSequencesCommand.ThrownExceptions
    .Subscribe(async ex => 
    {
        _logger.LogError(ex, "Failed to load sequences");
        await _dialogService.ShowErrorAsync("Error", 
            $"Failed to load sequences: {ex.Message}");
    })
    .DisposeWith(disposables);
```

## Migration Strategy

### Phased Migration Approach

1. **Infrastructure Setup**
   - Create Avalonia project structure
   - Set up dependency injection
   - Implement base classes and services

2. **Core Navigation**
   - Implement navigation service
   - Set up main window and content hosting

3. **UI Component Migration**
   - Migrate ViewModels with ReactiveUI patterns
   - Implement views with proper activation

4. **Styling and Theming**
   - Implement Material Design styling
   - Create proper Avalonia selectors

5. **Testing and Refinement**
   - Test navigation paths
   - Verify styling consistency
   - Optimize performance

### Priority List

1. Core application infrastructure
2. Navigation and service framework
3. Main views and common components
4. Detail views and specialized components
5. Styling and visual refinements

## Common Patterns and Best Practices

### ReactiveUI Activation Pattern

```csharp
// In ViewModel
this.WhenActivated(disposables =>
{
    // Setup happens here
    // Register cleanup
    Disposable.Create(() => { /* Cleanup */ })
        .DisposeWith(disposables);
});

// In View
this.WhenActivated(disposables =>
{
    // One-way binding
    this.OneWayBind(ViewModel, 
            vm => vm.Name, 
            v => v.NameTextBlock.Text)
        .DisposeWith(disposables);
        
    // Two-way binding
    this.Bind(ViewModel,
            vm => vm.Amount,
            v => v.AmountTextBox.Text)
        .DisposeWith(disposables);
        
    // Command binding
    this.BindCommand(ViewModel,
            vm => vm.SaveCommand,
            v => v.SaveButton)
        .DisposeWith(disposables);
});
```

### Binding to Commands with Parameters

```xml
<!-- Command binding with parameter -->
<Button Command="{Binding ViewSequenceCommand}"
       CommandParameter="{Binding}">
    View
</Button>
```

### Error Handling

```csharp
// Exception handling with ReactiveCommand
DeleteCommand.ThrownExceptions
    .Subscribe(async ex => 
    {
        _logger.LogError(ex, "Error: {Message}", ex.Message);
        await _dialogService.ShowErrorAsync("Error", ex.Message);
    })
    .DisposeWith(disposables);
```

### Resource Organization

```csharp
// Register themes in App.axaml
<Application.Styles>
    <!-- Material Theme -->
    <material:MaterialTheme BaseTheme="Light" PrimaryColor="Blue" SecondaryColor="Indigo"/>
    
    <!-- Application-specific styles -->
    <StyleInclude Source="/Styles/Colors.axaml"/>
    <StyleInclude Source="/Styles/Themes.axaml"/>
</Application.Styles>
```

## Troubleshooting

### Common Issues and Solutions

1. **Resource Not Found**
   - Ensure resources are registered in App.axaml
   - Check for proper namespace declarations

2. **Binding Errors**
   - Use x:DataType for compiled bindings
   - Enable diagnostics: `<Window x:DataType="vm:MainViewModel" x:CompileBindings="True">`

3. **Styling Issues**
   - Use DynamicResource for theme switching
   - Ensure proper selector syntax with pseudoclasses

4. **Navigation Failures**
   - Verify ViewModels are registered in the DI container
   - Ensure proper view activation is implemented

5. **ReactiveUI Integration**
   - Verify UseReactiveUI() is called in App builder
   - Implement IViewFor or use ReactiveUserControl/ReactiveWindow base classes

### Framework Differences

1. **Naming Differences**
   - XAML files have .axaml extension, not .xaml
   - Controls may have different names and properties

2. **Resource System**
   - Avalonia uses CSS-like selectors for styling
   - Resources are more scoped compared to WPF

3. **Dependency Properties**
   - Avalonia has different implementation of dependency properties
   - Use AvaloniaProperty instead of DependencyProperty

4. **Event Handling**
   - Use ReactiveUI's WhenActivated for view setup
   - Prefer reactive binding over event handlers

5. **Platform Specifics**
   - Consider platform differences for file operations
   - Use Avalonia's platform detection for platform-specific code

## Conclusion

The migration to Avalonia preserves the architectural patterns of the original WPF application while taking advantage of Avalonia's cross-platform capabilities and ReactiveUI's powerful reactive patterns. The enhanced implementation provides better performance, improved testability, and a more maintainable codebase.

By following the patterns and practices described in this guide, you can successfully migrate WPF applications to Avalonia and ensure they follow best practices for modern cross-platform application development.
