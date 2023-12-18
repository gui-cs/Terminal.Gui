# Layout

Terminal.Gui v2 supports the following View layout systems (controlled by the [View.LayoutStyle](~/api/Terminal.Gui.LayoutStyle.yml)):

* **Absolute** - Used to have the View positioned exactly in a location, with a fixed size. Absolute layout is accomplished by constructing a View with an argument of type [Rect](~/api/Terminal.Gui.Rect.yml) or directly changing the `Frame` property on the View.
* **Computed** - The Computed Layout system provides automatic aligning of Views with other Views, automatic centering, and automatic sizing. To use Computed layout set the 
 `X`, `Y`, `Width` and `Height` properties after the object has been created. Views laid out using the Computed Layout system can be resized with the mouse or keyboard, enabling tiled window managers and dynamic terminal UIs.
* **Overlapped** - New in V2 (But not yet) - Overlapped layout enables views to be positioned on top of each other. Overlapped Views are movable and sizable with both the keyboard and the mouse.

Examples:

```csharp
// Absolute layout using a provided rectangle
var label1 = new Label (new Rect (1, 1, 20, 1), "Hello")

// Computed Layout
var label2 = new Label ("Hello") {
    X = Pos.Right (label2),
    Y = Pos.Center (),
    Width = Dim.Fill (),
    Height = 1
};

```

When using *Computed Layout* the `X` and `Y` properties are of type [Pos](~/api/Terminal.Gui.Pos.yml) and the `Width` and `Height` properties are of type [Dim](~/api/Terminal.Gui.Dim.yml) both of which can be created implicitly from integer values.

## The `Pos` Type

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

## The `Dim` Type

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
