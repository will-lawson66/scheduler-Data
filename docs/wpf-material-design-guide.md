# WPF Material Design Application Architecture Guide

## Introduction

This document provides a comprehensive overview of the Instrument.Data.UI project, exploring its architecture, design decisions, and implementation patterns. It's designed as an educational resource for developers with limited WPF and Material Design experience, providing insight into how modern desktop applications are structured using the MVVM pattern and Material Design principles.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [MVVM Implementation](#mvvm-implementation)
4. [UI Design and Material Design Integration](#ui-design-and-material-design-integration)
5. [Data Management and Entity Framework](#data-management-and-entity-framework)
6. [Navigation System](#navigation-system)
7. [Common Controls and Patterns](#common-controls-and-patterns)
8. [Resource Management](#resource-management)
9. [Dependency Injection](#dependency-injection)
10. [Common Pitfalls and Troubleshooting](#common-pitfalls-and-troubleshooting)
11. [Testing Strategies](#testing-strategies)

## Architecture Overview

The Instrument.Data.UI project implements a Model-View-ViewModel (MVVM) architecture, which is the standard pattern for WPF applications. This pattern separates concerns into three distinct layers:

1. **Model**: Represents the business data and logic (defined primarily in the Instrument.Data project)
2. **View**: The user interface components (.xaml files and their code-behind)
3. **ViewModel**: The bridge between Model and View, handling UI logic and state

The application follows a modular approach, with distinct components for data access, business logic, and presentation. This separation enables better maintainability, testability, and flexibility.

The key architectural components include:

- **Views**: XAML-based UI components
- **ViewModels**: Classes that manage the state and behavior of Views
- **Services**: Helper classes that provide functionality like dialog management and navigation
- **Models**: Entity classes representing business data
- **Resources**: Reusable styles, templates, and converters

## Project Structure

The project is organized into logical folders that reflect the architectural components:

```
Instrument.Data.UI/
├── App.xaml                 # Application entry point and global resources
├── MainWindow.xaml          # Main application window
├── Program.cs               # .NET Core entry point with DI configuration
├── Controls/                # Reusable UI controls
├── Helpers/                 # Utility classes and converters
│   └── Converters/          # Value converters for data binding
├── Resources/               # Global style resources
│   ├── Colors.xaml          # Color definitions
│   └── Styles.xaml          # UI element styles
├── Services/                # Application services
│   ├── DialogService.cs     # Service for displaying dialogs
│   └── NavigationService.cs # Service for navigating between views
├── ViewModels/              # ViewModels that power the UI
│   ├── ViewModelBase.cs     # Base class for all ViewModels
│   └── [Feature]ViewModels  # Feature-specific ViewModels
└── Views/                   # UI components
    └── [Feature]Views       # Feature-specific Views
```

This structure follows the principle of separation of concerns, making it easier to locate and modify specific components without affecting others. Each folder serves a specific purpose in the application architecture.

## MVVM Implementation

### ViewModelBase

All ViewModels inherit from `ViewModelBase`, which implements the `INotifyPropertyChanged` interface required for WPF data binding. This class uses the CommunityToolkit.Mvvm library to simplify property change notifications:

```csharp
public abstract class ViewModelBase : ObservableObject
{
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
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
```

This base class provides:
- Property change notifications through `ObservableObject`
- Loading state management
- Error handling patterns
- Common functionality used across ViewModels

### Command Implementation

Commands use the `RelayCommand` and `AsyncRelayCommand` classes from CommunityToolkit.Mvvm:

```csharp
// Synchronous command
[RelayCommand]
private void Refresh()
{
    // Command implementation
}

// Asynchronous command
[RelayCommand]
private async Task LoadDataAsync()
{
    await ExecuteWithLoadingAsync(async () =>
    {
        // Load data from service
    });
}
```

This approach eliminates boilerplate code for command implementations and provides features like automatic enabling/disabling based on can-execute conditions.

### Example ViewModel

The `SequencesViewModel` class demonstrates a typical ViewModel implementation:

```csharp
public class SequencesViewModel : ViewModelBase
{
    private readonly ISequenceService _sequenceService;
    private readonly INavigationService _navigationService;
    
    private ObservableCollection<Sequence> _sequences;
    private Sequence _selectedSequence;
    
    public string Title => "Sequences";
    
    public ObservableCollection<Sequence> Sequences
    {
        get => _sequences;
        set => SetProperty(ref _sequences, value);
    }
    
    public Sequence SelectedSequence
    {
        get => _selectedSequence;
        set => SetProperty(ref _selectedSequence, value);
    }
    
    public bool HasNoSequences => Sequences?.Count == 0;
    
    public SequencesViewModel(
        ISequenceService sequenceService,
        INavigationService navigationService)
    {
        _sequenceService = sequenceService;
        _navigationService = navigationService;
        
        Sequences = new ObservableCollection<Sequence>();
    }
    
    [RelayCommand]
    private async Task LoadSequencesAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var sequences = await _sequenceService.GetAllAsync();
            Sequences.Clear();
            foreach (var sequence in sequences)
            {
                Sequences.Add(sequence);
            }
            OnPropertyChanged(nameof(HasNoSequences));
        });
    }
    
    [RelayCommand]
    private void ViewSequence()
    {
        if (SelectedSequence == null) return;
        _navigationService.NavigateTo<SequenceDetailViewModel>(SelectedSequence.Id);
    }
    
    [RelayCommand]
    private void CreateSequence()
    {
        _navigationService.NavigateTo<SequenceDetailViewModel>();
    }
    
    [RelayCommand]
    private async Task DeleteSequenceAsync()
    {
        if (SelectedSequence == null) return;
        
        // Confirmation could be handled by DialogService
        await _sequenceService.DeleteAsync(SelectedSequence.Id);
        await LoadSequencesAsync();
    }
}
```

Key points to note:
- Services are injected through constructor
- Properties use `SetProperty` for change notification
- Commands encapsulate user actions
- Navigation is handled through a service
- Loading state is managed through `ExecuteWithLoadingAsync`

## UI Design and Material Design Integration

### Material Design Setup

Material Design is integrated through the MaterialDesignThemes.Wpf NuGet package. The proper way to configure Material Design in App.xaml is:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Material Design Resources -->
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml" />
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml" />
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Pink.xaml" />
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml" />
            
            <!-- Application Resources -->
            <ResourceDictionary Source="pack://application:,,,/Instrument.Data.UI;component/Resources/Colors.xaml" />
            <ResourceDictionary Source="pack://application:,,,/Instrument.Data.UI;component/Resources/Styles.xaml" />
        </ResourceDictionary.MergedDictionaries>
        
        <!-- Converters and other resources -->
    </ResourceDictionary>
</Application.Resources>
```

This setup:
1. Loads the base Material Design Light theme
2. Applies Blue primary and Pink accent colors
3. Loads default Material Design styles for common controls
4. Incorporates application-specific colors and styles

### Resource Organization

Resources are organized into separate files for better maintainability:

1. **Colors.xaml**: Defines color resources like `PrimaryBrush`, `AccentBrush`, `TextPrimaryBrush`, etc.
2. **Styles.xaml**: Defines styles for common UI elements based on Material Design styles

This organization makes it easier to:
- Maintain a consistent color scheme
- Apply theme changes globally
- Create custom styles that extend Material Design

### Custom Control Styles

The application extends Material Design styles with custom styles for specific UI patterns:

```xml
<!-- Button Styles -->
<Style x:Key="PrimaryButton" TargetType="Button" BasedOn="{StaticResource MaterialDesignRaisedButton}">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="materialDesign:ButtonAssist.CornerRadius" Value="4"/>
    <Setter Property="Padding" Value="16,8"/>
    <Setter Property="Margin" Value="0,8"/>
</Style>
```

These custom styles:
- Maintain Material Design principles
- Ensure consistency across the application
- Simplify styling of common UI elements

### View Layout Structure

Views follow a consistent layout pattern:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    
    <!-- Header Section -->
    <Grid Grid.Row="0" Margin="0,0,0,20">
        <!-- Title and Actions -->
    </Grid>
    
    <!-- Content Section -->
    <materialDesign:Card Grid.Row="1">
        <!-- Main Content -->
    </materialDesign:Card>
</Grid>
```

This structure provides:
- Consistent user experience across views
- Clear separation between header and content
- Material Design visual hierarchy

## Data Management and Entity Framework

### Entity Framework Integration

The UI project connects to the data layer through services defined in the Instrument.Data project. Entity Framework Core is used for data access, with repositories abstracting the database operations.

### Service Pattern

Services provide a clean API for ViewModels to interact with data:

```csharp
public interface ISequenceService
{
    Task<IEnumerable<Sequence>> GetAllAsync();
    Task<Sequence> GetByIdAsync(string id);
    Task<Sequence> CreateAsync(Sequence sequence);
    Task UpdateAsync(Sequence sequence);
    Task DeleteAsync(string id);
}

public class SequenceService : ISequenceService
{
    private readonly ISequenceRepository _repository;
    
    public SequenceService(ISequenceRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<IEnumerable<Sequence>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
    
    // Other methods implemented similarly
}
```

This pattern:
- Decouples ViewModels from data access details
- Enables mocking for unit tests
- Centralizes business logic
- Provides a clean API for UI operations

### Entity Structure

The data model includes entities like:
- Sequence
- SequenceGroup
- Parameter
- Range
- Resource

These entities reflect the domain model of the application and follow proper Entity Framework conventions.

## Navigation System

### Navigation Service

Navigation between views is handled by the `NavigationService` class:

```csharp
public interface INavigationService
{
    void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase;
    void GoBack();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _viewModelToViewMapping;
    
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // Define mappings between ViewModels and Views
        _viewModelToViewMapping = new Dictionary<Type, Type>
        {
            { typeof(SequencesViewModel), typeof(SequencesView) },
            { typeof(SequenceDetailViewModel), typeof(SequenceDetailView) },
            // Other mappings
        };
    }
    
    public void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        
        if (parameter != null && viewModel is IParameterizedNavigationAware paramViewModel)
        {
            paramViewModel.Initialize(parameter);
        }
        
        var viewType = _viewModelToViewMapping[typeof(TViewModel)];
        var view = (UserControl)Activator.CreateInstance(viewType);
        view.DataContext = viewModel;
        
        // Update the content of the main window
        // This depends on your specific navigation container approach
    }
    
    public void GoBack()
    {
        // Navigation history management
    }
}
```

This service:
- Resolves ViewModels from the dependency injection container
- Maps ViewModels to their corresponding Views
- Manages navigation parameters
- Supports back navigation

### Content Region

The MainWindow contains a content region for displaying the current view:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    
    <!-- App Bar -->
    <materialDesign:ColorZone Grid.Row="0" Mode="PrimaryMid" Padding="16">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="Instrument Data Manager" FontSize="20" VerticalAlignment="Center"/>
            <!-- Navigation menu items could go here -->
        </StackPanel>
    </materialDesign:ColorZone>
    
    <!-- Content Region -->
    <ContentControl Grid.Row="1" Content="{Binding CurrentView}" Margin="16"/>
</Grid>
```

This approach:
- Provides a consistent frame for all views
- Centralizes navigation control
- Maintains the application's visual hierarchy

## Common Controls and Patterns

### Card Pattern

Material Design Cards are used extensively for content sections:

```xml
<materialDesign:Card Padding="16" Margin="0,0,0,16">
    <StackPanel>
        <TextBlock Text="Section Title" Style="{StaticResource HeadingMedium}"/>
        <!-- Content -->
    </StackPanel>
</materialDesign:Card>
```

Cards provide:
- Visual separation of content
- Elevation and shadow effects
- Consistent padding and spacing

### Form Layout Pattern

Forms follow a consistent layout pattern:

```xml
<StackPanel>
    <TextBlock Text="Sequence Details" Style="{StaticResource HeadingMedium}"/>
    
    <TextBox 
        Text="{Binding Name}" 
        Style="{StaticResource FormTextBox}"
        materialDesign:HintAssist.Hint="Sequence Name"/>
    
    <TextBox 
        Text="{Binding Description}"
        Style="{StaticResource FormTextBox}"
        materialDesign:HintAssist.Hint="Description"
        TextWrapping="Wrap"
        AcceptsReturn="True"
        Height="80"/>
    
    <CheckBox 
        IsChecked="{Binding CanBeParallel}"
        Content="Can Run in Parallel"
        Style="{StaticResource FormCheckBox}"/>
    
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
        <Button 
            Content="Cancel" 
            Command="{Binding CancelCommand}"
            Style="{StaticResource SecondaryButton}"
            Margin="0,0,8,0"/>
        
        <Button 
            Content="Save" 
            Command="{Binding SaveCommand}"
            Style="{StaticResource PrimaryButton}"/>
    </StackPanel>
</StackPanel>
```

This pattern:
- Aligns form elements consistently
- Uses Material Design hints for labels
- Provides consistent spacing
- Places action buttons in a predictable location

### List View Pattern

Lists of items follow a consistent pattern:

```xml
<ListView 
    ItemsSource="{Binding Items}"
    SelectedItem="{Binding SelectedItem}"
    Style="{StaticResource DataListViewStyle}">
    <ListView.ItemTemplate>
        <DataTemplate>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <TextBlock 
                    Grid.Column="0"
                    Text="{Binding Name}"
                    FontWeight="Medium"/>
                
                <StackPanel 
                    Grid.Column="1" 
                    Orientation="Horizontal">
                    <Button 
                        Content="View" 
                        Command="{Binding DataContext.ViewItemCommand, 
                                  RelativeSource={RelativeSource AncestorType=ListView}}"
                        Style="{StaticResource FlatButton}"/>
                </StackPanel>
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

This pattern:
- Provides consistent item display
- Supports selection
- Includes consistent action buttons
- Uses Material Design styling

## Resource Management

### Resource Dictionary Organization

Resource dictionaries are organized hierarchically:

1. **Material Design base themes** (in App.xaml)
2. **Colors.xaml** for application-specific colors
3. **Styles.xaml** for application-specific styles

This organization provides:
- Clear separation of concerns
- Proper theme inheritance
- Centralized control over visual elements

### Resource Naming Conventions

Resources follow consistent naming conventions:

- Colors: `[Purpose]Brush` (e.g., `PrimaryBrush`, `TextPrimaryBrush`)
- Styles: `[ElementType]` or `[Purpose][ElementType]` (e.g., `HeadingMedium`, `PrimaryButton`)

These conventions make it easier to:
- Find resources by purpose
- Understand the intended use of resources
- Maintain consistency in naming

### Value Converters

Value converters are defined in a centralized location and used for data binding transformations:

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

These converters:
- Simplify XAML bindings
- Centralize conversion logic
- Enable reuse across the application

## Dependency Injection

### DI Container Setup

Dependency injection is configured in Program.cs:

```csharp
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();
        
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SequencesViewModel>();
        services.AddTransient<SequenceDetailViewModel>();
        // Other ViewModels
        
        // Register data services
        services.AddScoped<ISequenceService, SequenceService>();
        // Other services
    }
}
```

This setup:
- Uses Microsoft.Extensions.DependencyInjection
- Registers services with appropriate lifetimes
- Configures ViewModels for injection
- Integrates with the WPF application lifecycle

### Service Lifetimes

Different service lifetimes are used appropriately:

- **Singleton**: Services that should have one instance for the application (NavigationService, DialogService)
- **Transient**: ViewModels that should be created each time they're requested
- **Scoped**: Services with a lifetime tied to a specific operation or context

## Common Pitfalls and Troubleshooting

### Material Design Integration Issues

One common issue is incorrectly configuring Material Design resources:

**Problem**: Resources not found, such as `MaterialDesignOutlinedButton`

**Solution**: Ensure proper resource dictionary loading order:
1. First load MaterialDesignTheme.Light.xaml
2. Then load color themes (Primary and Accent)
3. Then load MaterialDesignTheme.Defaults.xaml
4. Finally load application-specific resources

**Incorrect**:
```xml
<materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="Blue" SecondaryColor="Pink" />
```

**Correct**:
```xml
<ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml" />
<ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml" />
<ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Pink.xaml" />
```

### Resource Dictionary Conflicts

Another common issue is loading the same resources multiple times:

**Problem**: Resource dictionary conflicts causing unpredictable styling

**Solution**: Only include each Material Design resource once, typically in App.xaml

### Performance Considerations

WPF applications can face performance issues due to:

1. **Excessive property change notifications**: Use throttling or debouncing for high-frequency updates
2. **Heavy resource dictionaries**: Split resources into logical files and only load what's needed
3. **Unoptimized data binding**: Use virtualization for large lists
4. **UI thread blocking**: Move heavy operations to background threads

### ObservableCollection Updates

Updates to ObservableCollection should be handled carefully:

**Problem**: Collection modified on background thread causing cross-thread access exceptions

**Solution**: Use dispatcher to update collections:
```csharp
await Dispatcher.InvokeAsync(() => {
    Sequences.Clear();
    foreach (var sequence in sequencesFromService)
    {
        Sequences.Add(sequence);
    }
});
```

## Testing Strategies

### ViewModel Testing

ViewModels can be tested using standard unit testing techniques:

```csharp
[Fact]
public async Task LoadSequencesCommand_ShouldPopulateSequencesCollection()
{
    // Arrange
    var mockService = new Mock<ISequenceService>();
    mockService.Setup(s => s.GetAllAsync())
        .ReturnsAsync(new List<Sequence> 
        { 
            new Sequence { Id = "1", Name = "Test Sequence" } 
        });
    
    var viewModel = new SequencesViewModel(mockService.Object, Mock.Of<INavigationService>());
    
    // Act
    await viewModel.LoadSequencesCommand.ExecuteAsync(null);
    
    // Assert
    Assert.Single(viewModel.Sequences);
    Assert.Equal("Test Sequence", viewModel.Sequences.First().Name);
}
```

Key testing strategies:
- Mock dependencies using a mocking framework like Moq
- Test command execution and property changes
- Verify that ViewModels interact correctly with services

### UI Testing

UI testing can be performed using:
1. **Manual testing** for visual verification
2. **Automated UI tests** with tools like White or WPF UI Automation
3. **Integration tests** that verify the interaction between components

## Conclusion

Building a WPF application with Material Design involves:
1. Following the MVVM pattern for separation of concerns
2. Properly configuring Material Design resources
3. Organizing code into a maintainable structure
4. Using dependency injection for loosely coupled components
5. Creating consistent UI patterns
6. Managing resources effectively
7. Testing components at various levels

By understanding these concepts and patterns, developers can create maintainable, testable, and visually appealing WPF applications with Material Design.

The Instrument.Data.UI project demonstrates these principles in a real-world application, providing a solid foundation for similar projects.
