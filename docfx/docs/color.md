# Color

## Tenets for Terminal.Gui Color Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Gracefully Degrade** - 
* ..

## Color APIs

...

The [ColorScheme](~/api/Terminal.Gui.ColorScheme.yml) represents
four values, the color used for Normal text, the color used for normal text when
a view is focused an the colors for the hot-keys both in focused and unfocused modes.

By using `ColorSchemes` you ensure that your application will work correctbly both
in color and black and white terminals.

Some views support setting individual color attributes, you create an
attribute for a particular pair of Foreground/Background like this:

```
var myColor = Application.Driver.MakeAttribute (Color.Blue, Color.Red);
var label = new Label (...);
label.TextColor = myColor
```
