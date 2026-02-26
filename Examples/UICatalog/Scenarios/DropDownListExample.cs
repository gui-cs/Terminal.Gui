#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DropDownListExample", "Shows how to use MenuBarItem as a Drop Down List")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
public sealed class DropDownListExample : Scenario
{
    private EventLog _eventLog = null!;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        _eventLog = new EventLog ();

        View contentArea = new ()
        {
            CanFocus = true,
            Width = Dim.Fill (_eventLog),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Rounded
        };

        // Example 1: Editable dropdown using TextField + MenuBarItem directly.
        Label editableLabel = new () { Title = "_Editable DropDown:" };

        View editableDropdown = CreateEditableDropDown ();
        editableDropdown.X = Pos.Right (editableLabel) + 1;

        //// Example 2: ReadOnly ComboBox using the ComboBox helper class.
        //Label readonlyLabel = new ()
        //{
        //    Title = "_ReadOnly DropDown:",
        //    Y = Pos.Bottom (editableLabel) + 1
        //};

        //ComboBox readonlyCombo = new ()
        //{
        //    X = Pos.Right (readonlyLabel) + 1,
        //    Y = readonlyLabel.Y,
        //    Width = 20,
        //    ReadOnly = true
        //};
        //readonlyCombo.SetSource (["Alpha", "Beta", "Gamma", "Delta", "Epsilon"]);
        //readonlyCombo.SelectedItemChanged += (_, e) => _eventLog.Log ($"ReadOnly ComboBox SelectedItemChanged: [{e.Item}] {e.Value}");

        contentArea.Add (editableLabel, editableDropdown/*, readonlyLabel, readonlyCombo*/);
        appWindow.Add (contentArea, _eventLog);

        app.Run (appWindow);
    }

    /// <summary>
    ///     Creates a composite view consisting of a <see cref="TextField"/> paired with a single-item
    ///     <see cref="MenuBarItem"/> that acts as a dropdown button. When the MenuBar receives focus the dropdown
    ///     opens, aligned to the left edge of the TextField.
    /// </summary>
    private View CreateEditableDropDown ()
    {
        TextField tf = new () { Text = "item 2", Height = 1 };

        MenuItem [] items = Enumerable.Range (1, 5)
                                      .Select (i => new MenuItem ($"item {i}", null, null, null))
                                      .ToArray ();

        MenuBarItem menuBarItem = new ($"{Glyphs.DownArrow}", items)
        {
            CanFocus = true,
            X = Pos.AnchorEnd (),
            Y = 0,
            Width = 1,
            ElementSpacing = 0
        };

        tf.Width = Dim.Fill (menuBarItem);

        menuBarItem.PopoverMenu!.SchemeName = "Menu";

        // Anchor the dropdown to the TextField so it opens aligned to tf's left edge.
        menuBarItem.PopoverMenuAnchor = () => tf.FrameToScreen ();

        //menuBarItem.PopoverMenuOpenChanging += (sender, args) =>
        //                                       {
        //                                           if (sender is not MenuBarItem mbi)
        //                                           {
        //                                               return;
        //                                           }

        //                                           Rectangle tfScreen = tf.FrameToScreen ();
        //                                           mbi.PopoverMenu?.Root?.Width = tfScreen.Width;
        //                                       };

        // When the dropdown opens, pre-select the item matching the current TextField value.
        menuBarItem.PopoverMenuOpenChanged += (sender, e) =>
                                              {
                                                  if (!e.NewValue)
                                                  {
                                                      return;
                                                  }

                                                  if (sender is not MenuBarItem mbi)
                                                  {
                                                      return;
                                                  }

                                                  MenuItem? current = mbi.PopoverMenu?.Root?.SubViews.OfType<MenuItem> ()
                                                                         .FirstOrDefault (mi => mi.Title == tf.Text);
                                                  current?.SetFocus ();
                                              };
        _eventLog.SetViewToLog (tf);
        _eventLog.SetViewToLog (menuBarItem);

        View dropDownMenu = new ()
        {
#if DEBUG
            Id = "dropDownMenu",
#endif
            CommandsToBubbleUp = [Command.Activate],
            CanFocus = true,
            Height = Dim.Auto (),
            Width = 30,
            BorderStyle = LineStyle.Single,
            Arrangement = ViewArrangement.Resizable
        };

        dropDownMenu.Activated += (sender, args) =>
                                 {
                                     //if (sender is MenuItem mi)
                                     {
                                         //tf.Text = menuBarItem.PopoverMenu.MostFocused?.Title ?? string.Empty;
                                     }
                                 };

        menuBarItem.PopoverMenu.Activated += (sender, args) =>
                                 {
                                     //if (sender is MenuItem mi)
                                     {
                                         //tf.Text = menuBarItem.PopoverMenu.MostFocused?.Title ?? string.Empty;
                                     }
                                 };

        dropDownMenu.Add (tf, menuBarItem);

        _eventLog.SetViewToLog (dropDownMenu);
        return dropDownMenu;
    }
}
