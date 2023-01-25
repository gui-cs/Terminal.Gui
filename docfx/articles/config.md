# Configuration Management

Terminal.Gui provides configuration and theme management for Terminal.Gui applications.

The [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.Configuration.ConfigurationManager.yml) class loads and saves Json-formatted configuration files. 

Users can set Terminal.Gui settings on a global or per-application basis by providing JSON formatted configuration files. There are two types, or scopes, of settings: Setting scope and Theme scope. Setting scope settings are generally applied to the [`Application`](~/api/Terminal.Gui/Terminal.Gui.Application.yml) class. Theme scope settings are applied to various classes such as [`FrameView`](~/api/Terminal.Gui/Terminal.Gui.FrameView.yml) and [`Window`](~/api/Terminal.Gui/Terminal.Gui.Window.yml).

See below for more information on [`ThemeManager`](~/api/Terminal.Gui/Terminal.Gui.Configuration.ThemeManager.yml).

The The [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.Configuration.ConfigurationManager.yml) will look for configuration files in the `.tui` folder in the user's home directory (e.g. `C:/Users/username/.tui` or `/usr/username/.tui`), the folder where the Terminal.Gui application was launched from (e.g. `./.tui`), or as a resource within the Terminal.Gui application's main assembly.

Settings that will apply to all applications (global settings) reside in files named config.json. Settings that will apply to a specific Terminal.Gui application reside in files named appname.config.json, where appname is the assembly name of the application (e.g. `UICatalog.config.json`).

Settings are applied using the following precedence (higher precedence settings overwrite lower precedence settings):

1. App specific settings found in the users's home directory (`~/.tui/appname.config.json`). -- Highest precedence.

2. App specific settings found in the directory the app was launched from (`./.tui/appname.config.json`).

3. App settings in app resources (`Resources/config.json`).

4. Global settings found in the the user's home directory (`~/.tui/config.json`).

5. Global settings found in the directory the app was launched from (`./.tui/config.json`).

6. Default settings defined in the Terminal.Gui assembly -- Lowest precedence.

The `UI Catalog` application provides an example of how to use the [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.Configuration.ConfigurationManager.yml) class to load and save configuration files. The `Configuration Editor` scenario provides an editor that allows users to edit the configuration files. UI Catalog also uses a file system watcher to detect changes to the configuration files to tell [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.Configuration.ConfigurationManager.yml) to reaload them; allowing users to change settings without having to restart the application.

# What Can Be Configured

## Settings

Settings for the [`Application`](~/api/Terminal.Gui/Terminal.Gui.Application.yml) class.
    * [QuitKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#QuitKey)
    * [AlternateForwardKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#AlternateForwardKey)
    * [AlternateBackwardKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#AlternateBackwardKey)
    * [UseSystemConsole](~/api/Terminal.Gui/Terminal.Gui.Application.yml#UseSystemConsole)
    * [IsMouseDisabled](~/api/Terminal.Gui/Terminal.Gui.Application.yml#IsMouseDisabled)
    * [HeightAsBuffer](~/api/Terminal.Gui/Terminal.Gui.Application.yml#HeightAsBuffer)

## Themes

A Theme is a collection of settings that are named. The default theme is named "Default". The built-in configuration stored within the Terminal.Gui library defines two additional themes: "Dark", and "Light". Additional themes can be defined in the configuration files. 

The Json property `Theme` defines the name of the theme that will be used. If the theme is not found, the default theme will be used.

Themes support defining ColorSchemes as well as various default settings for Views. Both the default color schemes and user defined color schemes can be configured. See [ColorSchemes](~/api/Terminal.Gui/Terminal.Gui.Colors.yml) for more information.

# Example Configuration File

```json
{
  "$schema": "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
  "Application.QuitKey": {
    "Key": "Esc"
  },
  "Themes": [
    {
      "UI Catalog Theme": {
        "ColorSchemes": [
          {
            "UI Catalog Scheme": {
              "Normal": {
                "Foreground": "White",
                "Background": "Green"
              },
              "Focus": {
                "Foreground": "Green",
                "Background": "White"
              },
              "HotNormal": {
                "Foreground": "Blue",
                "Background": "Green"
              },
              "HotFocus": {
                "Foreground": "BrightRed",
                "Background": "White"
              },
              "Disabled": {
                "Foreground": "BrightGreen",
                "Background": "Gray"
              }
            }
          },
          {
            "TopLevel": {
              "Normal": {
                "Foreground": "DarkGray",
                "Background": "White"
              ...
              }
            }
          }
        ],
        "FrameView.DefaultBorderStyle": "Double"
      }
    }
  ]
}
```

# Configuration File Schema

Settings are defined in JSON format, according to the schema found here: 

https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json
