# Configuration Management

Terminal.Gui provides persistent configuration settings via the [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) class.

1) **Settings**. Settings are applied to the [`Application`](~/api/Terminal.Gui.Application.yml) class. Settings are accessed via the `Settings` property of [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml).
2) **Themes**. Themes are a named collection of settings impacting how applications look. The default theme is named "Default". Two other built-in themes are provided: "Dark", and "Light". Additional themes can be defined in the configuration files.
3) **AppSettings**. Applications can use the [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) to store and retrieve application-specific settings.

The [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) will look for configuration files in the `.tui` folder in the user's home directory (e.g. `C:/Users/username/.tui` or `/usr/username/.tui`), the folder where the Terminal.Gui application was launched from (e.g. `./.tui`), or as a resource within the Terminal.Gui application's main assembly.

Settings that will apply to all applications (global settings) reside in files named `config.json`. Settings that will apply to a specific Terminal.Gui application reside in files named `appname.config.json`, where *appname* is the assembly name of the application (e.g. `UICatalog.config.json`).

Settings are applied using the following precedence (higher precedence settings overwrite lower precedence settings):

1. @Terminal.Gui.ConfigLocations.Default - Default settings in the Terminal.Gui assembly -- Lowest precedence.

2. @Terminal.Gui.ConfigLocations.Runtime - Settings stored in the @Terminal.Gui.ConfigurationManager.RuntimeConfig static property.

3. @Terminal.Gui.ConfigLocations.AppResources - App settings in app resources (`Resources/config.json`).

4. @Terminal.Gui.ConfigLocations.AppHome - App-specific settings in the users's home directory (`~/.tui/appname.config.json`). 

5. @Terminal.Gui.ConfigLocations.AppCurrent - App-specific settings in the directory the app was launched from (`./.tui/appname.config.json`).

6. @Terminal.Gui.ConfigLocations.GlobalHome - Global settings in the the user's home directory (`~/.tui/config.json`).

7. @Terminal.Gui.ConfigLocations.GlobalCurrent - Global settings in the directory the app was launched from (`./.tui/config.json`) --- Hightest precedence.

The `UICatalog` application provides an example of how to use the [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) class to load and save configuration files. The `Configuration Editor` scenario provides an editor that allows users to edit the configuration files. UI Catalog also uses a file system watcher to detect changes to the configuration files to tell [`ConfigurationManager`](~/api/Terminal.Gui.ConfigurationManager.yml) to reload them; allowing users to change settings without having to restart the application.

# What Can Be Configured

## Settings

> [!IMPORTANT]
> This list is not complete; search the source code for `SerializableConfigurationProperty` to find all settings that can be configured.

  * @Terminal.Gui.Application.QuitKey
  * @Terminal.Gui.Application.NextTabKey
  * @Terminal.Gui.Application.PrevTabKey
  * @Terminal.Gui.Application.NextTabGroupKey
  * @Terminal.Gui.Application.PrevTabGroupKey
  * @Terminal.Gui.Application.ArrangeKey
  * @Terminal.Gui.Application.ForceDriver
  * @Terminal.Gui.Application.Force16Colors
  * @Terminal.Gui.Application.IsMouseDisabled
  

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