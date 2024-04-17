# Layout

Terminal.Gui provides a rich system for how `View` objects are laid out relative to each other. The layout system also defines how coordinates are specified.

## Dealing with coordinates

The coordinate systems in Terminal.Gui are:

* `Screen` - Describes the dimensions and characteristics of the underlying terminal. Currently Terminal.Gui only supports applications that run "full-screen", meaning they fill the entire terminal when running. As the user resizes their terminal, the `Screen` changes size and the applicaiton will be resized to fit. *Screen-Relative* means an origin (`0, 0`) at the top-left corner of the terminal. `ConsoleDriver`s operate exclusively on *Screen-Relative* coordinates.
* `Application` - The dimensions and characteristics of the application. Because only full-screen apps are currently supported, `Application` is effectively the same as `Screen` from a layout perspective. *Application-Relative* currently means an origin (`0, 0`) at the top-left corner of the terminal. `Applicaiton.Top` is a `View` with a top-left corner fixed at the *Application.Relative* coordinate of (`0, 0`) and is the size of `Screen`.
* `View Frame` - A rectangle at a particular point, and of a particular size, within another `View`, referred to as the `Superview`. *Frame-Relative* means a coordinate is relative to the top-left corner of the View in question. `View.FrameToScreen ()` and `View.ScreenToFrame ()` are helper methods for translating a *Frame-Relative* coordinate to a *Screen-Relative* coordinate and vice-versa.
* `View Content/Content Area` - A rectangle, with an origin of (`0, 0`) and size (defined by `View.ContentSize`) where the View's content exists. *Content-Relative* means a coordinate is relative to the top-left corner of the content, which is always (`0,0`). `View.ContentToScreen ()` and `View.ScreenToContent ()` are helper methods for translating a *Content-Relative* coordinate to a *Screen-Relative* coordinate and vice-versa.
* `Viewport` - A *Content-Relative* rectangle representing the subset of the View's content that is visible to the user. If `View.ContentSize` is larger than the Viewport, scrolling is enabled. *Viewport-Relative* means a coordinate that is bound by (`0,0`) and the size of the inner-rectangle of the View's `Padding`. The View drawing primitives (e.g. `View.Move`) take *Viewport-Relative* coordinates; `Move (0, 0)` means the `Cell` in the top-left corner of the inner rectangle of `Padding`. `View.ViewportToScreen ()` and `View.ScreenToViewport ()` are helper methods for translating a *Viewport-Relative* coordinate to a *Screen-Relative* coordinate and vice-versa. To convert a *Viewport-Relative* coordinate to a *Content-Relative* coordinate, simply subtract `Viewport.X` and/or `Viewport.Y` from the *Content-Relative* coordinate. To convert a *Viewport-Relative* coordinate to a *Frame-Relative* coordinate, subtract the point returned by `View.GetViewportOffsetFromFrame`.

## The Viewport

The Viewport (`View.Viewport`) is a rectangle describing a sub-set of the View's content. It is a "portal" into the content. The `Viewport.Location` is relative to the top-left corner of the inner rectangle of `View.Padding`. If `Viewport.Size` is the same as `View.ContentSize`, `Viewport.Location` will be `0,0`. 

To enable scrolling set `View.ContentSize` and then set `Viewport.Location` to positive values. Making `Viewport.Location` positive moves the Viewport down and to the right in the content. 

The `View.ViewportSettings` property controls how the Viewport is constrained. By default, the `ViewportSettings` is set to `ViewportSettings.None`. To enable the viewport to be moved up-and-to-the-left of the content, use `ViewportSettings.AllowNegativeX` and or `ViewportSettings.AllowNegativeY`. 

The default `ViewportSettings` also constrains the Viewport to the size of the content, ensuring the right-most column or bottom-most row of the content will always be visible (in v1 the equivalent concept was `ScrollBarView.AlwaysKeepContentInViewport`). To allow the Viewport to be smaller than the content, set `ViewportSettings.AllowXGreaterThanContentWidth` and/or `ViewportSettings.AllowXGreaterThanContentHeight`.

## Layout

Terminal.Gui provides a rich system for how views are laid out relative to each other. **Computed Layout** means the position and size of a View is being computed by the layout engine. **Absolute Layout** means the position and size of a View has been declared to be an absolute values. The position of a view is set by setting the `X` and `Y` properties, which are of time [Pos](~/api/Terminal.Gui.Pos.yml). The size is set via `Width` and `Height`, which are of type [Dim](~/api/Terminal.Gui.Dim.yml).

* **Computed Layout** supports dynamic console apps where UI elements (Views) automatically adjust as the terminal resizes or other Views change size or position. The X, Y, Width, and Height properties are Dim and Pos objects that dynamically update the position of a view.

* **Absolute Layout** requires specifying coordinates and sizes of Views explicitly, and the View will typically stay in a fixed position and size.

```cs
// Absolute layout using a absoulte values for X, Y, Width, and Height
var label1 = new Label () { X = 1, Y = 2, Width = 3, Height = 4, Title = "Absolute")

// Computed Layout using Pos and Dim objects for X, Y, Width, and Height
var label2 = new Label () {
    Title = "Computed",
    X = Pos.Right (otherView),
    Y = Pos.Center (),
    Width = Dim.Fill (),
    Height = Dim.Percent (50)
};

```

The `Frame` property is a rectangle that provides the current location and size of the view relative to the View's `Superview`'s Content area. 

The `View.LayoutStyle` property can be used to determine how the View is treated. If the style is `Absolute`, the `X`, `Y`, `Width`, and `Height` objects are all absolute values. If the style is `Computed`, one or more of the `X`, `Y`, `Width`, and `Height` objects describe a computed value.

## The `Pos` Type

The [Pos](~/api/Terminal.Gui.Pos.yml) is the type of `View.X` and `View.Y` and supports the following sub-types:

* Absolute position, by passing an integer - `Pos.Absolute(n)`.
* Percentage of the parent's view size - `Pos.Percent(percent)`.
* Anchored from the end of the dimension - `Pos.AnchorEnd()`.
* Centered, using `Pos.Center()`.
* The `Pos.Left(otherView)`, `Pos.Top(otherView)`, `Pos.Bottom(otherView)`, `Pos.Right(otherView)` positions of another view.

All `Pos` coordinates are relative to the Superview's content area.

`Pos` values can be combined using addition or subtraction:

```cs
// Set the X coordinate to 10 characters left from the center
view.X = Pos.Center () - 10;

view.Y = Pos.Percent (20);

anotherView.X = AnchorEnd (10);
anotherView.Width = 9;

myView.X = Pos.X (view);
myView.Y = Pos.Bottom (anotherView) + 5;
```
## The `Dim` Type

The [Dim](~/api/Terminal.Gui.Dim.yml) is the type of `View.Width` and `View.Height` and supports the following sub-types:

* Absolute size, by passing an integer - `Dim.Absolute(n)`.
* Percentage of the Superview's Content Area  - `Dim.Percent(percent)`.
* Fill to the end of the Superview's Content Area - `Dim.Fill ()`.
* Reference the Width or Height of another view - `Dim.Width (otherView)`, `Dim.Height (otherView)`.
* An arbitrary function - `Dim.Function(fn)`
* Automatic size based on the View's content (either Subviews or Text) - `Dim.Auto()`

All `Dim` dimensions are relative to the Superview's content area.

Like, `Pos`, objects of type `Dim` can be combined using addition or subtraction, like this:

```cs
// Set the Width to be 10 characters less than filling 
// the remaining portion of the screen
view.Width = Dim.Fill () - 10;

view.Height = Dim.Percent(20) - 1;

anotherView.Height = Dim.Height (view) + 1;
```
