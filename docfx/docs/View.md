# View Deep Dive

## View Lexicon & Taxonomy

### Hierarchy

  * *@"Terminal.Gui.View"* - The base class for implementing higher-level visual/interactive Terminal.Gui elements. Implemented in the @Terminal.Gui.View base class.
  
  * *SubView* - A View that is contained in another view and will be rendered as part of the containing view's **ContentArea**. SubViews are added to another view via the @"Terminal.Gui.View.Add(Terminal.Gui.View)" method. A View may only be a SubView of a single View. Each View has a @Terminal.Gui.View.SubViews property that is a list of all SubViews that have been added.
  
  * *@"Terminal.Gui.View.SuperView"* - The View that is a container for SubViews. Each View has a @Terminal.Gui.View.SuperView property that references it's SuperView after it has been added.
  
  * *Child View* - A view that holds a reference to another view in a parent/child relationship. Terminal.Gui uses the terms "Child" and "Parent" sparingly. Generally SubView/SuperView is preferred.
  
  * *Parent View* - A view that holds a reference to another view in a parent/child relationship, but is NOT a SuperView of the child. Terminal.Gui uses the terms "Child" and "Parent" sparingly. Generally SubView/SuperView is preferred.
  
### Input

See the [Keyboard Deep Dive](keyboard.md) and [Mouse Deep Dive](mouse.md).

### Layout and Arrangement

See the [Layout Deep Dive](layout.md) and the [Arrangement Deep Dive](arrangement.md).

### Drawing

See the [Drawing Deep Dive](drawing.md).

### Navigation

See the [Navigation Deep Dive](navigation.md).

### Scrolling

See the [Scrolling Deep Dive](scrolling.md).


### Application Concepts 

  * *TopLevel* - The v1 term used to describe a view that can have a MenuBar and/or StatusBar. In v2, we will delete the `TopLevel` class and ensure ANY View can have a menu bar and/or status bar (via `Adornments`).
    * NOTE: There will still be an `Application.Top` which is the [View](~/api/Terminal.Gui.View.yml) that is the root of the `Application`'s view hierarchy.

  * *Runnable* - TBD

  * *Modal* - *Modal* - The term used when describing a [View](~/api/Terminal.Gui.View.yml) that was created using the `Application.Run(view)` or `Application.Run<T>` APIs. When a View is running as a modal, user input is restricted to just that View until `Application.Run` exits. A `Modal` View has its own `RunState`. 
    * In v1, classes derived from `Dialog` were originally thought to only work modally. However, `Wizard` proved that a `Dialog`-based class can also work non-modally. 
    * In v2, we will simplify the `Dialog` class, and let any class be run via `Applicaiton.Run`. The `Modal` property will be set by `Application.Run` so the class can detect it is running modally if it needs to. 

