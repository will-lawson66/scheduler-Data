# Instrument.Data.UI Presentation Layer Structure

## Table of Contents

1. [Introduction](#introduction)
2. [Core Architectural Pattern: MVVM](#core-architectural-pattern-mvvm)
3. [Directory Structure](#directory-structure)
4. [Component Details](#component-details)
5. [Navigation Strategies](#navigation-strategies)
6. [State Management](#state-management)
7. [Testing Strategies](#testing-strategies)
8. [Best Practices](#best-practices)
9. [Implementation Steps](#implementation-steps)
10. [Common Patterns and Examples](#common-patterns-and-examples)

## Introduction

The presentation layer is a crucial component of the Instrument.Data application, serving as the bridge between users and the underlying business logic. This document outlines a robust, maintainable structure for the C# presentation layer, focusing specifically on the WPF implementation in the Instrument.Data.UI project, but with principles that can be applied to other UI frameworks.

## Core Architectural Pattern: MVVM

The Model-View-ViewModel (MVVM) pattern is the foundation of our presentation layer design. MVVM enables a clear separation of concerns and facilitates unit testing of UI logic.

### Key Components

- **Model**: Represents the data and business logic from the domain layer (Instrument.Data)
- **View**: The UI elements users interact with (XAML files)
- **ViewModel**: Mediates between View and Model, exposing data and commands to the View

## Directory Structure

```
Instrument.Data.UI/
├── App.xaml / App.xaml.cs           # Application entry point
├── Controls/                        # Custom controls
│   └── FormFieldControl.xaml        # Example control
├── Converters/                      # Value converters
│   ├── BooleanToVisibilityConverter.cs
│   └── NullValueToBooleanConverter.cs
├── DependencyInjection/             # DI container setup
│   └── ServiceCollectionExtensions.cs
├── Helpers/                         # UI utility classes
│   ├── Converters/                  # Additional converters
│   └── StyleHelper.cs               # Style utilities
├── MainWindow.xaml                  # Main application window
├── Program.cs                       # .NET Core entry point
├── Resources/                       # Resource dictionaries
│   ├── Colors.xaml                  # Color definitions
│   └── Styles.xaml                  # UI element styles
├── Services/                        # UI-specific services
│   ├── Interfaces/                  # Service interfaces
│   │   ├── IDialogService.cs
│   │   └── INavigationService.cs
│   ├── Dialog/                      # Dialog service
│   │   └── DialogService.cs
│   └── Navigation/                  # Navigation service
│       └── NavigationService.cs
├── ViewModels/                      # ViewModels
│   ├── Base/                        # Base ViewModel classes
│   │   └── ViewModelBase.cs
│   ├── MainViewModel.cs
│   ├── SequencesViewModel.cs
│   └── SequenceDetailViewModel.cs
└── Views/                           # UI Views
    ├── SequencesView.xaml
    └── SequenceDetailView.xaml
```

## Component Details

### 1. Controls

**Purpose**: House custom UI controls that are reusable across multiple views.

```
Controls/
├── FormFieldControl.xaml            # Reusable form field
└── DataGridExtensions.xaml          # Enhanced DataGrid
```

**Example**:

```xml
<!-- FormFieldControl.xaml -->
<UserControl x:Class="Instrument.Data.UI.Controls.FormFieldControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <TextBlock Grid.Row="0" 
                   Text="{Binding Label, RelativeSource={RelativeSource AncestorType=UserControl}}"
                   Margin="0,0,0,4"/>
        
        <ContentPresenter Grid.Row="1" 
                          Content="{Binding Content, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
    </Grid>
</UserControl>
```

### 2. Converters

**Purpose**: Transform data between the format used in the ViewModel and what's needed for display.

```csharp
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? Visibility.Visible : Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (Visibility)value == Visibility.Visible;
    }
}
```

### 3. Helpers

**Purpose**: Provide utility functions specific to UI operations.

```csharp
public static class StyleHelper
{
    public static Style GetNamedStyle(string styleName)
    {
        if (Application.Current.Resources.Contains(styleName))
        {
            return (Style)Application.Current.Resources[styleName];
        }
        
        return null;
    }
    
    public static bool ApplyNamedStyle(FrameworkElement element, string styleName)
    {
        var style = GetNamedStyle(styleName);
        if (style != null)
        {
            element.Style = style;
            return true;
        }
        
        return false;
    }
}
```

### 4. Resources

**Purpose**: Centralize reusable resource dictionaries.

```xml
<!-- Colors.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Color x:Key="PrimaryColor">#1976D2</Color>
    <Color x:Key="AccentColor">#FF4081</Color>
    <Color x:Key="BackgroundColor">#FAFAFA</Color>
    <Color x:Key="TextPrimaryColor">#212121</Color>
    <Color x:Key="TextSecondaryColor">#757575</Color>
    
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}" />
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}" />
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}" />
</ResourceDictionary>
```

### 5. Services

**Purpose**: Provide UI-specific functionality isolated from business logic.

```csharp
public interface IDialogService
{
    bool ShowConfirmation(string title, string message);
    void ShowInformation(string title, string message);
    void ShowWarning(string title, string message);
    void ShowError(string title, string message);
    string ShowOpenFileDialog(string title, string filter);
    string ShowSaveFileDialog(string title, string filter, string defaultFileName);
}

public class DialogService : IDialogService
{
    public bool ShowConfirmation(string title, string message)
    {
        return MessageBox.Show(
            message, 
            title, 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
    
    // Other implementations
}
```

### 6. ViewModels

**Purpose**: Contain the presentation logic and state for views.

```csharp
public abstract class ViewModelBase : ObservableObject
{
    private bool _isLoading;
    private string _title = string.Empty;
    
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
    
    // Common ViewModel functionality
    protected async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        try
        {
            IsLoading = true;
            await action();
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class SequencesViewModel : ViewModelBase
{
    private readonly ISequenceService _sequenceService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SequencesViewModel> _logger;
    
    private ObservableCollection<Sequence> _sequences = new();
    private Sequence? _selectedSequence;
    
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
    
    public bool HasNoSequences => !Sequences.Any();
    
    public SequencesViewModel(
        ISequenceService sequenceService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger<SequencesViewModel> logger)
    {
        _sequenceService = sequenceService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _logger = logger;
        
        Title = "Sequences";
    }
    
    [RelayCommand]
    private async Task LoadSequencesAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            try
            {
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
                _logger.LogError(ex, "Failed to load sequences");
                _dialogService.ShowError("Error", $"Failed to load sequences: {ex.Message}");
            }
        });
    }
    
    // Other commands and methods
}
```

### 7. Views

**Purpose**: Define the visual elements and user interface.

```xml
<!-- SequencesView.xaml -->
<UserControl x:Class="Instrument.Data.UI.Views.SequencesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
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
                       Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                       VerticalAlignment="Center"/>
            
            <Button Grid.Column="1"
                    Content="Add Sequence"
                    Command="{Binding CreateSequenceCommand}"
                    Style="{StaticResource PrimaryButton}"/>
        </Grid>
        
        <!-- Content -->
        <materialDesign:Card Grid.Row="1" Margin="0">
            <Grid>
                <!-- Loading indicator -->
                <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}"
                             IsIndeterminate="True"
                             Value="0"
                             Visibility="{Binding IsLoading, 
                                Converter={StaticResource BooleanToVisibilityConverter}}"/>
                
                <!-- No data message -->
                <TextBlock Text="No sequences found. Click 'Add Sequence' to create one."
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center"
                           Visibility="{Binding HasNoSequences, 
                                Converter={StaticResource BooleanToVisibilityConverter}}"/>
                
                <!-- Data list -->
                <ListView ItemsSource="{Binding Sequences}"
                          SelectedItem="{Binding SelectedSequence}"
                          Visibility="{Binding HasNoSequences, 
                                Converter={StaticResource InverseBooleanToVisibilityConverter}}">
                    <!-- ListView content -->
                </ListView>
            </Grid>
        </materialDesign:Card>
    </Grid>
</UserControl>
```

## Navigation Strategies

### Centralized Navigation Service

The application uses a centralized navigation service to manage navigation between views:

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

### View Initialization

ViewModels that need initialization implement the `IInitializable` interface:

```csharp
public interface IInitializable
{
    void Initialize(object parameter);
}

public class SequenceDetailViewModel : ViewModelBase, IInitializable
{
    // Properties and services
    
    public void Initialize(object parameter)
    {
        if (parameter is string id)
        {
            LoadSequenceAsync(id).ConfigureAwait(false);
        }
        else
        {
            // Create new sequence mode
            IsNewSequence = true;
            CurrentSequence = new Sequence
            {
                Id = Guid.NewGuid().ToString(),
                Name = "New Sequence",
                WorstCaseTime = TimeSpan.FromMinutes(1)
            };
        }
    }
    
    private async Task LoadSequenceAsync(string id)
    {
        // Load sequence by ID
    }
}
```

## State Management

### ViewModel State

ViewModels manage UI state using several techniques:

1. **Observable Properties**: Properties that notify the UI when they change

```csharp
private Sequence? _currentSequence;

public Sequence? CurrentSequence
{
    get => _currentSequence;
    set => SetProperty(ref _currentSequence, value);
}
```

2. **Computed Properties**: Properties derived from other properties

```csharp
public bool CanSave => !string.IsNullOrEmpty(CurrentSequence?.Name);
```

3. **Commands**: Encapsulate user actions

```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task SaveAsync()
{
    // Save implementation
}
```

4. **State Indicators**: Properties that indicate UI state

```csharp
private bool _isLoading;
private bool _hasErrors;
private string _errorMessage = string.Empty;

public bool IsLoading
{
    get => _isLoading;
    set => SetProperty(ref _isLoading, value);
}

public bool HasErrors
{
    get => _hasErrors;
    set => SetProperty(ref _hasErrors, value);
}

public string ErrorMessage
{
    get => _errorMessage;
    set
    {
        SetProperty(ref _errorMessage, value);
        HasErrors = !string.IsNullOrEmpty(value);
    }
}
```

### Application State

For broader application state, consider:

1. **Singleton Services**: Application-wide state

```csharp
public class ApplicationStateService
{
    public event EventHandler<string> ThemeChanged;
    
    private string _currentTheme = "Light";
    
    public string CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ThemeChanged?.Invoke(this, value);
            }
        }
    }
}
```

2. **Messenger Patterns**: Communication between ViewModels

```csharp
public interface IMessenger
{
    void Subscribe<TMessage>(object subscriber, Action<TMessage> action);
    void Unsubscribe<TMessage>(object subscriber);
    void Send<TMessage>(TMessage message);
}

// Usage in ViewModels
public class SequenceDetailViewModel : ViewModelBase
{
    private readonly IMessenger _messenger;
    
    public SequenceDetailViewModel(IMessenger messenger)
    {
        _messenger = messenger;
        _messenger.Subscribe<SequenceUpdatedMessage>(this, OnSequenceUpdated);
    }
    
    private void OnSequenceUpdated(SequenceUpdatedMessage message)
    {
        // Handle the message
    }
    
    public override void Dispose()
    {
        _messenger.Unsubscribe<SequenceUpdatedMessage>(this);
        base.Dispose();
    }
}
```

## Testing Strategies

### Unit Testing ViewModels

```csharp
[Fact]
public async Task LoadSequencesCommand_ShouldPopulateSequencesCollection()
{
    // Arrange
    var mockSequenceService = new Mock<ISequenceService>();
    mockSequenceService.Setup(s => s.GetAllSequencesAsync())
        .ReturnsAsync(new List<Sequence>
        {
            new Sequence { Id = "1", Name = "Sequence 1" },
            new Sequence { Id = "2", Name = "Sequence 2" }
        });
    
    var mockNavigationService = new Mock<INavigationService>();
    var mockDialogService = new Mock<IDialogService>();
    var mockLogger = new Mock<ILogger<SequencesViewModel>>();
    
    var viewModel = new SequencesViewModel(
        mockSequenceService.Object,
        mockNavigationService.Object,
        mockDialogService.Object,
        mockLogger.Object);
    
    // Act
    await viewModel.LoadSequencesCommand.ExecuteAsync(null);
    
    // Assert
    Assert.Equal(2, viewModel.Sequences.Count);
    Assert.Equal("Sequence 1", viewModel.Sequences[0].Name);
    Assert.Equal("Sequence 2", viewModel.Sequences[1].Name);
    Assert.False(viewModel.IsLoading);
}

[Fact]
public async Task SaveCommand_WhenValidationFails_ShouldShowError()
{
    // Arrange
    var mockSequenceService = new Mock<ISequenceService>();
    mockSequenceService.Setup(s => s.UpdateSequenceAsync(It.IsAny<Sequence>()))
        .ThrowsAsync(new ValidationException("Name is required"));
    
    var mockNavigationService = new Mock<INavigationService>();
    var mockDialogService = new Mock<IDialogService>();
    var mockLogger = new Mock<ILogger<SequenceDetailViewModel>>();
    
    var viewModel = new SequenceDetailViewModel(
        mockSequenceService.Object,
        mockNavigationService.Object,
        mockDialogService.Object,
        mockLogger.Object);
    
    viewModel.CurrentSequence = new Sequence { Id = "1" };
    viewModel.IsNewSequence = false;
    
    // Act
    await viewModel.SaveCommand.ExecuteAsync(null);
    
    // Assert
    mockDialogService.Verify(d => d.ShowWarning(
        It.Is<string>(s => s.Contains("Validation Error")),
        It.Is<string>(s => s.Contains("Name is required"))),
        Times.Once);
    
    mockNavigationService.Verify(n => n.GoBack(),
        Times.Never);
}
```

### UI Automation Testing

1. **UI Automation Hooks**: Add automation IDs to elements

```xml
<Button Content="Save"
        Command="{Binding SaveCommand}"
        AutomationProperties.AutomationId="SaveButton"/>
```

2. **Test Helpers**: Create helpers for UI testing

```csharp
public static class UITestHelper
{
    public static async Task<TElement> WaitForElementAsync<TElement>(
        AutomationElement root, 
        string automationId, 
        TimeSpan timeout) where TElement : AutomationElement
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < timeout)
        {
            var element = root.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.AutomationIdProperty, 
                    automationId));
            
            if (element != null)
            {
                return (TElement)element;
            }
            
            await Task.Delay(100);
        }
        
        throw new TimeoutException($"Element with automation ID '{automationId}' not found within timeout");
    }
}
```

## Best Practices

1. **Keep Views Thin**: Minimal code-behind, with logic in ViewModels
2. **Use Data Binding**: Prefer data binding over direct UI manipulation
3. **Interface-Based Design**: Define interfaces for services to enable testing
4. **Dependency Injection**: Avoid static service locators, use DI
5. **Resource Organization**: Use merged dictionaries for scalable resources
6. **Consistent Naming**: Follow clear naming conventions for all components
7. **Modular Design**: Design components to be independently replaceable
8. **Progressive Disclosure**: Hide complexity until needed in the UI
9. **Platform Abstraction**: Abstract platform-specific code behind interfaces
10. **UI Thread Management**: Handle threading concerns consistently

## Implementation Steps

1. Set up the basic folder structure
2. Implement core infrastructure (ViewModelBase, Navigation)
3. Define application-wide resources
4. Create shared controls and behaviors
5. Implement view-specific ViewModels
6. Create views with minimal code-behind
7. Develop UI-specific services
8. Implement validation and error handling
9. Connect to application/domain layer via services

## Common Patterns and Examples

### Command Pattern Implementation

```csharp
// Using CommunityToolkit.Mvvm
[RelayCommand]
private void Execute()
{
    // Command implementation
}

[RelayCommand(CanExecute = nameof(CanExecute))]
private void ExecuteWithCanExecute()
{
    // Command implementation
}

private bool CanExecute() => SomeCondition;
```

### Event to Command Binding

```xml
<ListView ItemsSource="{Binding Items}">
    <ListView.ItemContainerStyle>
        <Style TargetType="ListViewItem" BasedOn="{StaticResource {x:Type ListViewItem}}">
            <Setter Property="HorizontalContentAlignment" Value="Stretch" />
        </Style>
    </ListView.ItemContainerStyle>
    <ListView.ItemTemplate>
        <DataTemplate>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                
                <TextBlock Grid.Column="0" 
                           Text="{Binding Name}" 
                           VerticalAlignment="Center" />
                
                <Button Grid.Column="1"
                        Content="View"
                        Command="{Binding DataContext.ViewItemCommand, 
                                  RelativeSource={RelativeSource AncestorType=ListView}}"
                        CommandParameter="{Binding}" />
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### Property Change Notification

```csharp
// Using CommunityToolkit.Mvvm
public partial class MyViewModel : ObservableObject
{
    // Auto-generated property with notification
    [ObservableProperty]
    private string _name = string.Empty;
    
    // Manual property with notification
    private bool _isValid;
    
    public bool IsValid
    {
        get => _isValid;
        set 
        {
            if (SetProperty(ref _isValid, value))
            {
                // Property changed additional logic
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }
    
    // Calculated property
    public bool CanSave => !string.IsNullOrEmpty(Name) && IsValid;
}
```

## See Also

- [Core Data Layer](./core-data-layer.md) - Details of the data layer architecture
- [Integration Guide](./integration-guide.md) - How to integrate the presentation layer with services
- [WPF Material Design Guide](./wpf-material-design-guide.md) - Material Design implementation details
