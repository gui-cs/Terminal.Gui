# Configuration Management

Terminal.Gui provides persistent configuration settings via the [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) class.

## Lexicon and Taxonomy

The following terms are used for Configuration Management:

- `Load` - Load configuration from the given location(s), updating the configuration with any new values. Loading does not apply the settings to the application; that happens when the `Apply` method is called.
- `Reset` - Reset the configuration to either the current values or the hard-coded defaults. Resetting does not load the configuration; it only resets the configuration to the default values.
- `Apply` - Apply the configuration to the application; this means the settings are copied from the configuration properties to the corresponding `static` `[ConfigurationProperty]` properties.
- `Sources` - A source is a location where a configuration can be stored. Sources are defined in the `ConfigLocations` enum.
- `Setting` - A setting is a property that is part of a configuration.
- `Theme` - A theme is a named collection of settings that impact the visual style of Terminal.Gui applications.
- `AppSettings` - Application-specific settings are stored in the application's resources.
- `Configuration` - A collection of settings that impact the behavior of an application.
- `ConfigurationProperty` - A property that is part of a configuration.
- `ConfigLocation` - A location where a configuration can be stored.

## Fundamentals

The `ConfigurationManager` class provides a way to store and retrieve configuration settings for an application. The configuration is stored in a JSON file, which can be located in the user's home directory, the current working directory, or as a resource within the application's main assembly.

Settings are defined in JSON format, according to this schema: https://gui-cs.github.io/Terminal.GuiV2Docs/schemas/tui-config-schema.json.

Terminal.Gui library developers can define settings in code and set the default values in the Terminal.Gui assembly's resources (e.g. `Terminal.Gui.Resources.config.json`).

Terminal.Gui application developers can define settings in their apps' code and set the default values in their apps' resources (e.g. `Resources/config.json`) or by setting @Terminal.Gui.Application.RuntimeConfig to string containing JSON.

Users can change settings on a global or per-application basis by providing JSON formatted configuration files. The configuration files can be placed in at .tui folder in the user's home directory (e.g. `C:/Users/username/.tui`, or `/usr/username/.tui`) or the folder where the Terminal.Gui application was launched from (e.g. `./.tui`).

## CM is Disabled by Default

The `ConfigurationManager` class is disabled by default. To enable it, call @Terminal.Gui.ConfigurationManager.Enable() in your application's `Main` method.

```csharp
ConfigurationManager.Enable();
```

If `ConfigurationManager.Enable()` is not called (`ConfigurationManager.IsEnabled` is 'false'), all configuration settings are ignored and ConfigurationManager will effectively be a no-op. All `[ConfigurationProperty]` properties will initially be their hard-coded default values. Calling @Terminal.Gui.ConfigurationManager.Reset will reset all configuration properties back to their hard-coded default values.

Other than that, no other ConfigurationManager APIs will have any effect.

## Loading and Applying Configuration

The `ConfigurationManager` class provides a `Load` method that loads the configuration from the given location. The `Load` method does not apply the settings to the application; that happens when the `Apply` method is called.

When a configuration has been loaded, the @Terminal.Gui.ConfigurationManager.Apply method must be called to apply the settings to the application. This method uses reflection to find all static fields decorated with the `[ConfigurationProperty]` attribute and applies the settings to the corresponding properties.

```csharp
// Load the configuration from just the users home directory.
ConfigurationManager.Locations = ConfigLocations.GlobalHome;
ConfigurationManager.Load();
ConfigurationManager.Apply();
```

> [!IMPORTANT]
>  Configuration Settings Apply at the Process Level. 
> Configuration settings are applied at the process level, which means that they are applied to all applications that are part of the same process. This is due to the fact that configuration properties are defined as static fields, which are static for the process.

## How Settings are Defined 

Application Developers define settings by decorating static properties with the `[ConfigurationProperty]` attribute.

```csharp
class MyApp
{
    [ConfigurationProperty]
    public static string MySetting { get; set; } = "Default Value";
}
```

Configuration Properties must be `public` or `internal` static properties.

The above example will define a configuration property in the `AppSettings` scope. The name of the property will be `MyApp.MySetting` and will appear in JSON as:

```json
{
    "AppSettings": {
      "MyApp.MySetting": "Default Value"
    }
}
```

`AppSettings` property names must be globally unique. To ensure this, the name of the AppSettings property is the name of the property prefixed with a period and the full name of the class that holds it. In the example above, the AppSettings property is named `MyApp.MySetting`.

Terminal.Gui library developers can use the `SettingsScope` and `ThemeScope` attributes to define settings and themes for the terminal.Gui library.

> [!IMPORTANT] App developers cannot define `SettingScope` or `ThemeScope` properties.

```csharp
    /// <summary>
    ///     Gets or sets whether <see cref="Button"/>s are shown with a shadow effect by default.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.None;
```

## Precedence

Settings are applied using the following precedence (higher precedence settings overwrite lower precedence settings):

1. Hard-coded default values in any static property decorated with the `[ConfigurationProperty]` attribute.

2. @Terminal.Gui.ConfigLocations.Default - Default settings in the Terminal.Gui assembly -- Lowest precedence.

3. @Terminal.Gui.ConfigLocations.Runtime - Settings stored in the @Terminal.Gui.ConfigurationManager.RuntimeConfig static property.

4. @Terminal.Gui.ConfigLocations.AppResources - App settings in app resources (`Resources/config.json`).

5. @Terminal.Gui.ConfigLocations.AppHome - App-specific settings in the users's home directory (`~/.tui/appname.config.json`). 

6. @Terminal.Gui.ConfigLocations.AppCurrent - App-specific settings in the directory the app was launched from (`./.tui/appname.config.json`).

7. @Terminal.Gui.ConfigLocations.GlobalHome - Global settings in the the user's home directory (`~/.tui/config.json`).

8. @Terminal.Gui.ConfigLocations.GlobalCurrent - Global settings in the directory the app was launched from (`./.tui/config.json`) --- Hightest precedence.


The [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) will look for configuration files in the `.tui` folder in the user's home directory (e.g. `C:/Users/username/.tui` or `/usr/username/.tui`), the folder where the Terminal.Gui application was launched from (e.g. `./.tui`), or as a resource within the Terminal.Gui application's main assembly.

Settings that will apply to all applications (global settings) reside in files named `config.json`. Settings that will apply to a specific Terminal.Gui application reside in files named `appname.config.json`, where *appname* is the assembly name of the application (e.g. `UICatalog.config.json`).

# Sample Code

The `UICatalog` application provides an example of how to use the [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) class to load and save configuration files. The `Configuration Editor` scenario provides an editor that allows users to edit the configuration files. UI Catalog also uses a file system watcher to detect changes to the configuration files to tell [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) to reload them; allowing users to change settings without having to restart the application.

# What Can Be Configured

The `ConfigurationManager` class provides the following features:

1) **Settings**. Settings are applied to the [`Application`](~/api/Terminal.Gui.Application.yml) class. Settings are accessed via the `Settings` property of [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml). E.g. `Settings["Application.QuitKey"]`
2) **Themes**. Themes are a named collection of settings impacting how applications look. The default theme is named "Default". Two other built-in themes are provided: "Dark", and "Light". Additional themes can be defined in the configuration files. `Settings ["Themes"]` is a dictionary of theme names to theme settings.
3) **AppSettings**. Applications can use the [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) to store and retrieve application-specific settings.

## Discovering What Can Be Configured

Methods for discovering what can be configured are available in the `ConfigurationManager` class:

- Call @ConfigurationManager.GetConfigurationProperties()
- Search the source code for `[ConfigurationProperty]` 
- View `./Terminal.Gui/Resources/config.json`

## Themes

A Theme is a named collection of settings that impact the visual style of Terminal.Gui applications. The default theme is named "Default". The built-in configuration within the Terminal.Gui library defines two more themes: "Dark", and "Light". Additional themes can be defined in the configuration files. The JSON property `Theme` defines the name of the theme that will be used. If the theme is not found, the default theme will be used.

Themes support defining ColorSchemes as well as various default settings for Views. Both the default color schemes and user-defined color schemes can be configured. See [ColorSchemes](~/api/Terminal.Gui.Colors.yml) for more information.

Themes support changing the standard set of glyphs used by views (e.g. the default indicator for [Button](~/api/Terminal.Gui.Button.yml)) and line drawing (e.g. [LineCanvas](~/api/Terminal.Gui.LineCanvas.yml)).

The value can be either a decimal number or a string. The string may be:

- A Unicode char (e.g. "☑")
- A hex value in U+ format (e.g. "U+2611")
- A hex value in UTF-16 format (e.g. "\\u2611")

```json
  "Glyphs.RightArrow": "►",
  "Glyphs.LeftArrow": "U+25C4",
  "Glyphs.DownArrow": "\\u25BC",
  "Glyphs.UpArrow": 965010
```

The `UI Catalog` application defines a `UICatlog` Theme. Look at the UI Catalog's `./Resources/config.json` file to see how to define a theme.

# Key Bindings

> [!WARNING]
>  Configuration Manager support for key bindings is not yet implemented.

Key bindings are defined in the `KeyBindings` property of the configuration file. The value is an array of objects, each object defining a key binding. The key binding object has the following properties:

- `Key`: The key to bind to. The format is a string describing the key (e.g. "q", "Q,  "Ctrl+Q"). Function keys are specified as "F1", "F2", etc. 

# Configuration File Schema

Settings are defined in JSON format, according to the schema found here:

https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json

## Schema

[!code-json[tui-config-schema.json](../schemas/tui-config-schema.json)]

# The Default Config File

To illustrate the syntax, the below is the `config.json` file found in `Terminal.Gui.dll`:

[!code-json[config.json](../../Terminal.Gui/Resources/config.json)]