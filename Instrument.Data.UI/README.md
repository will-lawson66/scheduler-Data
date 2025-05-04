# Instrument.Data.UI Architecture

This document outlines the architecture and organization of the Instrument.Data.UI project.

## Directory Structure

The UI project follows a clean, organized structure based on the MVVM (Model-View-ViewModel) pattern:

```
Instrument.Data.UI/
├── App.xaml / App.xaml.cs           # Application entry point
├── Assets/                          # Static resources
│   ├── Fonts/
│   ├── Images/
│   └── Styles/                      # Global styles and themes
├── Behaviors/                       # Reusable UI behaviors
├── Controls/                        # Custom controls
├── Converters/                      # Value converters
├── DependencyInjection/             # DI container setup
├── Extensions/                      # UI-specific extension methods
├── Helpers/                         # UI utility classes
├── Models/                          # UI-specific models
├── Resources/                       # Resource dictionaries
│   ├── Colors.xaml
│   ├── Styles.xaml
│   └── Themes/
├── Services/                        # UI-specific services
│   ├── Dialog/                      # Dialog service
│   ├── Interfaces/                  # Service interfaces
│   └── Navigation/                  # Navigation service
├── Validation/                      # Input validation
├── ViewModels/                      # ViewModels
│   └── Base/                        # Base ViewModel classes
└── Views/                           # UI Views
    ├── Controls/                    # View-specific controls
    ├── Dialogs/                     # Dialog views
    └── Pages/                       # Main page views
```

## Key Components

### ViewModels

ViewModels serve as the intermediary between the Views and the underlying data/business logic. All ViewModels inherit from `ViewModelBase` which provides common functionality such as:

- Property change notification
- Busy state management
- Error handling

### Navigation

The application uses a centralized navigation service (`INavigationService`) to handle navigation between views. This service:

- Maintains a mapping between ViewModels and Views
- Handles navigation within the main frame
- Manages dialog presentation

### Services

Services provide functionality to the UI layer, including:

- **Dialog Service**: Manages showing dialogs and message boxes
- **Navigation Service**: Handles navigation between views

### Resource Management

Resources are organized into separate XAML files:

- **Colors.xaml**: Contains color definitions
- **Styles.xaml**: Contains control styles

### Converters

Value converters transform data for display in the UI:

- `BooleanToVisibilityConverter`: Converts boolean values to `Visibility` values
- `InverseBooleanToVisibilityConverter`: Inverts boolean values before converting to `Visibility`
- `NullValueToBooleanConverter`: Converts null/non-null values to boolean values
- `InverseNullValueToBooleanConverter`: Converts null/non-null values to boolean values (inverted)

## Usage Guidelines

### Adding a New View

1. Create a new View in the `Views` directory
2. Create a corresponding ViewModel in the `ViewModels` directory
3. Register the View-ViewModel mapping in `ServiceCollectionExtensions.cs`

### Adding a New Service

1. Define the service interface in `Services/Interfaces`
2. Implement the service in the appropriate service directory
3. Register the service in `ServiceCollectionExtensions.cs`

### Styling Guidelines

1. Use existing styles from `Styles.xaml` where possible
2. Define new styles in the appropriate resource dictionary
3. Follow the naming convention: `[Purpose][ElementType]` (e.g., `PrimaryButton`)

## Dependencies

The UI layer depends on the following external packages:

- **MaterialDesignThemes**: Provides the material design styling
- **CommunityToolkit.Mvvm**: Provides MVVM infrastructure
- **Microsoft.Extensions.DependencyInjection**: Handles dependency injection
- **Microsoft.Xaml.Behaviors.Wpf**: Provides behavior support

## Best Practices

1. Keep Views as simple as possible, with minimal code-behind
2. Use data binding to connect Views and ViewModels
3. Use commands for user interactions
4. Keep UI-specific logic in the ViewModel
5. Use services to abstract platform-specific functionality
6. Follow the single responsibility principle for all components
7. Maintain a clean separation between UI and business logic
