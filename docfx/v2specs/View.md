# V2 Spec for View refactor

IMPORTANT: I am critical of the existing codebase below. Do not take any of this personally. It is about the code, not the amazing people who wrote the code.

ALSO IMPORTANT: I've written this to encourage and drive DEBATE. My style is to "Have strong opinions, weakly held." If you read something here you don't understand or don't agree with, SAY SO. Tell me why. Take a stand. 

This covers my thinking on how we will refactor `View` and the classes in the `View` heirarchy(inclidng `Responder`). It does not cover 
  * Text formatting which will be covered in another spec. 
  * TrueColor support which will be covered separately.
  * ConsoleDriver refactor.

## What's wrong with the View and the View-class Heirarchy in v1?



## Terminal.Gui v2 View-related Lexicon & Taxonomy

  * *Responder* - A class that can handle user input. Implemented in the `Responder` base-class. 
    * In v2 we will move all mouse/keyboard base-logic out of `View` and `Window` and into `Responder`.
  * *Container* - A class that can hold other Responders. Implemented in the `Container` base-class. 
    * In v2 we will move all logic for adding/removing views out of `View` and `Window` and into `Container`.
    * NOT DONE YET and somewhat confused in my current WIP.
  * *View* - A base-class for implementing higher level visual/interactive Terminal.Gui elements. Implemented in the `View` base-class, which is a `Responder` and a `Container`. 
    * In v2 we will move all logic for rendering out of `View` and `Window` and into `View`.
  * *SubView* - A View that is contained in antoher view and will be rendered as part of the containing view's *ContentArea*. SubViews are added to another view via the `View.Add` method. A View may only be a SubView of a single View. 
  * *SuperView* - The View that a *SubView* was added to. 
  * *Child View* - A view that is held by another view in a parent/child relationshiop, but is NOT a SubView. Examples of this are sub-menus of `MenuBar`. 
  * *Parent View* - A view that holds a reference to another view in a parent/child relationship, but is NOT a SuperView of the child. 
  * *Thickness* - Describes rectangle where each of the four sides can have a width. Valid width values are >= 0. The inner area of a Thickness is the sum of the widths of the four sides minus the size of the rectangle.
  * *Margin* - Means the Thickness that separtes a View from other SubViews of the same SuperView. The Margin is not part of the View's content and is not clipped by the View's `ClipArea`. 
    * QUESTION: Will it be possilble to have a negative Margin? If so, will that allow us to have "magic borderframe connections" as I've demonsrated in my TileViewExperiment?
  * *Title* - Means text that is displayed for the View that describes the View to users. Typically the Title is displayed at the top-left, overlaying the Border. The Title is not part of the View's content and is not clipped by the View's `ClipArea`. 
  * *Border* - Means the Thickness where a visual border (drawn using line-drawing glyphs) and the Title are drawn. The Border expands inward; in other words if `Border.Thickness.Top == 2` the border & title will take up the first row and the second row will be filled with spaces. The Border is not part of the View's content and is not clipped by the View's `ClipArea`.
  * *Adornments* (NOT IMPLEMENTED YET)- The Thickness between the Margin and Padding. The Adornments property of `View` is a `View`-subclass that hosts SubViews that are not part of the View's content and are rendered within the Adornment Thickness. Adornments are not part of the View's content and are not clipped by the View's `ClipArea`. Examples of Adornments:
    * A `TitleBar` renders the View's `Title` and a horizontal line defining the top of the View. Adds thickness to the top of Adornments. 
    * One or more `LineView`s that render the View's border (NOTE: The magic of `LineCanvas` lets us automatically have the right joins for these and `TitleBar`!).
    * A `Vertical Scrollbar` adds thickness to `Adornments.Right` (or `.Left` when right-to-left language support is added). 
    * A `Horizontal Scrollbar` adds thickness to `Adornments.Bottom` when enabled.
    * A `MenuBar` adds thickness to `Adornments.Top` (NOTE: This is a change from v1 where `subview.Y = 1` is required).
    * A `StatusBar` adds thickness ot `Adornments.Bottom` and is rendered at the bottom of Padding.
    * NOTE: The use of `View.Add` in v1 to add adornments to Views is the cause of much code complexity. Changing the API such that `View.Add` is ONLY for subviews and adding a `View.Adornments.Add` API for menu, statusbar, scroll bar... will enable us to signficantly simplify the codebase.
  * *Padding* - Means the Thickness inside of an element that offsets the `Content` from the Border. (NOTE: in v1 `Padding` is OUTSIDE of the `Border`). Padding is `{0, 0, 0, 0}` by default. Padding is not part of the View's content and is not clipped by the View's `ClipArea`.
  * *Frame* - Means the `Rect` that defines the location and size of the `View` including all of the margin, border, adornments, padding, and content area. The coordinates are relative to the SuperView of the View (or, in the case of `Application.Top`, `ConsoleDriver.Row == 0; ConsoleDriver.Col == 0`). The Frame's location and size are controlled by either `Absolute` or `Computed` positioning via the `.X`, `.Y`, `.Height`, and `.Width` properties of the View. 
  * *VisibleArea* - (NOT IMPLEMENTED YET) Means the area inside of the Margin + Border (Title) + Padding. `VisibleArea.Location` is always `{0, 0}`. `VisibleArea.Size` is the `View.Frame.Size` shrunk by Margin + Border + Padding. 
  * *ContentArea* - (NOT IMPLEMENTED YET; currently `Bounds`) The `Rect` that describes the location and size of the View's content, relative to `VisibleArea`. If `ContentArea.Location` is negative, anything drawn there will be clipped and any subview positioned in the negative area will cause (optional) scrollbars to appear (making the Thickness of Padding thicker on the appropriate sides). If `ContentArea.Size` is changed such that the dimensions fall outside of `Frame.Size shrunk by Margin + Border + Padding`, drawning will be clipped and (optional) scrollbars will appear.
    * QUESTION: Can we just have one `ContentArea` property that is the `Rect` that describes the location and size of the View's content, relative to `Frame`? If so, we can remove `VisibleArea` and `Bounds` and just have `ContentArea` and `Frame`? The key to answering this is all wrapped up in scrolling and clipping.
  * *Bounds* - Synomous with *VisibleArea*. (Debate: Do we rename `Bounds` to `VisbleArea` in v2?)
  * *ClipArea* - Means the currently vislble portion of the *Content*. This is defined as a`Rect` in coordinates relative to *ContentArea* (NOT *VisibleArea*) (e.g. `ClipArea {X = 0, Y = 0} == ContentArea {X = 0, Y = 0}`). This `Rect` is passed to `View.Redraw` (and should be named "clipArea" not "bounds"). It defines the clip-region the caller desires the `Redraw` implementation to clip itself to (see notes on clipping below).
  * *Modal* - The term used when describing a View that was created using the `Application.Run(view)` or `Application.Run<T>` APIs. When a View is running as a modal, user input is restricted to just that View until `Application.Run` exits. A `Modal` View has its own `RunState`. 
  * *TopLevel* - The v1 term used to describe a view that is both Modal and can have a MenuBar and/or StatusBar. I propose in v2 we deprecate the term `TopLevel` and instead use `Modal` to describe the same thing. I do not think `Modal` should be a class, but a property of `View` that can be set to `true` or `false`.
  * *Window* - A View that, by default, has a `Border` and a `Title`. 
    * QUESTION: Why can't this just be a property on `View` (e.g. `View.Border = true`)? Why do we need a `Window` class at all in v2?

### View classes to be nuked
* PanelView (done)
* FrameView (almost done)
* Window?


  ### Questions


### Problems with Current Architecture & Implementation

* `Frame`, `Bounds`, and `ClipRect` are confusing and not consistently applied...
  * `Bounds` is `Rect` but is used to describe a `Size` (e.g. `Bounds.Size` is the size of the `View`'s content area). It literaly is implemented as a property that returns `new Rect(0, 0, Width, Height)`. Throughtout the codebase `bounds` is used for things that have non-zero `Size` (and actually descibe either the cliprect or the Frame).
  * The restrictive nature of how `Bounds` is defined led to the hacky `FrameView` and `Window` classes with an embedded `ContentView` in order to draw a border around the content. 
    * The only reason FrameView exists is because the original architecture didn't support offsetting `View.Bounds`  such that a border could be drawn and the interior content would clip correctly. Thus Miguel (or someone) built
  FrameView with nested `ContentView` that was at `new Rect(+1, +1, -2, -2)`. 
    * `Border` was added later, but couldn't be retrofitted into `View` such that if `View.Border ~= null` just worked like `FrameView`.
    * Thus devs are forced to use the clunky `FrameView` instead of just setting `View.Border`.
  * `Border` has a bunch of confusing concepts that don't match other systems (esp the Web/HTML)
    * `Margin` on the web means the space between elements - `Border` doesn't have a margin property, but does has the confusing `DrawMarginFrame` property.
    * `Border` on the web means the space where a border is drawn. The current implementaiton confuses the term `Frame` and `Border`. `BorderThickness` is provided. 
    * `Padding` on the web means the padding inside of an element between the `Border` and `Content`. In the current implementation `Padding` is actually OUTSIDE of the `Border`. This means it's not possible for a view to offset internally by simply changing `Bounds`. 
    * `Content` on the web means the area inside of the Margin + Border + Padding. `View` does not currently have a concept of this (but `FrameView` and `Window` do via the embeded `ContentView`s.
    * `Border` has a `Title` property. So does `Window` and `FrameView`. This is unneeded duplicate code.
    * It is not possilble for a class drived from View to orverride the drawing of the "Border" (frame, title, padding, etc...). Multiple devs have asked to be able to have the border frame to be drawn with a different color than `View.ColorScheme`. The API should explicitly enable devs to override the drawing of `Border` independently of the `View.Draw` method. See how `WM_NCDRAW` works in wWindows (Draw non-client). It should be easy to do this from within a `View` sub-class (e.g. override `OnDrawBorder`) and externally (e.g. `DrawBorder += () => ...`. 

* `AutoSize` mostly works, but only because of heroic special-casing logic all over the place by @bdisp. This should be massively simplified.
* `FrameView` is superlufous and should be removed from the heirarchy (instead devs should just be able to manipulate `View.Border` (or similar) to achieve what `FrameView` provides). The internal `FrameView.ContentView` is a bug-farm and un-needed if `View.Border` worked correctly. 
* `TopLevel` is currently built around several concepts that are muddled:
  * Views that host a Menu and StatusBar. It is not clear why this is and if it's needed as a concept. 
  * Views that can be run via `Application.Run<TopLevel>` (need a separate `RunState`). It is not clear why ANY VIEW can't be run this way, but it seems to be a limitation of the current implementation.
  * Views that can be used as a pop-up (modal) (e.g. `Dialog`). As proven by `Wizard`, it is possible to build a View that works well both ways. But it's way too hard to do this today.
  * Views that can be moved by the user must inherit from `Window` today. It should be possilbe to enable moving of any View (e.g. `View.CanMove = true`).
* The `MdiContainer` stuff is complex, perhaps overly so, and is not actually used by anyone outside of the project. It's also mis-named because Terminal.Gui doesn't actually support "documents" nor does it have a full "MDI" system like Windows (did). It seems to represent features useful in overlapping Views, but it is super confusing on how this works, and the naming doesn't help. This all can be refactored to support specific scenarios and thus be simplified.
* There is no facility for users' resizing of Views. @tznind's awesome work on `LineCanvas` and `TileView` combined with @tig's experiments show it could be done in a great way for both modal (overlapping) and tiled Views. 
* `DrawFrame` and `DrawTitle` are implemented in `ConsoleDriver` and can be replaced by a combination of `LineCanvas` and `Border`.
* Colors - 
  * As noted above each of Margin, Border, Padding, and Content should support independent colors.
  * Many View sub-classes bastardize the exiting ColorSchemes to get look/feel that works (e.g. `TextView` and `Wizard`). Separately we should revamp ColorSchemes to enable more scenarios. 
  * TrueColor support is needed and should be the default.
* `Responder` is supposed to be where all common, non-visual-related, code goes. We should ensure this is the case.
* `View` should have default support for scroll bars. e.g. assume in the new world `View.ContentBounds` is the clip area (defined by `VIew.Frame` minus `Margin` + `Border` + `Padding`) then if any view is added with `View.Add` that has Frame coordinates outside of `ContentBounds` the appropriate scroll bars show up automatgically (optioally of course). Without any code, scrolling just works. 
* We have many requests to support non-full-screen apps. We need to ensure the `View` class heirachy suppports this in a simple, understandable way. In a world with non-full-screen (where screen is defined as the visible terminal view) apps, the idea that `Frame` is "screen relative" is broken. Although we COULD just define "screen" as "the area that bounds the Terminal.GUI app.".  

## Thoughts on Built-in Views
* `LineView` can be replaced by `LineCanvas`?
* `Button` and `Label` can be merged. 
* `StatusBar` and `Menu` could be combined. If not, then at least made more consistent (e.g. in how hotkeys are specified).

## Design

* `Responder`("Responder base class implemented by objects that want to participate on keyboard and mouse input.") remains mostly unchanged, with minor changes:
   * Methods that take `View` parametsrs (e.g. `OnEnter`) change to take `Responder` (bad OO design).
   * Nuke `IsOverriden` (bad OO design)
   * Move `View.Data` to `Responder` (primitive)
   * Move `Command` and `KeyBinding` stuff from `View`.
   * Move generic mouse and keyboard stuff from `View` (e.g. `WantMousePositionReports`)


## Example of creating Adornments
```cs
// ends up looking just like the v1 default Window with a menu & status bar
// and a vertical scrollbar. In v2 the Window class would do all of this automatically.
var top = new TitleBar() {
    X = 0, Y = 0,
    Width = Dim.Fill(),
    Height = 1
    LineStyle = LineStyle.Single
};
var left = new LineView() {
    X = 0, Y = 0,
    Width = 1,
    Height = Dim.Fill(),
    LineStyle = LineStyle.Single
};
var right = new LineView() {
    X = Pos.AnchorEnd(), Y = 0,
    Width = 1,
    Height = Dim.Fill(),
    LineStyle = LineStyle.Single
};
var bottom = new LineView() {
    X = 0, Y = Pos.AnchorEnd(),
    Width = Dim.Fill(),
    Height = 1,
    LineStyle = LineStyle.Single
};

var menu = new MenuBar() { 
    X = Pos.Right(left), Y = Pos.Bottom(top)
};
var status = new StatusBar () {
    X = Pos.Right(left), Y = Pos.Top(bottom)
};
var vscroll = new ScrollBarView () {
    X = Pos.Left(right),
    Y = Dim.Fill(2) // for menu & status bar
};

Adornments.Add(titleBar);
Adornments.Add(left);
Adornments.Add(right);
Adornments.Add(bottom);
Adornments.Add(vscroll);

var treeView = new TreeView () {
    X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
};
Add (treeView);
```
