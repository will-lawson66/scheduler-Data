# Instrument Data Manager

This application provides a user interface for managing instrument data, including sequences, parameters, ranges, resources, and sequence groups.

## Running the Application

There are several ways to run the application:

### Option 1: Run using batch file

Simply double-click the `run-ui.bat` file in the root directory. This will build and run the application.

### Option 2: Run from Visual Studio

1. Open the solution file `Instrument.Data.sln` in Visual Studio
2. Set the `Instrument.Data.UI` project as the startup project
3. Press F5 or click the "Start" button

### Option 3: Run using .NET CLI

```bash
cd scheduler-Data
dotnet run --project Instrument.Data.UI/Instrument.Data.UI.csproj
```

## Application Features

- **Entity Management**: Create, edit, delete, and associate all entities
- **Relationship Visualization**: Interactive visualization of entity relationships
- **Import/Export**: Tools for importing and exporting data

## Project Structure

- **Instrument.Data**: Core data models and services
- **Instrument.Data.UI**: User interface for data management
- **Instrument.Data.UT**: Unit tests

## Architecture

The application follows the MVVM (Model-View-ViewModel) architecture pattern:

- **Models**: Business entities and data services in the Instrument.Data project
- **ViewModels**: UI logic and state management
- **Views**: WPF XAML UI components

## Dependencies

- .NET 8.0
- Entity Framework Core
- Material Design for WPF
- CommunityToolkit.Mvvm
