---
uid: Terminal.Gui.Configuration
summary: Configuration management, themes, and persistent settings.
---

The `Configuration` namespace provides comprehensive configuration management for Terminal.Gui applications.

## Key Types

- **ConfigurationManager** - Central system for loading and applying configuration
- **ConfigurationPropertyAttribute** - Marks properties as configurable
- **Scope** - Configuration contexts (Settings, Themes, AppSettings)
- **ThemeManager** - Theme loading and application

## Configuration Scopes

| Scope | Purpose |
|-------|---------|
| Settings | Runtime behavior settings |
| Themes | Visual styling and color schemes |
| AppSettings | Application-specific settings |

## Configuration Sources

Configuration is loaded from multiple locations in priority order:

1. Application directory (`appSettings.json`)
2. User directory (`~/.tui/`)
3. Environment variables
4. Code-based defaults

## Example

```csharp
// Enable configuration from all sources
ConfigurationManager.Enable (ConfigLocations.All);

// Access theme
ThemeManager.Theme = "Dark";

// Mark a property as configurable
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static bool MyFeature { get; set; } = true;
```

## See Also

- [Configuration Deep Dive](~/docs/config.md)
