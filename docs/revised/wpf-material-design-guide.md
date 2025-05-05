# Instrument.Data.UI WPF Material Design Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Material Design Overview](#material-design-overview)
3. [Project Setup](#project-setup)
4. [Material Design Integration](#material-design-integration)
5. [UI Components and Patterns](#ui-components-and-patterns)
6. [Styling and Themes](#styling-and-themes)
7. [Custom Controls](#custom-controls)
8. [Responsive Design](#responsive-design)
9. [Accessibility Considerations](#accessibility-considerations)
10. [Common Pitfalls and Troubleshooting](#common-pitfalls-and-troubleshooting)

## Introduction

This guide provides comprehensive information about implementing Material Design in the Instrument.Data.UI WPF application. It covers the setup, integration, and customization of Material Design components, as well as best practices and common patterns used throughout the application.

Material Design provides a modern, consistent, and visually appealing user interface that enhances user experience and productivity. This guide will help developers understand how Material Design is integrated into the application and how to maintain consistency when adding new features.

## Material Design Overview

Material Design is a design system created by Google that helps teams build high-quality digital experiences. It provides guidelines for visual, motion, and interaction design across platforms and devices. Key principles include:

1. **Material as a metaphor**: Surfaces and edges provide visual cues based on reality
2. **Bold, graphic, intentional**: Typography, grids, space, scale, color, and imagery guide visuals
3. **Motion provides meaning**: Animation reinforces user actions and provides feedback

In our WPF implementation, we use the MaterialDesignThemes.Wpf library, which provides Material Design-styled controls and themes for WPF applications.

## Project Setup

### Required NuGet Packages

The following NuGet packages are required for Material Design integration:

```xml
<ItemGroup>
  <PackageReference Include="MaterialDesignThemes" Version="4.9.0" />
  <PackageReference Include="MaterialDesignColors" Version="2.1.4" />
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
  <PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.77" />
</ItemGroup>
```

### Project Structure for Material Design

```
Instrument.Data.UI/
├── App.xaml                 # Global application resources
├── Resources/               # Themed resources
│   ├── Colors.xaml          # Color definitions
│   └── Styles.xaml          # Custom styles
└── Controls/                # Custom-styled controls
```

## Material Design Integration

### App.xaml Configuration

The Material Design theme is configured in App.xaml:

```xml
<Application x:Class="Instrument.Data.UI.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Material Design Theme -->
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml" />
                
                <!-- Primary and Accent Colors -->
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml" />
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Indigo.xaml" />
                
                <!-- Component Themes -->
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml" />
                
                <!-- Application-specific resources -->
                <ResourceDictionary Source="pack://application:,,,/Instrument.Data.UI;component/Resources/Colors.xaml" />
                <ResourceDictionary Source="pack://application:,,,/Instrument.Data.UI;component/Resources/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
            
            <!-- Global settings -->
            <Style TargetType="{x:Type Control}" BasedOn="{StaticResource {x:Type Control}}">
                <Setter Property="FontFamily" Value="{StaticResource MaterialDesignFont}" />
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### Material Design Theme Configuration

The application uses a Light theme with Blue as the primary color and Indigo as the accent color. The theme can be configured in App.xaml or programmatically:

```csharp
// Programmatic theme configuration
public static void ConfigureTheme(ITheme theme)
{
    // Primary colors
    theme.PrimaryLight = Colors.LightBlue;
    theme.PrimaryMid = Colors.Blue;
    theme.PrimaryDark = Colors.DarkBlue;
    
    // Accent colors
    theme.SecondaryLight = Colors.LightIndigo;
    theme.SecondaryMid = Colors.Indigo;
    theme.SecondaryDark = Colors.DarkIndigo;
    
    // Apply theme
    var paletteHelper = new PaletteHelper();
    paletteHelper.SetTheme(theme);
}
```

### Window Configuration

Material Design is applied to the main window:

```xml
<Window x:Class="Instrument.Data.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        TextElement.Foreground="{DynamicResource MaterialDesignBody}"
        TextElement.FontWeight="Regular"
        TextElement.FontSize="13"
        TextOptions.TextFormattingMode="Ideal"
        TextOptions.TextRenderingMode="Auto"
        Background="{DynamicResource MaterialDesignPaper}"
        FontFamily="{materialDesign:MaterialDesignFont}"
        Title="Instrument Data Manager" 
        Height="750" 
        Width="1200"
        WindowStartupLocation="CenterScreen">
    <!-- Window content -->
</Window>
```

## UI Components and Patterns

### Card Pattern

Material Design Cards are used extensively to group related content:

```xml
<materialDesign:Card Padding="16" Margin="0,0,0,16">
    <StackPanel>
        <TextBlock Text="Section Title" 
                  Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                  Margin="0,0,0,8"/>
        
        <!-- Card content -->
        <Grid>
            <!-- Grid content -->
        </Grid>
    </StackPanel>
</materialDesign:Card>
```

### App Bar Pattern

The application uses the ColorZone component for app bars:

```xml
<materialDesign:ColorZone Mode="PrimaryDark" 
                         Padding="16" 
                         materialDesign:ShadowAssist.ShadowDepth="Depth2">
    <DockPanel>
        <StackPanel Orientation="Horizontal" DockPanel.Dock="Left">
            <materialDesign:PackIcon Kind="Database" 
                                   VerticalAlignment="Center" 
                                   Width="24" 
                                   Height="24" 
                                   Margin="0,0,8,0"/>
            <TextBlock Text="Instrument Data Manager" 
                      VerticalAlignment="Center" 
                      FontSize="20"/>
        </StackPanel>
    </DockPanel>
</materialDesign:ColorZone>
```

### Form Layout Pattern

Forms follow a consistent layout pattern:

```xml
<StackPanel Margin="16">
    <TextBlock Text="Sequence Details" 
              Style="{StaticResource MaterialDesignHeadline6TextBlock}"
              Margin="0,0,0,16"/>
    
    <TextBox 
        Text="{Binding Name}" 
        materialDesign:HintAssist.Hint="Sequence Name"
        Style="{StaticResource MaterialDesignOutlinedTextBox}"
        Margin="0,0,0,8"/>
    
    <TextBox 
        Text="{Binding Description}"
        materialDesign:HintAssist.Hint="Description"
        Style="{StaticResource MaterialDesignOutlinedTextBox}"
        TextWrapping="Wrap"
        AcceptsReturn="True"
        Height="80"
        Margin="0,0,0,8"/>
    
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,16,0,0">
        <Button 
            Content="Cancel" 
            Command="{Binding CancelCommand}"
            Style="{StaticResource MaterialDesignOutlinedButton}"
            Margin="0,0,8,0"/>
        
        <Button 
            Content="Save" 
            Command="{Binding SaveCommand}"
            Style="{StaticResource MaterialDesignRaisedButton}"/>
    </StackPanel>
</StackPanel>
```

### List View Pattern

Lists of items follow a consistent pattern:

```xml
<ListView ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedItem}">
    <ListView.ItemTemplate>
        <DataTemplate>
            <Grid Margin="8">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <StackPanel Grid.Column="0">
                    <TextBlock Text="{Binding Name}"
                              Style="{StaticResource MaterialDesignSubtitle1TextBlock}"/>
                    <TextBlock Text="{Binding Description}"
                              Style="{StaticResource MaterialDesignBody2TextBlock}"
                              TextWrapping="Wrap"/>
                </StackPanel>
                
                <StackPanel Grid.Column="1" 
                          Orientation="Horizontal" 
                          VerticalAlignment="Center">
                    <Button 
                        Command="{Binding DataContext.ViewItemCommand, 
                                 RelativeSource={RelativeSource AncestorType=ListView}}"
                        CommandParameter="{Binding}"
                        Style="{StaticResource MaterialDesignIconButton}">
                        <materialDesign:PackIcon Kind="Eye" Width="22" Height="22"/>
                    </Button>
                    <Button 
                        Command="{Binding DataContext.EditItemCommand, 
                                 RelativeSource={RelativeSource AncestorType=ListView}}"
                        CommandParameter="{Binding}"
                        Style="{StaticResource MaterialDesignIconButton}">
                        <materialDesign:PackIcon Kind="Edit" Width="22" Height="22"/>
                    </Button>
                </StackPanel>
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### Dialog Pattern

Dialogs are implemented using MaterialDesignThemes.Wpf dialogs:

```csharp
public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var view = new ConfirmationDialog
        {
            Title = title,
            Message = message
        };
        
        var result = await DialogHost.Show(view, "RootDialog");
        return (bool)result;
    }
    
    public async Task ShowInformationAsync(string title, string message)
    {
        var view = new InformationDialog
        {
            Title = title,
            Message = message
        };
        
        await DialogHost.Show(view, "RootDialog");
    }
    
    // Other dialog methods
}
```

```xml
<!-- Dialog host in MainWindow.xaml -->
<materialDesign:DialogHost Identifier="RootDialog">
    <materialDesign:DialogHost.DialogContent>
        <!-- Default dialog content -->
        <Grid Margin="16">
            <TextBlock Text="Loading..." />
        </Grid>
    </materialDesign:DialogHost.DialogContent>
    
    <!-- Main content -->
    <Grid>
        <!-- Application content -->
    </Grid>
</materialDesign:DialogHost>
```

### Progress Indicators

Material Design provides various progress indicators:

```xml
<!-- Circular progress -->
<materialDesign:Card>
    <Grid>
        <!-- Content -->
        
        <!-- Loading overlay -->
        <Grid Background="#80FFFFFF"
              Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}">
            <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}"
                        IsIndeterminate="True"
                        Value="0"
                        HorizontalAlignment="Center"
                        VerticalAlignment="Center"/>
        </Grid>
    </Grid>
</materialDesign:Card>

<!-- Linear progress -->
<ProgressBar Value="{Binding Progress}"
            Maximum="100"
            Minimum="0"
            Visibility="{Binding IsProgressVisible, Converter={StaticResource BooleanToVisibilityConverter}}"
            Margin="0,8"/>
```

## Styling and Themes

### Custom Color Scheme

Custom colors are defined in `Colors.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
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

### Custom Styles

Custom styles are defined in `Styles.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <!-- Button styles -->
    <Style x:Key="PrimaryButton" TargetType="Button" 
           BasedOn="{StaticResource MaterialDesignRaisedButton}">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="materialDesign:ButtonAssist.CornerRadius" Value="4"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="Margin" Value="0,8"/>
    </Style>
    
    <Style x:Key="SecondaryButton" TargetType="Button" 
           BasedOn="{StaticResource MaterialDesignOutlinedButton}">
        <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="materialDesign:ButtonAssist.CornerRadius" Value="4"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="Margin" Value="0,8"/>
    </Style>
    
    <!-- TextBox styles -->
    <Style x:Key="FormTextBox" TargetType="TextBox" 
           BasedOn="{StaticResource MaterialDesignOutlinedTextBox}">
        <Setter Property="Margin" Value="0,8"/>
        <Setter Property="materialDesign:TextFieldAssist.TextFieldCornerRadius" Value="4"/>
    </Style>
    
    <!-- CheckBox styles -->
    <Style x:Key="FormCheckBox" TargetType="CheckBox" 
           BasedOn="{StaticResource MaterialDesignCheckBox}">
        <Setter Property="Margin" Value="0,8"/>
    </Style>
    
    <!-- ListView styles -->
    <Style x:Key="DataListViewStyle" TargetType="ListView" 
           BasedOn="{StaticResource MaterialDesignListView}">
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="ScrollViewer.VerticalScrollBarVisibility" Value="Auto"/>
    </Style>
    
    <!-- Card styles -->
    <Style x:Key="ContentCard" TargetType="materialDesign:Card">
        <Setter Property="Padding" Value="16"/>
        <Setter Property="Margin" Value="0,0,0,16"/>
        <Setter Property="materialDesign:ShadowAssist.ShadowDepth" Value="Depth1"/>
    </Style>
</ResourceDictionary>
```

### Theme Switching

Implement theme switching functionality:

```csharp
public class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper;
    
    public ThemeService()
    {
        _paletteHelper = new PaletteHelper();
    }
    
    public bool IsDarkTheme()
    {
        var theme = _paletteHelper.GetTheme();
        return theme.GetBaseTheme() == BaseTheme.Dark;
    }
    
    public void SetLightTheme()
    {
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
    }
    
    public void SetDarkTheme()
    {
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(BaseTheme.Dark);
        _paletteHelper.SetTheme(theme);
    }
    
    public void ToggleTheme()
    {
        var theme = _paletteHelper.GetTheme();
        var baseTheme = theme.GetBaseTheme();
        
        theme.SetBaseTheme(baseTheme == BaseTheme.Light ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
    }
}
```

## Custom Controls

### FormFieldControl

```xml
<UserControl x:Class="Instrument.Data.UI.Controls.FormFieldControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <TextBlock Grid.Row="0" 
                   Text="{Binding Label, RelativeSource={RelativeSource AncestorType=UserControl}}"
                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                   Margin="0,0,0,4"
                   Visibility="{Binding Label, RelativeSource={RelativeSource AncestorType=UserControl}, 
                                Converter={StaticResource NullValueToBooleanConverter}, 
                                ConverterParameter=true}"/>
        
        <ContentPresenter Grid.Row="1" 
                          Content="{Binding Content, 
                                   RelativeSource={RelativeSource AncestorType=UserControl}}"/>
        
        <TextBlock Grid.Row="2" 
                   Text="{Binding ErrorMessage, 
                          RelativeSource={RelativeSource AncestorType=UserControl}}"
                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                   Foreground="{StaticResource ErrorBrush}"
                   Margin="0,4,0,0"
                   Visibility="{Binding ErrorMessage, 
                                RelativeSource={RelativeSource AncestorType=UserControl}, 
                                Converter={StaticResource NullValueToBooleanConverter}, 
                                ConverterParameter=true}"/>
    </Grid>
</UserControl>
```

```csharp
public class FormFieldControl : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormFieldControl));
        
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(FormFieldControl));
        
    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(FormFieldControl));
    
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    
    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }
    
    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }
}
```

### Usage

```xml
<controls:FormFieldControl Label="Name">
    <TextBox Text="{Binding Name}" 
             Style="{StaticResource FormTextBox}"/>
</controls:FormFieldControl>
```

## Responsive Design

Implement responsive design using Grid and ColumnDefinitions:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <!-- Responsive columns -->
        <ColumnDefinition Width="*" MinWidth="200" />
        <ColumnDefinition Width="*" MinWidth="200" />
    </Grid.ColumnDefinitions>
    
    <!-- First column content -->
    <materialDesign:Card Grid.Column="0" Margin="0,0,8,0" Style="{StaticResource ContentCard}">
        <!-- Content -->
    </materialDesign:Card>
    
    <!-- Second column content -->
    <materialDesign:Card Grid.Column="1" Margin="8,0,0,0" Style="{StaticResource ContentCard}">
        <!-- Content -->
    </materialDesign:Card>
</Grid>
```

### Layout Considerations

- Use Grid with proportional sizing (`*`) for flexible layouts
- Set `MinWidth` and `MinHeight` to ensure minimum content visibility
- Use `Margin` to create spacing between elements
- Use `ScrollViewer` for content that might overflow
- Implement UI virtualization for large collections

## Accessibility Considerations

### Keyboard Navigation

Ensure proper keyboard navigation by setting tab order and keyboard shortcuts:

```xml
<Button Content="Save" 
        Command="{Binding SaveCommand}"
        TabIndex="3"
        ToolTip="Save changes (Ctrl+S)">
    <Button.InputBindings>
        <KeyBinding Key="S" Modifiers="Control" Command="{Binding SaveCommand}" />
    </Button.InputBindings>
</Button>
```

### Screen Reader Support

Provide accessibility information for screen readers:

```xml
<Button Content="Add"
        Command="{Binding AddCommand}"
        AutomationProperties.Name="Add new item"
        AutomationProperties.HelpText="Adds a new item to the collection">
    <materialDesign:PackIcon Kind="Plus" />
</Button>
```

### High Contrast Support

Ensure high contrast support by using system colors when appropriate:

```csharp
public class HighContrastHelper
{
    public static bool IsHighContrastEnabled()
    {
        return SystemParameters.HighContrast;
    }
    
    public static void ApplyHighContrastTheme(ResourceDictionary resources)
    {
        if (IsHighContrastEnabled())
        {
            resources["PrimaryBrush"] = SystemColors.HighlightBrush;
            resources["TextPrimaryBrush"] = SystemColors.WindowTextBrush;
            resources["BackgroundBrush"] = SystemColors.WindowBrush;
        }
    }
}
```

## Common Pitfalls and Troubleshooting

### Resource Dictionary Issues

**Problem**: Resource not found exceptions

**Solution**: Ensure proper resource dictionary loading order:

1. First load MaterialDesignTheme.Light.xaml or MaterialDesignTheme.Dark.xaml
2. Then load primary and accent color resources
3. Then load MaterialDesignTheme.Defaults.xaml
4. Finally load application-specific resources

**Incorrect**:
```xml
<materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="Blue" SecondaryColor="Indigo" />
```

**Correct**:
```xml
<ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml" />
<ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml" />
<ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Indigo.xaml" />
```

### Style Inheritance Issues

**Problem**: Custom styles not inheriting Material Design properties

**Solution**: Base custom styles on Material Design styles:

```xml
<!-- Incorrect -->
<Style x:Key="CustomButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
</Style>

<!-- Correct -->
<Style x:Key="CustomButtonStyle" TargetType="Button" 
       BasedOn="{StaticResource MaterialDesignRaisedButton}">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
</Style>
```

### Performance Issues

**Problem**: UI performance issues with large collections

**Solution**: Implement UI virtualization:

```xml
<ListView ItemsSource="{Binding LargeCollection}"
          VirtualizingStackPanel.IsVirtualizing="True"
          VirtualizingStackPanel.VirtualizationMode="Recycling"
          ScrollViewer.IsDeferredScrollingEnabled="True">
    <!-- ListView content -->
</ListView>
```

### Dialog Hosting Issues

**Problem**: Dialogs not showing or incorrectly positioned

**Solution**: Ensure proper DialogHost setup:

1. Add a DialogHost at the root of your main window
2. Set a unique identifier for the DialogHost
3. Reference this identifier when showing dialogs

```xml
<materialDesign:DialogHost Identifier="RootDialog">
    <!-- Main content -->
</materialDesign:DialogHost>
```

```csharp
await DialogHost.Show(dialogContent, "RootDialog");
```

### Theme Switching Issues

**Problem**: Theme changes not applied consistently

**Solution**: Update theme through the PaletteHelper:

```csharp
var paletteHelper = new PaletteHelper();
var theme = paletteHelper.GetTheme();
theme.SetBaseTheme(BaseTheme.Dark);
paletteHelper.SetTheme(theme);
```

## See Also

- [Material Design Guidelines](https://material.io/design)
- [MaterialDesignInXAML Toolkit Documentation](http://materialdesigninxaml.net/)
- [Core Data Layer](./core-data-layer.md)
- [Integration Guide](./integration-guide.md)
- [Presentation Layer Structure](./presentation-layer-structure.md)
