# Layout

Terminal.Gui provides a rich system for how [View](View.md) objects are laid out relative to each other. The layout system also defines how coordinates are specified.

See [View Deep Dive](View.md) and [Arrangement Deep Dive](arrangement.md) for more.

## Lexicon & Taxonomy

### Coordinates

* **Screen-Relative** - Describes the dimensions and characteristics of the underlying terminal. Currently Terminal.Gui only supports applications that run "full-screen", meaning they fill the entire terminal when running. As the user resizes their terminal, the @Terminal.Gui.Application.Screen changes size and the application will be resized to fit. *Screen-Relative* means an origin (`0, 0`) at the top-left corner of the terminal. @Terminal.Gui.ConsoleDriver s operate exclusively on *Screen-Relative* coordinates.

* **Application-Relative** - The dimensions and characteristics of the application. Because only full-screen apps are currently supported, @Terminal.Gui.Application is effectively the same as `Screen` from a layout perspective. *Application-Relative* currently means an origin (`0, 0`) at the top-left corner of the terminal. @Terminal.Gui.Application.Top  is a `View` with a top-left corner fixed at the *Application.Relative* coordinate of (`0, 0`) and is the size of `Screen`.

* **Frame-Relative**  - The @Terminal.Gui.View.Frame property of a `View` is a rectangle that describes the current location and size of the view relative to the `Superview`'s content area. *Frame-Relative* means a coordinate is relative to the top-left corner of the View in question. @Terminal.Gui.View.FrameToScreen and @Terminal.Gui.View.ScreenToFrame are helper methods for translating a *Frame-Relative* coordinate to a *Screen-Relative* coordinate and vice-versa.

* **Content-Relative** - A rectangle, with an origin of (`0, 0`) and size (defined by @Terminal.Gui.View.GetContentSize) where the View's content exists. *Content-Relative* means a coordinate is relative to the top-left corner of the content, which is always (`0,0`). @Terminal.Gui.View.ContentToScreen and @Terminal.Gui.View.ScreenToContent are helper methods for translating a *Content-Relative* coordinate to a *Screen-Relative* coordinate and vice-versa.

* **Viewport-Relative** - A *Content-Relative* rectangle representing the subset of the View's content that is visible to the user: @Terminal.Gui.View.Viewport. 

    If @Terminal.Gui.View.GetContentSize is larger than the @Terminal.Gui.View.Viewport, scrolling is enabled. 
    
    *Viewport-Relative* means a coordinate that is bound by (`0,0`) and the size of the inner-rectangle of the View's `Padding`. The View drawing primitives (e.g. `View.Move`) take *Viewport-Relative* coordinates; `Move (0, 0)` means the `Cell` in the top-left corner of the inner rectangle of `Padding`. `View.ViewportToScreen ()` and `View.ScreenToViewport ()` are helper methods for translating a *Viewport-Relative* coordinate to a *Screen-Relative* coordinate and vice-versa. To convert a *Viewport-Relative* coordinate to a *Content-Relative* coordinate, simply subtract `Viewport.X` and/or `Viewport.Y` from the *Content-Relative* coordinate. To convert a *Viewport-Relative* coordinate to a *Frame-Relative* coordinate, subtract the point returned by @Terminal.Gui.View.GetViewportOffsetFromFrame.

### View Composition

* *@Terminal.Gui.Thickness* - A `record struct` describing a rectangle where each of the four sides can have a width. Valid width values are >= 0. The inner area of a Thickness is the sum of the widths of the four sides minus the size of the rectangle.

* *@Terminal.Gui.View.Frame* - The `Rectangle` that defines the location and size of the @Terminal.Gui.View including all of the margin, border, padding, and content area. The coordinates are relative to the SuperView of the View (or, in the case of `Application.Top`, `ConsoleDriver.Row == 0; ConsoleDriver.Col == 0`). The Frame's location and size are controlled by the `.X`, `.Y`, `.Height`, and `.Width` properties of the View. 

* *Adornments* - The `Thickness`es that separate the `Frame` from the `ContentArea`. There are three Adornments, `Margin`, `Padding`, and `Border`. Adornments are not part of the View's content and are not clipped by the View's `ClipArea`. Examples of Adornments:

* *@Terminal.Gui.View.Margin* - The `Adornment` that separates a View from other SubViews of the same SuperView. The Margin is not part of the View's content and is not clipped by the View's `ClipArea`. By default `Margin` is `{0,0,0,0}`. 

    Enabling @Terminal.Gui.View.ShadowStyle will change the `Thickness` of the `Margin` to include the shadow.

    `Margin` can be used instead of (or with) `Dim.Pos` to position a View relative to another View. 

    Eg. 
    ```cs
    view.X = Pos.Right (otherView) + 1;
    view.Y = Pos.Bottom (otherView) + 1;
    ```
    is equivalent to 
    ```cs
    otherView.Margin.Thickness = new Thickness (0, 0, 1, 1);
    view.X = Pos.Right (otherView);
    view.Y = Pos.Bottom (otherView);
    ```

* *@Terminal.Gui.View.Border* - The `Adornment` where a visual border (drawn using line-drawing glyphs) and the @Terminal.Gui.View.Title are drawn, and where the user can interact with the mouse/keyboard to adjust the Views' [Arrangement](arrangement.md). 

    The Border expands inward; in other words if `Border.Thickness.Top == 2` the border & title will take up the first row and the second row will be filled with spaces. The Border is not part of the View's content and is not clipped by the View's `Clip`.

* *@Terminal.Gui.View.Padding*  - The `Adornment` that offsets the `ContentArea` from the `Border`. `Padding` is `{0, 0, 0, 0}` by default. Padding is not part of the View's content and is not clipped by the View's `Clip`. 

    When, enabled, scroll bars reside within `Padding`. 

## Arrangement Modes

See [Arrangement Deep Dive](arrangement.md) for more.

* *Tile*, *Tiled*, *Tiling* - Refer to a form of @Terminal.Gui.View are visually arranged such that they abut each other and do not overlap. In a Tiled view arrangement, Z-ordering only comes into play when a developer intentionally causes views to be aligned such that they overlap. Borders that are drawn between the SubViews can optionally support resizing the SubViews (negating the need for `TileView`).

* *Overlap*, *Overlapped*, *Overlapping* - Refers to a form [Layout](layout.md) where SubViews of a View are visually arranged such that their Frames overlap. In Overlap view arrangements there is a Z-axis (Z-order) in addition to the X and Y dimension. The Z-order indicates which Views are shown above other views.

## The Frame

The @Terminal.Gui.View.Frame property of a `View` is a rectangle that describes the current location and size of the view relative to the `Superview`'s content area. The `Frame`  has a `Location` and `Size`. The `Location` describes the top-left corner of the view relative to the `SuperView`'s content area. The `Size` describes the width and height of the view. The `Frame` is used to determine where the view is drawn on the screen and is used to calculate the Viewport and content size.

## The Content Area

 The content area is the area where the view's content is drawn. Content can be any combination of the @Terminal.Gui.View.Text property, `Subviews`, and other content drawn by the View. The @Terminal.Gui.View.GetContentSize method gets the size of the content area of the view. *Content Area* refers to the rectangle with a location of `0,0` with the size returned by @Terminal.Gui.View.GetContentSize.

 The Content Area size tracks the size of the @Terminal.Gui.View.Viewport by default. If the content size is set via @Terminal.Gui.View.SetContentSize, the content area is the provided size. If the content size is larger than the @Terminal.Gui.View.Viewport, scrolling is enabled. 

## The Viewport

The Viewport (@Terminal.Gui.View.Viewport) is a rectangle describing the portion of the *Content Area* that is currently visible to the user. It is a "portal" into the content. The `Viewport.Location` is relative to the top-left corner of the inner rectangle of `View.Padding`. If `Viewport.Size` is the same as `View.GetContentSize()`, `Viewport.Location` will be `0,0`. 

To enable scrolling call `View.SetContentSize()` and then set `Viewport.Location` to positive values. Making `Viewport.Location` positive moves the Viewport down and to the right in the content. 

The `View.ViewportSettings` property controls how the Viewport is constrained. By default, the `ViewportSettings` is set to `ViewportSettings.None`. To enable the viewport to be moved up-and-to-the-left of the content, use `ViewportSettings.AllowNegativeX` and or `ViewportSettings.AllowNegativeY`. 

The default `ViewportSettings` also constrains the Viewport to the size of the content, ensuring the right-most column or bottom-most row of the content will always be visible (in v1 the equivalent concept was `ScrollBarView.AlwaysKeepContentInViewport`). To allow the Viewport to be smaller than the content, set `ViewportSettings.AllowXGreaterThanContentWidth` and/or `ViewportSettings.AllowXGreaterThanContentHeight`.


* *@Terminal.Gui.View.GetContentSize()* - The content area is the area where the view's content is drawn. Content can be any combination of the @Terminal.Gui.View.Text property, `Subviews`, and other content drawn by the View. The @Terminal.Gui.View.GetContentSize method gets the size of the content area of the view. *Content Area* refers to the rectangle with a location of `0,0` with the size returned by @Terminal.Gui.View.GetContentSize. The [Layout Deep Dive](layout.md) has more details on the Content Area.

* *@Terminal.Gui.View.Viewport* A rectangle describing the portion of the *Content Area* that is currently visible to the user. It is a "portal" into the content. The `Viewport.Location` is relative to the top-left corner of the inner rectangle of `View.Padding`. If `Viewport.Size` is the same as `View.GetContentSize()`, `Viewport.Location` will be `0,0`. 
  
## Layout

Terminal.Gui provides a rich system for how views are laid out relative to each other. The position of a view is set by setting the `X` and `Y` properties, which are of time @Terminal.Gui.Pos. The size is set via `Width` and `Height`, which are of type @Terminal.Gui.Dim.

```cs
var label1 = new Label () { X = 1, Y = 2, Width = 3, Height = 4, Title = "Absolute")

var label2 = new Label () {
    Title = "Computed",
    X = Pos.Right (otherView),
    Y = Pos.Center (),
    Width = Dim.Fill (),
    Height = Dim.Percent (50)
};
```

The `Frame` property is a rectangle that provides the current location and size of the view relative to the View's `Superview`'s Content area. 

## The `Pos` Type

The @Terminal.Gui.Pos is the type of `View.X` and `View.Y` and supports the following sub-types:

* Absolute position, by passing an integer - @Terminal.Gui.Pos.Absolute(System.Int32).
* Percentage of the parent's view size - @Terminal.Gui.Pos.Percent(System.Int32)
* Anchored from the end of the dimension - @Terminal.Gui.Pos.AnchorEnd(System.Int32)
* Centered, using @Terminal.Gui.Pos.Center()
* The @Terminal.Gui.Pos.Left(Terminal.Gui.View), @Terminal.Gui.Pos.Right(Terminal.Gui.View), @Terminal.Gui.Pos.Top(Terminal.Gui.View), and @Terminal.Gui.Pos.Bottom(Terminal.Gui.View) tracks the position of another view.
* Aligned (left, right, center, etc...) with other views - @Terminal.Gui.Pos.Align(Terminal.Gui.Alignment,Terminal.Gui.AlignmentModes,System.Int32).
* An arbitrary function - @Terminal.Gui.Pos.FuncTerminal.Gui.Pos.Func(System.Func{System.Int32})

All `Pos` coordinates are relative to the SuperView's content area.

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

The @Terminal.Gui.Dim is the type of `View.Width` and `View.Height` and supports the following sub-types:

* Automatic size based on the View's content (either Subviews or Text) - @Terminal.Gui.Dim.Auto(Terminal.Gui.DimAutoStyle,Terminal.Gui.Dim,Terminal.Gui.Dim) - See [Dim.Auto Deep Dive](dimauto.md).
* Absolute size, by passing an integer - @Terminal.Gui.Dim.Absolute(System.Int32).
* Percentage of the SuperView's Content Area  - @Terminal.Gui.Dim.Percent(System.Int32).
* Fill to the end of the SuperView's Content Area - @Terminal.Gui.Dim.Fill.
* Reference the Width or Height of another view - @Terminal.Gui.Dim.Width(Terminal.Gui.View), @Terminal.Gui.Dim.Height(Terminal.Gui.View).
* An arbitrary function - @Terminal.Gui.Dim.Func(System.Func{System.Int32}).

All `Dim` dimensions are relative to the SuperView's content area.

Like, `Pos`, objects of type `Dim` can be combined using addition or subtraction, like this:

```cs
// Set the Width to be 10 characters less than filling 
// the remaining portion of the screen
view.Width = Dim.Fill () - 10;

view.Height = Dim.Percent(20) - 1;

anotherView.Height = Dim.Height (view) + 1;
```
