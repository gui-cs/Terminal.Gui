# Configuration Management

Terminal.Gui provides settings and configuration management for Terminal.Gui applications.

## Configuration Files

The [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.Configuration.ConfigurationManager.yml)  class provides a simple way to load and save configuration files. The configuration files are simple JSON files. The [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.Configuration.ConfigurationManager.yml) class provides a simple way to load and save configuration files. The configuration files are simple JSON files.

Users can set Terminal.Gui settings on a global or per-application basis by providing JSON formatted configuration files. 

The The [`ConfigurationManager`](~/api/Terminal.Gui/Terminal.Gui.Configuration.ConfigurationManager.yml) will look for configuration files in the `.tui` folder in the user's home directory (e.g. `C:/Users/username/.tui` or `/usr/username/.tui`), the folder where the Terminal.Gui application was launched from (e.g. `./.tui`), or as a resource within the Terminal.Gui application's main assembly.

Settings that will apply to all applications (global settings) reside in files named config.json. Settings that will apply to a specific Terminal.Gui application reside in files named appname.config.json, where appname is the assembly name of the application (e.g. `UICatalog.config.json`).

Settings are applied using the following precedence (higher precedence settings overwrite lower precedence settings):

1. App specific settings found in the users's home directory (`~/.tui/appname.config.json`). -- Highest precedence.

2. App specific settings found in the directory the app was launched from (`./.tui/appname.config.json`).

3. App settings in app resources (`Resources/config.json`).

4. Global settings found in the the user's home directory (`~/.tui/config.json`).

5. Global settings found in the directory the app was launched from (`./.tui/config.json`).

6. Default settings defined in the Terminal.Gui assembly -- Lowest precedence.

## What Can Be Configured

Currently these settings can be configured:

* Settings for the [`Application`](~/api/Terminal.Gui/Terminal.Gui.Application.yml) class.
    * [QuitKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#QuitKey)
    * [AlternateForwardKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#AlternateForwardKey)
    * [AlternateBackwardKey](~/api/Terminal.Gui/Terminal.Gui.Application.yml#AlternateBackwardKey)
    * [UseSystemConsole](~/api/Terminal.Gui/Terminal.Gui.Application.yml#UseSystemConsole)
    * [IsMouseDisabled](~/api/Terminal.Gui/Terminal.Gui.Application.yml#IsMouseDisabled)
    * [HeightAsBuffer](~/api/Terminal.Gui/Terminal.Gui.Application.yml#HeightAsBuffer)
* ColorSchemes - Both the default color schemes and user defined color schemes can be configured. See [ColorSchemes](~/api/Terminal.Gui/Terminal.Gui.Colors.yml) for more information.

## Example Configuration File

```json
{
  "@schema": "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
  "Settings": {
    "QuitKey": {
      "Key": "Esc"
    }
  },
  "ColorSchemes": {
    "UICatalog": {
      "Normal": {
        "Foreground": "White",
        "Background": "Black"
      },
      "Focus": {
        "Foreground": "Black",
        "Background": "BrightYellow"
      },
      "HotNormal": {
        "Foreground": "BrightYellow",
        "Background": "Black"
      },
      "HotFocus": {
        "Foreground": "White",
        "Background": "BrightYellow"
      },
      "Disabled": {
        "Foreground": "White",
        "Background": "Gray"
      }
    }
  }
}
```

## Configuration File Schema

Settings are defined in JSON format, according to the schema found here: 

https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json
