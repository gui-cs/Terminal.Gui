*Terminal.Gui* provides a rich set of views and controls for building terminal user interfaces:

* [Button](~/api/Terminal.Gui.Views.Button.yml) - A View that provides an item that invokes an System.Action when activated by the user.
* [CheckBox](~/api/Terminal.Gui.Views.CheckBox.yml) - Shows an on/off toggle that the user can set.
* [ColorPicker](~/api/Terminal.Gui.Views.ColorPicker.yml) - Enables to user to pick a color.
* [ComboBox](~/api/Terminal.Gui.Views.ComboBox.yml) - Provides a drop-down list of items the user can select from.
* [Dialog](~/api/Terminal.Gui.Views.Dialog.yml) - A pop-up Window that contains one or more Buttons.
  * [OpenDialog](~/api/Terminal.Gui.Views.OpenDialog.yml) - A Dialog providing an interactive pop-up Window for users to select files or directories.
  * [SaveDialog](~/api/Terminal.Gui.Views.SaveDialog.yml) - A Dialog providing an interactive pop-up Window for users to save files.
* [FrameView](~/api/Terminal.Gui.Views.FrameView.yml) - A container View that draws a frame around its contents. Similar to a GroupBox in Windows.
* [GraphView](~/api/Terminal.Gui.Views.GraphView.yml) - A View for rendering graphs (bar, scatter etc).
* [Hex viewer/editor](~/api/Terminal.Gui.Views.HexView.yml) - A hex viewer and editor that operates over a file stream. 
* [Label](~/api/Terminal.Gui.Views.Label.yml) - Displays a string at a given position and supports multiple lines.
* [ListView](~/api/Terminal.Gui.Views.ListView.yml) - Displays a scrollable list of data where each item can be activated to perform an action.
* [MenuBar](~/api/Terminal.Gui.Views.MenuBar.yml) - Provides a menu bar with drop-down and cascading menus.
* [MessageBox](~/api/Terminal.Gui.Views.MessageBox.yml) - Displays a modal (pup-up) message to the user, with a title, a message and a series of options that the user can choose from. 
* [ProgressBar](~/api/Terminal.Gui.Views.ProgressBar.yml) - Displays a progress Bar indicating progress of an activity.
* [RadioGroup](~/api/Terminal.Gui.Views.RadioGroup.yml) - Displays a group of labels each with a selected indicator. Only one of those can be selected at a given time
* [ScrollBarView](~/api/Terminal.Gui.Views.ScrollBar.yml) - A scrollbar, either horizontal or vertical.
* [StatusBar](~/api/Terminal.Gui.Views.StatusBar.yml) - A View that snaps to the bottom of a Toplevel displaying set of status items. Includes support for global app keyboard shortcuts.
* [TableView](~/api/Terminal.Gui.Views.TableView.yml) - A View for tabular data based on a System.Data.DataTable. 
* [TimeField](~/api/Terminal.Gui.Views.TimeField.yml) & [DateField](~/api/Terminal.Gui.Views.TimeField.yml) - Enables structured editing of dates and times.
* [TextField](~/api/Terminal.Gui.Views.TextField.yml) - Provides a single-line text entry.
* [TextValidateField](~/api/Terminal.Gui.Views.TextValidateField.yml) - Text field that validates input through a ITextValidateProvider.
* [TextView](~/api/Terminal.Gui.Views.TextView.yml)- A multi-line text editing View supporting word-wrap, auto-complete, context menus, undo/redo, and clipboard operations, 
* [TopLevel](~/api/Terminal.Gui.Views.Toplevel.yml) - The base class for modal/pop-up Windows.
* [TreeView](~/api/Terminal.Gui.Views.TreeView.yml) - A hierarchical tree view with expandable branches. Branch objects are dynamically determined when expanded using a user defined ITreeBuilder.
* [View](~/api/Terminal.Gui.ViewBase.View.yml) - The base class for all views on the screen and represents a visible element that can render itself and contains zero or more nested views.
* [Window](~/api/Terminal.Gui.Views.Window.yml) - A Toplevel view that draws a border around its Frame with a title at the top.
* [Wizard](~/api/Terminal.Gui.Views.Wizard.yml) - Provides navigation and a user interface to collect related data across multiple steps.
