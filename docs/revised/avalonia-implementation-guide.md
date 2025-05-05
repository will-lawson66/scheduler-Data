# Migrating from WPF to Avalonia: Implementation Guide for Instrument.Data

## Table of Contents

1. [Introduction](#introduction)
2. [Avalonia Overview](#avalonia-overview)
3. [Project Setup](#project-setup)
4. [MVVM Implementation](#mvvm-implementation)
5. [UI Component Conversion](#ui-component-conversion)
6. [Styling and Theming](#styling-and-theming)
7. [Navigation System](#navigation-system)
8. [Dependency Injection](#dependency-injection)
9. [Responsive Design](#responsive-design)
10. [Testing and Validation](#testing-and-validation)
11. [Migration Strategy](#migration-strategy)

## Introduction

This guide provides detailed instructions for migrating the Instrument.Data UI from WPF with Material Design to Avalonia. Avalonia is a cross-platform UI framework that enables running the same UI code on Windows, macOS, Linux, iOS, Android, and WebAssembly. By migrating to Avalonia, the Instrument.Data application can extend its reach beyond Windows while maintaining a similar architectural pattern.

## Avalonia Overview

Avalonia is a cross-platform .NET UI framework inspired by WPF but designed to be more modern and flexible. Key benefits include:

- **Cross-platform support**: Run on Windows, macOS, Linux, iOS, Android, and WebAssembly
- **Modern architecture**: MVVM-friendly design with reactive programming support
- **Familiar syntax**: XAML-based UI definition similar to WPF but with platform-specific enhancements
- **Active community**: Growing ecosystem with regular updates and improvements
- **Styling system**: Flexible styling similar to CSS selectors

While Avalonia shares many concepts with WPF, there are important differences to consider:

- Different namespace structure and control hierarchy
- Some WPF-specific features aren't available (like DependencyProperty implementation)
- Different binding syntax in some scenarios
- Different resource system for assets and themes
- Platform-specific considerations for deployment

## Project Setup

### 1. Create a New Project

First, install the Avalonia templates and create a new project:

```bash
dotnet new install Avalonia.Templates
dotnet new avalonia.mvvm -o Instrument.Data.Avalonia
```

### 2. Update Dependencies

Modify the project file to include necessary dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.0.5" />
    <PackageReference Include="Avalonia.Desktop" Version="11.0.5" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.0.5" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.0.5" />
    <PackageReference Include="Avalonia.ReactiveUI" Version="11.0.5" />
    <PackageReference Include="Material.Avalonia" Version="3.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Instrument.Data\Instrument.Data.csproj" />
  </ItemGroup>
</Project>
```

### 3. Project Structure

Set up a structure similar to the WPF project:

```
Instrument.Data.Avalonia/
├── App.axaml                # Application entry point
├── Assets/                  # Static resources
│   ├── Icons/
│   └── Styles/              # Global styles and themes
├── Controls/                # Custom controls
├── Converters/              # Value converters
├── DependencyInjection/     # DI container setup
├── Helpers/                 # UI utility classes
├── Models/                  # UI-specific models
├── Program.cs               # .NET Core entry point
├── Services/                # UI-specific services
│   ├── Interfaces/          # Service interfaces
│   ├── Navigation/          # Navigation service
│   └── Dialog/              # Dialog service
├── Styles/                  # Resource dictionaries
│   ├── Colors.axaml
│   └── Themes.axaml
├── ViewModels/              # ViewModels
│   ├── Base/                # Base ViewModel classes
│   └── ViewModelLocator.cs
├── Views/                   # UI Views
│   ├── MainWindow.axaml     # Main application window
│   ├── SequencesView.axaml
│   └── SequenceDetailView.axaml
└── App.axaml.cs             # Application logic
```

### 4. Program Entry Point

Create the entry point with dependency injection:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Instrument.Data.Configuration;
using Instrument.Data.Initialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Instrument.Data.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        // Initialize application services
        using (var scope = host.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<IDataInitializer>();
            initializer.InitializeAsync().Wait();
        }
        
        // Start Avalonia UI
        BuildAvaloniaApp(host).StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IHost host)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .With(new AvaloniaAppBuilderSettings { ServiceProvider = host.Services });

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // 1. Configure storage
                var storageConfig = new StorageConfiguration
                {
                    Provider = StorageProviderType.SQLite,
                    ConnectionString = "Data Source=Instrument.db"
                };
                
                // 2. Register data layer
                services.AddInstrumentData(storageConfig);
                services.AddDataInitialization();
                
                // 3. Register UI services
                RegisterServices(services);
                
                // 4. Register ViewModels
                RegisterViewModels(services);
            });

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        // Other services
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SequencesViewModel>();
        services.AddTransient<SequenceDetailViewModel>();
        // Other ViewModels
    }
}
```

### 5. Application Class

Implement the Application class:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Instrument.Data.Avalonia;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Get the host's service provider
            var serviceProvider = (IServiceProvider)AvaloniaLocator.Current.GetService(typeof(IServiceProvider));
            
            // Create main window with ViewModel from DI
            desktop.MainWindow = new MainWindow
            {
                DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

## MVVM Implementation

### 1. Base ViewModel Implementation

Create a base ViewModel class similar to the WPF version, but using ReactiveUI:

```csharp
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Instrument.Data.Avalonia.ViewModels.Base;

public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel, IDisposable
{
    private bool _isLoading;
    private string _title = string.Empty;
    private readonly CompositeDisposable _disposables = new();
    
    public ViewModelActivator Activator { get; } = new ViewModelActivator();
    
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
    
    protected ViewModelBase()
    {
        this.WhenActivated(disposables =>
        {
            HandleActivation();
            
            Disposable
                .Create(HandleDeactivation)
                .DisposeWith(disposables);
        });
    }
    
    protected virtual void HandleActivation() { }
    
    protected virtual void HandleDeactivation() { }
    
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
    
    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### 2. ViewModel Implementation

Implement a ViewModel using ReactiveUI patterns:

```csharp
using Instrument.Data.Avalonia.Services.Interfaces;
using Instrument.Data.Avalonia.ViewModels.Base;
using Instrument.Data.Entities;
using Instrument.Data.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Instrument.Data.Avalonia.ViewModels;

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
        set => this.RaiseAndSetIfChanged(ref _sequences, value);
    }
    
    public Sequence? SelectedSequence
    {
        get => _selectedSequence;
        set => this.RaiseAndSetIfChanged(ref _selectedSequence, value);
    }
    
    public bool HasNoSequences => !Sequences.Any();
    
    public ReactiveCommand<Unit, Unit> LoadSequencesCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateSequenceCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewSequenceCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSequenceCommand { get; }
    
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
        
        LoadSequencesCommand = ReactiveCommand.CreateFromTask(LoadSequencesAsync);
        
        CreateSequenceCommand = ReactiveCommand.Create(() => 
            _navigationService.NavigateTo<SequenceDetailViewModel>());
        
        ViewSequenceCommand = ReactiveCommand.Create(
            () => _navigationService.NavigateTo<SequenceDetailViewModel>(SelectedSequence?.Id),
            this.WhenAnyValue(x => x.SelectedSequence).Select(x => x != null));
        
        DeleteSequenceCommand = ReactiveCommand.CreateFromTask(
            DeleteSequenceAsync,
            this.WhenAnyValue(x => x.SelectedSequence).Select(x => x != null));
        
        // Observable for property changes
        this.WhenAnyValue(x => x.Sequences.Count)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasNoSequences)));
    }
    
    protected override void HandleActivation()
    {
        // Load data when activated
        _ = LoadSequencesAsync();
    }
    
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sequences");
                await _dialogService.ShowErrorAsync("Error", $"Failed to load sequences: {ex.Message}");
            }
        });
    }
    
    private async Task DeleteSequenceAsync()
    {
        if (SelectedSequence == null) return;
        
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Confirm Delete", 
            $"Are you sure you want to delete the sequence '{SelectedSequence.Name}'?");
            
        if (!confirmed) return;
        
        await ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                await _sequenceService.DeleteSequenceAsync(SelectedSequence.Id);
                Sequences.Remove(SelectedSequence);
                SelectedSequence = null;
                
                await _dialogService.ShowInformationAsync("Success", "Sequence deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete sequence {Id}", SelectedSequence.Id);
                await _dialogService.ShowErrorAsync("Error", $"Failed to delete sequence: {ex.Message}");
            }
        });
    }
}
```

## UI Component Conversion

### 1. Main Window

The main window in Avalonia:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Instrument.Data.Avalonia.ViewModels"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:material="using:Material.Styles"
        mc:Ignorable="d" d:DesignWidth="1200" d:DesignHeight="750"
        Width="1200" Height="750"
        x:Class="Instrument.Data.Avalonia.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Icon="/Assets/Icons/app-icon.ico"
        Title="Instrument Data Manager"
        WindowStartupLocation="CenterScreen">
        
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- App Bar -->
        <material:ColorZone Grid.Row="0" Mode="PrimaryDark" Padding="16" Elevation="2">
            <DockPanel>
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <material:PackIcon Kind="Database" Width="24" Height="24" VerticalAlignment="Center"/>
                    <TextBlock Text="Instrument Data Manager" 
                              FontSize="20"
                              FontWeight="Medium"
                              VerticalAlignment="Center"/>
                </StackPanel>
            </DockPanel>
        </material:ColorZone>
        
        <!-- Main Content -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="250"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- Navigation Menu -->
            <material:Card Grid.Column="0" Margin="8" Elevation="1">
                <ScrollViewer>
                    <StackPanel Margin="0,20,0,0">
                        <TextBlock Text="ENTITIES" 
                                  Margin="16,0,0,8" 
                                  Foreground="{DynamicResource MaterialBodyTextLightBrush}" 
                                  FontSize="12"/>
                        
                        <ListBox Name="NavigationItems" 
                                SelectedIndex="{Binding SelectedViewIndex}"
                                Margin="0,8">
                            <ListBoxItem>
                                <StackPanel Orientation="Horizontal" Spacing="8">
                                    <material:PackIcon Kind="ViewSequential" Width="24" Height="24"/>
                                    <TextBlock Text="Sequences" VerticalAlignment="Center"/>
                                </StackPanel>
                            </ListBoxItem>
                            <ListBoxItem>
                                <StackPanel Orientation="Horizontal" Spacing="8">
                                    <material:PackIcon Kind="Function" Width="24" Height="24"/>
                                    <TextBlock Text="Parameters" VerticalAlignment="Center"/>
                                </StackPanel>
                            </ListBoxItem>
                            <!-- More menu items -->
                        </ListBox>
                    </StackPanel>
                </ScrollViewer>
            </material:Card>
            
            <!-- Content Area -->
            <Grid Grid.Column="1">
                <material:Card Margin="16" Padding="16" Elevation="1">
                    <ContentControl Content="{Binding CurrentView}"/>
                </material:Card>
            </Grid>
        </Grid>
    </Grid>
</Window>
```

### 2. Sequences View

A typical list view implementation:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="using:Instrument.Data.Avalonia.ViewModels"
             xmlns:material="using:Material.Styles"
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
                
                <!-- Data list -->
                <ListBox ItemsSource="{Binding Sequences}"
                       Selection="{Binding SelectedSequence}"
                       IsVisible="{Binding !HasNoSequences}">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <Grid Margin="8">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>
                                
                                <StackPanel Grid.Column="0">
                                    <TextBlock Text="{Binding Name}"
                                              FontWeight="Medium"
                                              FontSize="16"/>
                                    <TextBlock Text="{Binding Description}"
                                              TextWrapping="Wrap"/>
                                </StackPanel>
                                
                                <StackPanel Grid.Column="1" 
                                          Orientation="Horizontal"
                                          Spacing="8"
                                          VerticalAlignment="Center">
                                    <Button Command="{Binding $parent[ListBox].DataContext.ViewSequenceCommand}"
                                           Classes="icon">
                                        <material:PackIcon Kind="Eye"/>
                                    </Button>
                                    <Button Command="{Binding $parent[ListBox].DataContext.DeleteSequenceCommand}"
                                           Classes="icon">
                                        <material:PackIcon Kind="Delete"/>
                                    </Button>
                                </StackPanel>
                            </Grid>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </Grid>
        </material:Card>
    </Grid>
</UserControl>
```

## Styling and Theming

### 1. Material Design for Avalonia

Integrate Material.Avalonia to provide Material Design styling:

```xml
<!-- App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:material="using:Material.Styles"
             x:Class="Instrument.Data.Avalonia.App">
    <Application.Styles>
        <!-- Material Theme -->
        <material:MaterialTheme BaseTheme="Light" PrimaryColor="Blue" SecondaryColor="Indigo"/>
        
        <!-- Application-specific styles -->
        <StyleInclude Source="/Styles/Colors.axaml"/>
        <StyleInclude Source="/Styles/Themes.axaml"/>
    </Application.Styles>
    
    <Application.Resources>
        <!-- Global resources -->
    </Application.Resources>
</Application>
```

### 2. Custom Colors

Define custom colors similar to the WPF implementation:

```xml
<!-- Styles/Colors.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Primary colors -->
    <Color x:Key="PrimaryColor">#1976D2</Color>
    <Color x:Key="PrimaryLightColor">#42A5F5</Color>
    <Color x:Key="PrimaryDarkColor">#0D47A1</Color>
    
    <!-- Accent colors -->
    <Color x:Key="AccentColor">#5C6BC0</Color>
    <Color x:Key="AccentLightColor">#8E99F3</Color>
    <Color x:Key="AccentDarkColor">#3949AB</Color>
    
    <!-- Text colors -->
    <Color x:Key="TextPrimaryColor">#212121</Color>
    <Color x:Key="TextSecondaryColor">#757575</Color>
    <Color x:Key="TextDisabledColor">#BDBDBD</Color>
    
    <!-- Background colors -->
    <Color x:Key="BackgroundColor">#FAFAFA</Color>
    <Color x:Key="SurfaceColor">#FFFFFF</Color>
    <Color x:Key="ErrorColor">#F44336</Color>
    <Color x:Key="WarningColor">#FF9800</Color>
    <Color x:Key="SuccessColor">#4CAF50</Color>
    
    <!-- Brushes -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <SolidColorBrush x:Key="PrimaryLightBrush" Color="{StaticResource PrimaryLightColor}" />
    <SolidColorBrush x:Key="PrimaryDarkBrush" Color="{StaticResource PrimaryDarkColor}" />
    
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="AccentLightBrush" Color="{StaticResource AccentLightColor}" />
    <SolidColorBrush x:Key="AccentDarkBrush" Color="{StaticResource AccentDarkColor}" />
    
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}" />
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}" />
    <SolidColorBrush x:Key="TextDisabledBrush" Color="{StaticResource TextDisabledColor}" />
    
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}" />
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}" />
    <SolidColorBrush x:Key="ErrorBrush" Color="{StaticResource ErrorColor}" />
    <SolidColorBrush x:Key="WarningBrush" Color="{StaticResource WarningColor}" />
    <SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource SuccessColor}" />
</ResourceDictionary>
```

### 3. Custom Styles

Define custom control styles:

```xml
<!-- Styles/Themes.axaml -->
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:material="using:Material.Styles">
    <!-- Button Styles -->
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="CornerRadius" Value="4"/>
        <Setter Property="material:ElevationEffect.Elevation" Value="Dp2"/>
    </Style>
    
    <Style Selector="Button.secondary">
        <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="CornerRadius" Value="4"/>
    </Style>
    
    <Style Selector="Button.icon">
        <Setter Property="Width" Value="40"/>
        <Setter Property="Height" Value="40"/>
        <Setter Property="Padding" Value="8"/>
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="CornerRadius" Value="20"/>
    </Style>
    
    <!-- TextBox Styles -->
    <Style Selector="TextBox.form">
        <Setter Property="Margin" Value="0,8"/>
        <Setter Property="Padding" Value="8,6"/>
        <Setter Property="CornerRadius" Value="4"/>
        <Setter Property="BorderThickness" Value="1"/>
    </Style>
    
    <!-- ListBox Styles -->
    <Style Selector="ListBox.data">
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="0"/>
        <Setter Property="Background" Value="Transparent"/>
    </Style>
</Styles>
```

### 4. Theme Switching

Create a theme service for switching between light and dark themes:

```csharp
public interface IThemeService
{
    bool IsDarkTheme { get; }
    void SetLightTheme();
    void SetDarkTheme();
    void ToggleTheme();
}

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

## Navigation System

### 1. Navigation Service

Implement a navigation service for Avalonia:

```csharp
public interface INavigationService
{
    void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase;
    void GoBack();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _viewModelToViewMap;
    private readonly Stack<(Type ViewModel, object Parameter)> _navigationStack = new();
    
    private ContentControl _contentControl;
    
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // Define mappings between ViewModels and Views
        _viewModelToViewMap = new Dictionary<Type, Type>
        {
            { typeof(SequencesViewModel), typeof(SequencesView) },
            { typeof(SequenceDetailViewModel), typeof(SequenceDetailView) },
            // Other mappings
        };
    }
    
    public void Initialize(ContentControl contentControl)
    {
        _contentControl = contentControl;
    }
    
    public void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
    {
        // Add current view to navigation stack
        if (_contentControl.Content != null && _contentControl.DataContext is ViewModelBase currentViewModel)
        {
            var currentViewModelType = currentViewModel.GetType();
            _navigationStack.Push((currentViewModelType, null)); // Save current state
        }
        
        // Resolve the ViewModel from DI
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        
        // Create the View
        var viewType = _viewModelToViewMap[typeof(TViewModel)];
        var view = (UserControl)Activator.CreateInstance(viewType);
        
        // Set DataContext
        view.DataContext = viewModel;
        
        // Initialize ViewModel with parameter
        if (parameter != null && viewModel is IInitializable initializable)
        {
            initializable.Initialize(parameter);
        }
        
        // Update the content control
        _contentControl.Content = view;
    }
    
    public void GoBack()
    {
        if (_navigationStack.Count > 0)
        {
            var (viewModelType, parameter) = _navigationStack.Pop();
            
            // Use reflection to call NavigateTo with the proper generic type
            var navigateToMethod = typeof(NavigationService).GetMethod(nameof(NavigateTo));
            var genericMethod = navigateToMethod.MakeGenericMethod(viewModelType);
            
            genericMethod.Invoke(this, new[] { parameter });
        }
    }
}
```

### 2. View Initialization Interface

Define an interface for view initialization:

```csharp
public interface IInitializable
{
    void Initialize(object parameter);
}

public interface IActivatable
{
    void Activate();
    void Deactivate();
}
```

## Dependency Injection

### 1. Dependency Injection Setup

Register services and ViewModels:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAvaloniaServices(this IServiceCollection services)
    {
        // Register UI services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        
        return services;
    }
    
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SequencesViewModel>();
        services.AddTransient<SequenceDetailViewModel>();
        // Other ViewModels
        
        return services;
    }
}
```

### 2. AvaloniaLocator Integration

Set up the AvaloniaLocator for accessing the service provider:

```csharp
public static class AvaloniaAppBuilderExtensions
{
    public static AppBuilder With(this AppBuilder builder, AvaloniaAppBuilderSettings settings)
    {
        // Register service provider in AvaloniaLocator
        AvaloniaLocator.CurrentMutable.Bind<IServiceProvider>().ToConstant(settings.ServiceProvider);
        
        return builder;
    }
}

public class AvaloniaAppBuilderSettings
{
    public IServiceProvider ServiceProvider { get; set; }
}
```

## Responsive Design

### 1. Grid Layout

Use the Avalonia Grid for responsive layouts:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <!-- Responsive columns that adjust to window size -->
        <ColumnDefinition Width="*" MinWidth="200" />
        <ColumnDefinition Width="*" MinWidth="200" />
    </Grid.ColumnDefinitions>
    
    <!-- First column content -->
    <material:Card Grid.Column="0" Margin="8,0,4,0">
        <!-- Content -->
    </material:Card>
    
    <!-- Second column content -->
    <material:Card Grid.Column="1" Margin="4,0,8,0">
        <!-- Content -->
    </material:Card>
</Grid>
```

### 2. Adaptive Layout

Implement adaptive layouts that change based on window size:

```xml
<Grid>
    <!-- Define a DataTrigger to change the layout based on window width -->
    <Grid.Styles>
        <Style Selector="Grid.responsive">
            <Style.Animations>
                <Animation Duration="0:0:0.2">
                    <KeyFrame Cue="0%">
                        <Setter Property="Grid.ColumnDefinitions">
                            <ColumnDefinitions>
                                <ColumnDefinition Width="*" />
                            </ColumnDefinitions>
                        </Setter>
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Grid.ColumnDefinitions">
                            <ColumnDefinitions>
                                <ColumnDefinition Width="250" />
                                <ColumnDefinition Width="*" />
                            </ColumnDefinitions>
                        </Setter>
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </Grid.Styles>
</Grid>
```

## Testing and Validation

### 1. Unit Testing ViewModels

Test ViewModels using xUnit and ReactiveUI.Testing:

```csharp
public class SequencesViewModelTests
{
    [Fact]
    public void LoadSequencesCommand_ShouldPopulateSequences()
    {
        // Arrange
        var mockSequenceService = new Mock<ISequenceService>();
        mockSequenceService.Setup(s => s.GetAllSequencesAsync())
            .ReturnsAsync(new List<Sequence>
            {
                new Sequence { Id = "1", Name = "Sequence 1" },
                new Sequence { Id = "2", Name = "Sequence 2" }
            });
        
        var viewModel = new SequencesViewModel(
            mockSequenceService.Object,
            Mock.Of<INavigationService>(),
            Mock.Of<IDialogService>(),
            Mock.Of<ILogger<SequencesViewModel>>());
        
        // Act
        viewModel.LoadSequencesCommand.Execute().Subscribe();
        
        // Assert
        Assert.Equal(2, viewModel.Sequences.Count);
        Assert.Equal("Sequence 1", viewModel.Sequences[0].Name);
        Assert.Equal("Sequence 2", viewModel.Sequences[1].Name);
        Assert.False(viewModel.IsLoading);
    }
}
```

### 2. UI Testing

Set up UI testing with Avalonia.Headless:

```csharp
public class SequencesViewTests
{
    [Fact]
    public async Task SequencesView_ShouldShowSequences()
    {
        // Use Avalonia.Headless to test the UI
        using var app = AvaloniaApp.GetApp();
        var host = HostBuilder
            .Create(app)
            .ShouldIgnoreMissingServices()
            .WithAutoFakeServices()
            .Build();
        
        // Setup mocks
        var sequenceService = new Mock<ISequenceService>();
        sequenceService.Setup(s => s.GetAllSequencesAsync())
            .ReturnsAsync(new List<Sequence>
            {
                new Sequence { Id = "1", Name = "Test Sequence" }
            });
        
        host.Services.AddSingleton(sequenceService.Object);
        
        // Get the view and viewmodel
        var viewModel = host.Services.GetRequiredService<SequencesViewModel>();
        var view = new SequencesView { DataContext = viewModel };
        
        await host.StartAsync();
        
        // Render the view
        var window = new TestWindow { Content = view };
        await window.ShowAsync();
        
        // Assert UI state
        var listBox = window.GetVisualDescendants().OfType<ListBox>().First();
        await viewModel.LoadSequencesCommand.Execute().ToTask();
        
        Assert.Equal(1, listBox.ItemCount);
        
        await host.StopAsync();
    }
}
```

## Migration Strategy

### 1. Phased Migration Approach

Follow this phased approach to migrate from WPF to Avalonia:

1. **Phase 1: Project Setup and Core Infrastructure**
   - Create the Avalonia project structure
   - Set up dependency injection and core services
   - Implement base classes (ViewModelBase, etc.)
   - Configure theming and styling

2. **Phase 2: Core Components**
   - Implement the main window and navigation system
   - Recreate core controls and converters
   - Implement critical services (navigation, dialogs)

3. **Phase 3: Feature Migration**
   - Convert ViewModels one by one, starting with simpler ones
   - Convert Views to match the ViewModels
   - Test each feature thoroughly after migration

4. **Phase 4: Integration and Testing**
   - Connect all components together
   - Perform integration testing
   - Address cross-cutting concerns (accessibility, performance)

5. **Phase 5: Deployment and Validation**
   - Create platform-specific packaging
   - Validate deployment on all target platforms
   - Monitor performance and user feedback

### 2. Component Conversion Priority

Prioritize conversion in this order:

1. Core infrastructure and services
2. Main navigation and shell
3. Simple, frequently used views
4. Complex, specialized views
5. Rarely used features

### 3. Testing During Migration

- Unit test each ViewModel after conversion
- Create UI tests for core user journeys
- Implement visual regression testing
- Test on all target platforms regularly

### 4. Running Both Projects During Transition

During transition, maintain both WPF and Avalonia projects:

```csharp
// In the startup code, decide which UI to launch
public static void Main(string[] args)
{
    bool useAvalonia = args.Contains("--avalonia") || 
                       Environment.GetEnvironmentVariable("USE_AVALONIA") == "1";
    
    if (useAvalonia)
    {
        // Start Avalonia version
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    else
    {
        // Start WPF version
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
```

## Conclusion

Migrating from WPF with Material Design to Avalonia provides significant benefits for cross-platform compatibility while maintaining a similar development experience. The key advantages include:

1. Run on multiple platforms with a single codebase
2. Maintain the MVVM architecture pattern
3. Similar XAML-based UI definition
4. Modern, reactive programming support
5. Flexible styling and theming options

By following this guide, you can successfully migrate the Instrument.Data.UI application to Avalonia while preserving the core architecture and functionality. The phased approach minimizes risk and ensures a smooth transition to the cross-platform framework.

For further resources on Avalonia, consider:
- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [Material.Avalonia Documentation](https://github.com/AvaloniaCommunity/Material.Avalonia)
- [ReactiveUI with Avalonia](https://www.reactiveui.net/docs/getting-started/installation/avalonia)