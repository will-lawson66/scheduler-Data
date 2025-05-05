# C# Presentation Layer Structure: A Comprehensive Guide

## Introduction

The presentation layer is a crucial component of any modern application, serving as the bridge between users and the underlying business logic. This document outlines a robust, maintainable structure for C# presentation layers that can be implemented across WPF, Avalonia, and .NET MAUI. By following these guidelines, developers can create UI applications that are testable, maintainable, and extensible regardless of the specific UI framework.

## Core Architectural Pattern: MVVM

The Model-View-ViewModel (MVVM) pattern is the foundation of our presentation layer design. MVVM enables a clear separation of concerns and facilitates unit testing of UI logic.

### Key Components

- **Model**: Represents the data and business logic from your domain/application layer
- **View**: The UI elements users interact with
- **ViewModel**: Mediates between View and Model, exposing data and commands to the View

## Presentation Layer Directory Structure

```
PresentationLayer/
├── App.xaml / App.xaml.cs           # Application entry point
├── Assets/                          # Static resources
│   ├── Fonts/
│   ├── Images/
│   └── Styles/                      # Global styles and themes
├── Controls/                        # Custom controls
│   └── Specialized/                 # Domain-specific controls
├── Behaviors/                       # Reusable UI behaviors
├── Converters/                      # Value converters
├── Extensions/                      # UI-specific extension methods
├── Helpers/                         # UI utility classes
├── Models/                          # UI-specific models (DTOs)
├── Resources/                       # Resource dictionaries
│   ├── Colors.xaml
│   ├── Icons.xaml
│   └── Styles.xaml
├── Services/                        # UI-specific services
│   ├── Interfaces/                  # Service interfaces
│   ├── Navigation/                  # Navigation service
│   ├── Dialog/                      # Dialog service
│   └── Implementations/             # Service implementations
├── Validation/                      # Input validation logic
├── ViewModels/                      # ViewModels
│   ├── Base/                        # Base ViewModel classes
│   └── Dialogs/                     # Dialog ViewModels
├── Views/                           # UI Views
│   ├── Pages/                       # Main page views
│   ├── Dialogs/                     # Dialog views
│   └── Controls/                    # View-specific controls
└── DependencyInjection/             # DI container setup for UI
```

## Detailed Component Breakdown

### 1. Assets

**Purpose**: Centralize static resources used throughout the application.

```
Assets/
├── Fonts/                # Custom fonts
├── Images/               # Images and icons 
│   ├── Icons/
│   └── Backgrounds/
└── Styles/               # Global style definitions
    ├── Dark/
    └── Light/
```

**Reasoning**: 
- Separates resources from code
- Makes resources easier to manage and update
- Facilitates theming and branding changes
- Enables resource optimization

### 2. Controls

**Purpose**: House custom UI controls that are reusable across multiple views.

```
Controls/
├── Base/                     # Base control classes
├── Common/                   # General-purpose controls
│   ├── EnhancedButton.xaml
│   ├── SearchBox.xaml
│   └── ...
└── Specialized/              # Domain-specific controls
    ├── DataVisualizers/
    └── DomainSpecific/
```

**Reasoning**:
- Promotes UI consistency
- Reduces code duplication
- Makes maintenance easier
- Allows specialized controls to evolve independently

### 3. Behaviors

**Purpose**: Encapsulate reusable UI interactions and behaviors.

```
Behaviors/
├── DragDropBehavior.cs
├── FocusBehavior.cs
├── ValidationBehavior.cs
└── ...
```

**Reasoning**:
- Separates interaction logic from visual elements
- Enables reuse of complex behaviors
- Makes UI interaction patterns consistent
- Improves testability of UI interactions

### 4. Converters

**Purpose**: Transform data between the format used in the ViewModel and what's needed for display.

```
Converters/
├── BooleanToVisibilityConverter.cs
├── DateTimeFormatConverter.cs
├── EnumToStringConverter.cs
└── ...
```

**Reasoning**:
- Keeps View and ViewModel cleanly separated
- Centralizes data transformation logic
- Makes UI bindings more powerful
- Improves readability of XAML

### 5. Extensions

**Purpose**: Contain UI-specific extension methods that enhance the framework.

```
Extensions/
├── ControlExtensions.cs
├── BindingExtensions.cs
├── ElementExtensions.cs
└── ...
```

**Reasoning**:
- Makes common UI operations more concise
- Improves code readability
- Encapsulates framework-specific workarounds
- Provides consistent solutions to common problems

### 6. Helpers

**Purpose**: Provide utility functions specific to UI operations.

```
Helpers/
├── ThemeHelper.cs
├── ScreenHelper.cs
├── UIDispatcherHelper.cs
└── ...
```

**Reasoning**:
- Centralizes common UI utility functions
- Abstracts platform-specific implementations
- Simplifies complex UI operations
- Improves code reuse across views

### 7. Models

**Purpose**: Define UI-specific data structures distinct from domain models.

```
Models/
├── UI/
│   ├── MenuItem.cs
│   ├── NotificationItem.cs
│   └── ...
└── DTOs/
    ├── UserProfileDto.cs
    └── ...
```

**Reasoning**:
- Separates UI-specific data structures from domain models
- Prevents UI concerns from leaking into domain layer
- Optimizes data structures for UI consumption
- Facilitates transformation between domain and UI models

### 8. Resources

**Purpose**: Centralize reusable resource dictionaries.

```
Resources/
├── Colors.xaml
├── Icons.xaml
├── Brushes.xaml
├── TextStyles.xaml
└── ControlStyles.xaml
```

**Reasoning**:
- Promotes consistent visual design
- Makes theming and branding easier to manage
- Improves application maintainability
- Reduces duplication of styling code

### 9. Services

**Purpose**: Provide UI-specific functionality isolated from business logic.

```
Services/
├── Interfaces/
│   ├── INavigationService.cs
│   ├── IDialogService.cs
│   ├── IThemeService.cs
│   └── ...
└── Implementations/
    ├── NavigationService.cs
    ├── DialogService.cs
    ├── ThemeService.cs
    └── ...
```

**Reasoning**:
- Abstracts platform-specific functionality
- Enables unit testing through interface-based design
- Facilitates clean separation of concerns
- Makes the UI layer more modular and testable

### 10. Validation

**Purpose**: Handle UI input validation separate from domain validation.

```
Validation/
├── Rules/
│   ├── RequiredFieldRule.cs
│   ├── EmailValidationRule.cs
│   └── ...
├── Validators/
│   ├── UserInputValidator.cs
│   └── ...
└── Behaviors/
    ├── ValidationBehavior.cs
    └── ...
```

**Reasoning**:
- Separates UI validation from domain validation
- Provides immediate user feedback
- Improves user experience
- Makes validation rules reusable across the application

### 11. ViewModels

**Purpose**: Contain the presentation logic and state for views.

```
ViewModels/
├── Base/
│   ├── ViewModelBase.cs
│   └── ...
├── Pages/
│   ├── HomeViewModel.cs
│   ├── ProfileViewModel.cs
│   └── ...
└── Dialogs/
    ├── ConfirmationDialogViewModel.cs
    └── ...
```

**Reasoning**:
- Encapsulates UI logic separate from views
- Makes UI testable through unit tests
- Provides clean data binding surface for views
- Follows MVVM pattern for maintainability

### 12. Views

**Purpose**: Define the visual elements and user interface.

```
Views/
├── Pages/
│   ├── HomePage.xaml
│   ├── ProfilePage.xaml
│   └── ...
├── Dialogs/
│   ├── ConfirmationDialog.xaml
│   └── ...
└── Controls/
    ├── PageHeader.xaml
    └── ...
```

**Reasoning**:
- Organizes UI by functional area
- Separates different types of views (pages vs. dialogs)
- Makes navigation structure clear
- Follows MVVM pattern for clean separation from logic

### 13. DependencyInjection

**Purpose**: Configure dependency injection specific to the UI layer.

```
DependencyInjection/
├── ViewModelLocator.cs
├── ServiceCollectionExtensions.cs
└── ...
```

**Reasoning**:
- Centralizes UI dependency registration
- Makes service composition explicit
- Simplifies testing by allowing service substitution
- Improves maintainability by decoupling dependencies

## Cross-Technology Considerations

### WPF-Specific Considerations

- Use `App.xaml` for application-wide resources
- Take advantage of WPF's rich binding system
- Utilize `DataTemplates` for view selection
- Consider using the XAML Hot Reload feature for development

### Avalonia-Specific Considerations

- Use `App.axaml` for application resources
- Remember platform-specific style differences
- Utilize Avalonia's cross-platform capabilities
- Consider ReactiveUI integration for reactive programming

### .NET MAUI-Specific Considerations

- Organize platform-specific code in platform folders
- Use MAUI's resource system for cross-platform resources
- Consider Shell navigation for mobile-centric applications
- Utilize Handlers for custom rendering on each platform

## Navigation Strategies

### Centralized Navigation Service

Regardless of the technology, implement a navigation service:

```csharp
public interface INavigationService
{
    Task NavigateToAsync<TViewModel>(object parameter = null);
    Task GoBackAsync();
    Task ShowDialogAsync<TDialogViewModel>(object parameter = null);
}
```

Implementation will differ slightly between platforms:
- WPF: Uses `Frame` or custom region management
- Avalonia: Uses `IViewLocator` and window management
- MAUI: Uses `Shell` navigation or `NavigationPage`

### Benefits:

- Decouples navigation logic from ViewModels
- Makes navigation testable
- Centralizes navigation state
- Simplifies deep linking

## State Management

### ViewModel State

ViewModels should manage UI state using:

- Properties with `INotifyPropertyChanged`
- Commands for user actions
- Observable collections for lists
- State properties (IsLoading, HasErrors, etc.)

### Application State

For broader application state:

- Consider a state service for cross-ViewModel state
- Use messenger patterns for ViewModel-to-ViewModel communication
- Implement state persistence for app lifecycle events

## Testing Strategies

### Unit Testing ViewModels

- Test ViewModel logic independently of UI
- Mock dependencies using interfaces
- Verify property changes and command behavior
- Test navigation requests via the navigation service

### UI Automation Testing

- Structure Views to enable UI automation
- Use `AutomationId` or equivalent for test hooks
- Consider MVVM-friendly UI testing frameworks
- Implement test helpers for common UI operations

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

## Conclusion

A well-structured presentation layer is essential for creating maintainable, testable, and extensible UI applications. The architecture described in this document provides a solid foundation that works across WPF, Avalonia, and .NET MAUI, while allowing for framework-specific optimizations. By following these guidelines, developers can create applications with a clean separation of concerns, enabling easier maintenance and evolution over time.

## Appendix: Common Patterns and Examples

### Command Pattern Implementation

```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
```

### Base ViewModel Implementation

```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

### Navigation Service Example

```csharp
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _viewModelToViewMapping;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _viewModelToViewMapping = new Dictionary<Type, Type>();
        
        // Register view mappings
        RegisterViewMapping<HomeViewModel, HomePage>();
        RegisterViewMapping<ProfileViewModel, ProfilePage>();
    }

    public void RegisterViewMapping<TViewModel, TView>()
        where TViewModel : class
        where TView : class
    {
        _viewModelToViewMapping[typeof(TViewModel)] = typeof(TView);
    }

    public async Task NavigateToAsync<TViewModel>(object parameter = null)
    {
        // Implementation differs based on UI framework
        // This is a conceptual example
        
        var viewModelType = typeof(TViewModel);
        if (!_viewModelToViewMapping.TryGetValue(viewModelType, out var viewType))
            throw new Exception($"No view mapping found for {viewModelType.Name}");
            
        var viewModel = _serviceProvider.GetService(viewModelType) as TViewModel;
        var view = Activator.CreateInstance(viewType) as Page;
        
        view.BindingContext = viewModel;
        
        // Initialize ViewModel if needed
        if (viewModel is IInitializable initializable)
            await initializable.InitializeAsync(parameter);
            
        // Actual navigation depends on framework
        // For example in MAUI:
        await Shell.Current.Navigation.PushAsync(view);
    }
    
    // Other implementation details...
}
```
