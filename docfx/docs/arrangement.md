# View Layout Arrangement

Terminal.Gui provides a feature of Layout known as **Arrangement**, which controls how the user can use the mouse and keyboard to arrange views and enables either **Tiled** or **Overlapped** layouts. Arrangement is a sub-topic of [Layout](layout.md).


* **Arrangement** - Describes the feature of [Layout](layout.md) which controls how the user can use the mouse and keyboard to arrange views and enables either **Tiled** or **Overlapped** layouts.

* **Arrange Mode** - The Arrange Modes are set via the @Terminal.Gui.View.Arrangement property. When a user presses `Ctrl+F5` (configurable via the @Terminal.Gui.Application.ArrangeKey property) the application goes into **Arrange Mode**. In this mode, indicators are displayed on an arrangeable view indicating which aspect of the View can be arranged. If @Terminal.Gui.ViewArrangement.Movable, a `◊` will be displayed in the top-left corner of the @Terminal.Gui.View.Border. If @Terminal.Gui.ViewArrangement.Resizable, pressing `Tab` (or `Shift+Tab`) will cycle to an an indictor in the bottom-right corner of the Border. The up/down/left/right cursor keys will act appropriately. `Esc`, `Ctrl+F5` or clicking outside of the Border will exit Arrange Mode.

* **Modal** - A modal view is one that is run as an "application" via @Terminal.Gui.Application.Run(System.Func{System.Exception,System.Boolean},Terminal.Gui.IConsoleDriver) where `Modal == true`. `Dialog`, `Messagebox`, and `Wizard` are the prototypical examples. When run this way, there IS a `z-order` but it is highly-constrained: the modal view has a z-order of 1 and everything else is at 0. 

* **Movable** - Describes a View that can be moved by the user using the keyboard or mouse. **Movable** is enabled on a per-View basis by setting the @Terminal.Gui.ViewArrangement.Movable flag on @Terminal.Gui.View.Arrangement. Dragging on the top @Terminal.Gui.View.Border of a View will move such a view. Pressing `Ctrl+F5` will activate **Arrange Mode** letting the user move the view with the up/down/left/right cursor keys.

* **Overlapped** - A form of layout where SubViews of a View are visually arranged such that their Frames overlap. In Overlap view arrangements there is a Z-axis (Z-order) in addition to the X and Y dimension. The Z-order indicates which Views are shown above other views. **Overlapped** is enabled on a per-View basis by setting the @Terminal.Gui.ViewArrangement.Overlapped flag on @Terminal.Gui.View.Arrangement. 

* **Sizable** - Describes a View that can be sized by the user using the keyboard or mouse. **Sizable** is enabled on a per-View basis by setting the @Terminal.Gui.ViewArrangement.Resizable flag on @Terminal.Gui.View.Arrangement. Dragging on the left, right, or bottom @Terminal.Gui.View.Border of a View will size that side of such a view. Pressing `Ctrl+F5` will activate **Arrange Mode** letting the user size the view with the up/down/left/right cursor keys.

* **Tiled** - A form of layout where SubViews of a View are visually arranged such that their Frames do not typically overlap. With **Tiled** views, there is no 'z-order` to the Subviews of a View. In most use-cases, subviews do not overlap with each other (the exception being when it's done intentionally to create some visual effect). As a result, the default layout for most TUI apps is "tiled", and by default @Terminal.Gui.View.Arrangement is set to @Terminal.Gui.ViewArrangement.Fixed.

* **Runnable** - Today, Overlapped and Runnable are intrinsically linked. A runnable view is one where `Application.Run(Toplevel)` is called.  Each *Runnable* view where (`Modal == false`) has it's own `RunState` and is, effectively, a self-contained "application". `Application.Run()` non-preemptively dispatches events (screen, keyboard, mouse , Timers, and Idle handlers) to the associated `Toplevel`. It is possible for such a `Toplevel` to create a thread and call `Application.Run(someotherToplevel)` on that thread, enabling pre-emptive user-interface multitasking (`BackgroundWorkerCollection` does this). 


