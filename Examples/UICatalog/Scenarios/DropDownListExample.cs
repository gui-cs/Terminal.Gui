#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DropDownListExample", "Shows how to use MenuBar as a Drop Down List")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
public sealed class DropDownListExample : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        Label label = new () { Title = "_DropDown TextField Using MenuBar:" };

        View dropdown = CreateDropDownTextFieldUsingMenuBar ();
        dropdown.X = Pos.Right (label) + 1;

        appWindow.Add (label, dropdown);

        app.Run (appWindow);
    }

    /// <summary>
    ///     Creates a composite view consisting of a <see cref="TextField"/> paired with a single-item
    ///     <see cref="MenuBar"/> that acts as a dropdown button. When the MenuBar receives focus the dropdown
    ///     opens, aligned to the left edge of the TextField.
    /// </summary>
    private View CreateDropDownTextFieldUsingMenuBar ()
    {
        TextField tf = new () { Text = "item 1", Width = 10, Height = 1 };

        MenuItem [] items = Enumerable.Range (1, 5).Select (
            i =>
            {
                MenuItem item = new ($"item {i}", null, null, null);

                item.Accepting += (sender, _) =>
                                  {
                                      if (sender is MenuItem mi)
                                      {
                                          tf.Text = mi.Title;
                                      }
                                  };

                return item;
            }).ToArray ();

        MenuBarItem menuBarItem = new ($"{Glyphs.DownArrow}", items);

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

                                                 if (mbi.PopoverMenu?.Root is { } root)
                                                 {
                                                     root.Width = tf.Width + mbi.Width;
                                                 }

                                                 MenuItem? current = mbi.PopoverMenu?.Root?.SubViews
                                                                        .OfType<MenuItem> ()
                                                                        .FirstOrDefault (mi => mi.Title == tf.Text);
                                                 current?.SetFocus ();
                                             };

        MenuBar mb = new ([menuBarItem]) { CanFocus = true, Width = Dim.Auto (), Y = Pos.Top (tf), X = Pos.Right (tf) };

        // Open the dropdown aligned to the TextField's left edge when the MenuBar receives focus.
        mb.HasFocusChanged += (_, e) =>
                              {
                                  if (e.NewValue)
                                  {
                                      mb.OpenMenu (new Point (tf.FrameToScreen ().X, tf.FrameToScreen ().Bottom));
                                  }
                              };

        View container = new () { CanFocus = true, Height = Dim.Auto (), Width = Dim.Auto () };
        container.Add (tf, mb);

        return container;
    }
}
