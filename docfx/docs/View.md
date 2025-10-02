# View Deep Dive

## Hierarchy

  * @Terminal.Gui.View - The base class for implementing higher-level visual/interactive Terminal.Gui elements. Implemented in the @Terminal.Gui.ViewBase.View base class.
  
  * *SubView* - A View that is contained in another view and will be rendered as part of the containing view's **ContentArea**. SubViews are added to another view via the @"Terminal.Gui.ViewBase.View.Add(Terminal.Gui.View)" method. A View may only be a SubView of a single View. Each View has a @Terminal.Gui.ViewBase.View.SubViews property that is a list of all SubViews that have been added.
  
  * @Terminal.Gui.View.SuperView - The View that is a container for SubViews. Each View has a @Terminal.Gui.ViewBase.View.SuperView property that references it's SuperView after it has been added.
  
  * *Child View* - A view that holds a reference to another view in a parent/child relationship. Terminal.Gui uses the terms "Child" and "Parent" sparingly. Generally SubView/SuperView is preferred.
  
  * *Parent View* - A view that holds a reference to another view in a parent/child relationship, but is NOT a SuperView of the child. Terminal.Gui uses the terms "Child" and "Parent" sparingly. Generally SubView/SuperView is preferred.

## Composition

[!INCLUDE [View Composition](~/includes/view-composition.md)]

### List of Built-In Views

See [List of Built-In Views](views.md)
  
### Commands

See the [Command Deep Dive](command.md).

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

## Modal Views

Views can either be Modal or Non-modal. Modal views take over all user input until the user closes the View. Examples of Modal Views are Toplevel, Dialog, and Wizard. Non-modal views can be used to create a new experience in your application, one where you would have a new top-level menu for example. Setting the `Modal` property on a View to `true` makes it modal.

To run any View (but especially Dialogs, Windows, or Toplevels) modally, invoke the `Application.Run` method on a Toplevel. Use the `Application.RequestStop()` method to terminate the modal execution.

```csharp

```

There is no return value from running modally, so the modal view must have a mechanism to indicate the reason the modal was closed. In the case above, the `okpressed` value is set to true if the user pressed or selected the `Ok` button.

### Dialogs

[Dialogs](~/api/Terminal.Gui.Views.Dialog.yml) are Modal [Windows](~/api/Terminal.Gui.Views.Window.yml) that are centered in the middle of the screen and are intended to be used modally - that is, they run, and they are expected to return a result before resuming execution of the application.

Dialogs expose an API for adding buttons and managing the layout such that buttons are at the bottom of the dialog (e.g. [`AddButton`](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui.Dialog.yml#Terminal_Gui_Dialog_AddButton_Terminal_Gui_Button_)).

Example:
```csharp
bool okpressed = false;
var ok = new Button() { Title = "Ok" };
var cancel = new Button() { Title = "Cancel" };
var dialog = new Dialog () { Text = "Are you sure you want to quit?", Title = "Quit", Buttons = { ok, cancel } };
```

Which will show something like this:

```
+- Quit -----------------------------------------------+
|            Are you sure you want to quit?            |
|                                                      |
|                  [ Ok ] [ Cancel ]                   |
+------------------------------------------------------+
```

### Wizards

[Wizards](~/api/Terminal.Gui.Views.Wizard.yml) are Dialogs that let users step through a series of steps to complete a task. 

```
╔╡Gandolf - The last step╞════════════════════════════════════╗
║                                     The wizard is complete! ║
║☐ Enable Final Final Step                                    ║
║                                     Press the Finish        ║
║                                     button to continue.     ║
║                                                             ║
║                                     Pressing ESC will       ║
║                                     cancel the wizard.      ║
║                                                             ║
║                                                             ║
║─────────────────────────────────────────────────────────────║
║⟦ Back ⟧                                         ⟦► Finish ◄⟧║
╚═════════════════════════════════════════════════════════════╝
```


## Application Concepts 

  * *TopLevel* - The v1 term used to describe a view that can have a MenuBar and/or StatusBar. In v2, we will delete the `TopLevel` class and ensure ANY View can have a menu bar and/or status bar (via `Adornments`).
    * NOTE: There will still be an `Application.Top` which is the [View](~/api/Terminal.Gui.ViewBase.View.yml) that is the root of the `Application`'s view hierarchy.

  * *Runnable* - TBD

  * *Modal* - *Modal* - The term used when describing a [View](~/api/Terminal.Gui.Viewbase.View.yml) that was created using the `Application.Run(view)` or `Application.Run<T>` APIs. When a View is running as a modal, user input is restricted to just that View until `Application.Run` exits. A `Modal` View has its own `RunState`. 
    * In v1, classes derived from `Dialog` were originally thought to only work modally. However, `Wizard` proved that a `Dialog`-based class can also work non-modally. 
    * In v2, we will simplify the `Dialog` class, and let any class be run via `Applicaiton.Run`. The `Modal` property will be set by `Application.Run` so the class can detect it is running modally if it needs to. 

