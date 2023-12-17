# Layout

## Tenets for Terminal.Gui View Layout (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Users Have Control** - *Terminal.Gui* provides default key bindings consistent with these tenets, but those defaults are configurable by the user. For example, `ConfigurationManager` allows users to redefine key bindings for the system, a user, or an application.

* **More Editor than Command Line** - Once a *Terminal.Gui* app starts, the user is no longer using the command line. Users expect keyboard idioms in TUI apps to be consistent with GUI apps (such as VS Code, Vim, and Emacs). For example, in almost all GUI apps, `Ctrl-V` is `Paste`. But the Linux shells often use `Shift-Insert`. *Terminal.Gui* binds `Ctrl-V` by default.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to keyboard input in a way that is consistent with the platform. For example, on Windows to erase a word to the left, users press `Ctrl-Backspace`. But on Linux, `Ctrl-W` is used.

* **The Source of Truth is Wikipedia** - We use this [Wikipedia article](https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts) as our guide for default key bindings.



Terminal.Gui supports two different layout systems, absolute and computed \
(controlled by the [LayoutStyle](~/api/Terminal.Gui.LayoutStyle.yml)
property on the view.

The absolute system is used when you want the view to be positioned exactly in
one location and want to manually control where the view is. This is done
by invoking your View constructor with an argument of type [Rect](~/api/Terminal.Gui.Rect.yml). When you do this, to change the position of the View, you can change the `Frame` property on the View.

The computed layout system offers a few additional capabilities, like automatic
centering, expanding of dimensions and a handful of other features. To use
this you construct your object without an initial `Frame`, but set the 
 `X`, `Y`, `Width` and `Height` properties after the object has been created.

Examples:

```csharp

// Dynamically computed
var label = new Label ("Hello") {
    X = 1,
    Y = Pos.Center (),
    Width = Dim.Fill (),
    Height = 1
};

// Absolute position using the provided rectangle
var label2 = new Label (new Rect (1, 2, 20, 1), "World")
```

The computed layout system does not take integers, instead the `X` and `Y` properties are of type [Pos](~/api/Terminal.Gui.Pos.yml) and the `Width` and `Height` properties are of type [Dim](~/api/Terminal.Gui.Dim.yml) both which can be created implicitly from integer values.

### The `Pos` Type

The `Pos` type on `X` and `Y` offers a few options:
* Absolute position, by passing an integer
* Percentage of the parent's view size - `Pos.Percent(n)`
* Anchored from the end of the dimension - `AnchorEnd(int margin=0)`
* Centered, using `Center()`
* Reference the Left (X), Top (Y), Bottom, Right positions of another view

The `Pos` values can be added or subtracted, like this:

```csharp
// Set the X coordinate to 10 characters left from the center
view.X = Pos.Center () - 10;

view.Y = Pos.Percent (20);

anotherView.X = AnchorEnd (10);
anotherView.Width = 9;

myView.X = Pos.X (view);
myView.Y = Pos.Bottom (anotherView);
```

### The `Dim` Type

The `Dim` type is used for the `Width` and `Height` properties on the View and offers
the following options:

* Absolute size, by passing an integer
* Percentage of the parent's view size - `Dim.Percent(n)`
* Fill to the end - `Dim.Fill ()`
* Reference the Width or Height of another view

Like, `Pos`, objects of type `Dim` can be added an subtracted, like this:


```csharp
// Set the Width to be 10 characters less than filling 
// the remaining portion of the screen
view.Width = Dim.Fill () - 10;

view.Height = Dim.Percent(20) - 1;

anotherView.Height = Dim.Height (view)+1
```
