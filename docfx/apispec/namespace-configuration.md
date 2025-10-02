---
uid: Terminal.Gui.Configuration
summary: The `Configuration` namespace provides comprehensive configuration management for Terminal.Gui applications.
---

@Terminal.Gui.Configuration provides a robust system for managing application settings, themes, and runtime configuration. This namespace includes the configuration manager, property attributes, and scoping mechanisms that allow applications to persist and load settings from various sources.

The configuration system supports multiple scopes (Settings, Themes, AppSettings) and sources (user directory, application directory, etc.), enabling flexible deployment and customization scenarios. It also provides theme inheritance and hot-reloading capabilities for dynamic configuration updates.

## Key Components

- **ConfigurationManager**: Central system for loading, applying, and managing configuration
- **ConfigProperty**: Attribute for marking properties as configurable
- **Scopes**: Settings, Theme, and AppSettings contexts
- **Sources**: Multiple storage locations for configuration persistence

## Example Usage

```csharp
// Mark a property as configurable
[ConfigProperty]
public static bool MyFeatureEnabled { get; set; } = true;

// Load configuration from default sources
ConfigurationManager.Enable(ConfigLocations.All);
```

## Deep Dive

- [Configuration Management Deep Dive](~/docs/config.md) - Comprehensive configuration system documentation
