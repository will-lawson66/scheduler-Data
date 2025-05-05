# Instrument.Data Documentation

Welcome to the comprehensive documentation for the Instrument.Data project. This documentation suite provides detailed information about the architecture, implementation, and integration patterns used throughout the system.

## Documentation Guide

This documentation suite consists of the following documents, each focusing on specific aspects of the system:

1. [**Core Data Layer**](./core-data-layer.md) - Detailed documentation of the data access layer, entities, repositories, and services
2. [**Integration Guide**](./integration-guide.md) - Guide to integrating the data layer with other application components
3. [**Presentation Layer Structure**](./presentation-layer-structure.md) - Guidelines for structuring the UI layer following MVVM pattern
4. [**WPF Material Design Guide**](./wpf-material-design-guide.md) - Implementation details for the WPF UI with Material Design

## Project Overview

Instrument.Data is a comprehensive data management solution for laboratory instrument scheduling operations. The system is designed around the following key principles:

- **Clean Architecture** with clear separation of concerns
- **Domain-Driven Design** using rich entity models
- **Repository Pattern** for data access abstraction
- **MVVM Pattern** for presentation layer design
- **Dependency Injection** for loose coupling between components

## System Architecture

The system is structured into the following main components:

```
┌───────────────────┐      ┌──────────────────┐      ┌───────────────────┐
│    Domain Layer   │      │  Application     │      │  Presentation     │
│                   │      │  Services        │      │  Layer            │
│  - Entities       │      │                  │      │                   │
│  - Value Objects  │◄────►│  - Use Cases     │◄────►│  - ViewModels     │
│  - Domain Services│      │  - Commands      │      │  - Views          │
│  - Events         │      │  - Queries       │      │  - UI Services    │
└───────────────────┘      └──────────────────┘      └───────────────────┘
```

### Key Projects

- **Instrument.Data** - Core data layer with entity models, repositories, and services
- **Instrument.Data.UI** - WPF presentation layer with MVVM implementation
- **Instrument.Data.UT** - Unit tests for data layer components

## Getting Started

To get started with the Instrument.Data system, see the [Integration Guide](./integration-guide.md) for details on how to configure and use the data layer in your application.

## Technology Stack

- **.NET 8.0** - Base framework
- **Entity Framework Core** - Data access and ORM
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **WPF** - UI framework
- **MaterialDesignThemes** - UI styling and components
- **CommunityToolkit.Mvvm** - MVVM implementation helpers
