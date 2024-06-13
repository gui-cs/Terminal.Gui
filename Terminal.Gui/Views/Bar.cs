using System;
using System.Linq;

namespace Terminal.Gui;

/// <summary>
///     Provides a horizontally or vertically oriented container for other views to be used as a menu, toolbar, or status bar.
/// </summary>
/// <remarks>
/// </remarks>
public class Bar : View
{
    /// <inheritdoc/>
    public Bar ()
    {
        SetInitialProperties ();
    }

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="Bar"/>. The default is
    ///     <see cref="Orientation.Horizontal"/>.
    /// </summary>
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    public bool StatusBarStyle { get; set; } = true;

    public override void Add (View view)
    {
        if (Orientation == Orientation.Horizontal)
        {
            //view.AutoSize = true;
        }

        //if (StatusBarStyle)
        //{
        //    // Light up right border
        //    view.BorderStyle = LineStyle.Single;
        //    view.Border.Thickness = new Thickness (0, 0, 1, 0);
        //}

        //if (view is not Shortcut)
        //{
        //    if (StatusBarStyle)
        //    {
        //        view.Padding.Thickness = new Thickness (0, 0, 1, 0);
        //    }

        //    view.Margin.Thickness = new Thickness (1, 0, 0, 0);
        //}

        //view.ColorScheme = ColorScheme;

        // Add any HotKey keybindings to our bindings
        //IEnumerable<KeyValuePair<Key, KeyBinding>> bindings = view.KeyBindings.Bindings.Where (b => b.Value.Scope == KeyBindingScope.HotKey);

        //foreach (KeyValuePair<Key, KeyBinding> binding in bindings)
        //{
        //    AddCommand (
        //                binding.Value.Commands [0],
        //                () =>
        //                {
        //                    if (view is Shortcut shortcut)
        //                    {
        //                        return shortcut.CommandView.InvokeCommands (binding.Value.Commands);
        //                    }

        //                    return false;
        //                });
        //    KeyBindings.Add (binding.Key, binding.Value);
        //}


        base.Add (view);
    }

    private void Bar_LayoutStarted (object sender, LayoutEventArgs e)
    {
        View prevBarItem = null;

        switch (Orientation)
        {
            case Orientation.Horizontal:
                for (var index = 0; index < Subviews.Count; index++)
                {
                    View barItem = Subviews [index];

                    if (!barItem.Visible)
                    {
                        continue;
                    }

                    barItem.BorderStyle = LineStyle.Dashed;
                    if (index == Subviews.Count - 1)
                    {
                        barItem.Border.Thickness = new Thickness (0, 0, 0, 0);
                    }
                    else
                    {
                        barItem.Border.Thickness = new Thickness (0, 0, 1, 0);
                    }

                    barItem.X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast);
                    barItem.Y = Pos.Center ();
                    prevBarItem = barItem;
                }

                break;

            case Orientation.Vertical:
                // CommandView is aligned left, HelpView is aligned right, KeyView is aligned right
                // All CommandView's are the same width, all HelpView's are the same width,
                // all KeyView's are the same width

                int maxCommandWidth = 0;
                int maxHelpWidth = 0;
                int minKeyWidth = 0;

                List<Shortcut> shortcuts = Subviews.Where (s => s is Shortcut && s.Visible).Cast<Shortcut> ().ToList ();

                foreach (Shortcut shortcut in shortcuts)
                {
                    // Let AutoSize do its thing to get the minimum width of each CommandView and HelpView
                    //shortcut.CommandView.SetRelativeLayout (new Size (int.MaxValue, int.MaxValue));
                    minKeyWidth = int.Max (minKeyWidth, shortcut.KeyView.Text.GetColumns ());
                }

                // Set the overall size of the Bar and arrange the views vertically
                var maxBarItemWidth = 0;

                for (var index = 0; index < Subviews.Count; index++)
                {
                    View barItem = Subviews [index];

                    if (barItem is Shortcut scBarItem)
                    {
                        scBarItem.MinimumKeyViewSize = minKeyWidth;
                    }

                    if (!barItem.Visible)
                    {
                        continue;
                    }

                    if (prevBarItem == null)
                    {
                        barItem.Y = 0;
                    }
                    else
                    {
                        // Align the view to the bottom of the previous view
                        barItem.Y = index;
                    }

                    prevBarItem = barItem;

                    if (barItem is Shortcut shortcut)
                    {
                        maxBarItemWidth = Math.Max (maxBarItemWidth, shortcut.Frame.Width);
                    }
                    else
                    {
                        maxBarItemWidth = Math.Max (maxBarItemWidth, barItem.Frame.Width);
                    }

                    barItem.X = 0;
                }

                foreach (Shortcut shortcut in shortcuts)
                {
                    shortcut.Width = maxBarItemWidth;
                }

                Height = Subviews.Count;

                break;
        }
    }

    private void SetInitialProperties ()
    {
        ColorScheme = Colors.ColorSchemes ["Menu"];
        CanFocus = true;

        Width = Dim.Auto ();
        Height = Dim.Auto ();

        LayoutStarted += Bar_LayoutStarted;
    }
}
