using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
///     Provides a horizontally or vertically oriented container for other views to be used as a menu, toolbar, or status bar.
/// </summary>
/// <remarks>
/// </remarks>
public class Bar : Toplevel
{
    /// <inheritdoc/>
    public Bar ()
    {
        CanFocus = true;

        Width = Dim.Auto ();
        Height = Dim.Auto ();

        LayoutStarted += Bar_LayoutStarted;

        Initialized += Bar_Initialized;

    }

    private void Bar_Initialized (object sender, EventArgs e)
    {
        ColorScheme = Colors.ColorSchemes ["Menu"];
        AdjustSubviewBorders();
    }

    /// <inheritdoc />
    public override void SetBorderStyle (LineStyle value)
    {
        // The default changes the thickness. We don't want that. We just set the style.
        Border.LineStyle = value;
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

        base.Add (view);
        AdjustSubviewBorders ();

    }

    /// <inheritdoc />
    public override void Remove (View view)
    {
        base.Remove (view);
        AdjustSubviewBorders ();
    }

    private void AdjustSubviewBorders ()
    {
        for (var index = 0; index < Subviews.Count; index++)
        {
            View barItem = Subviews [index];

            barItem.Border.LineStyle = BorderStyle;
            barItem.SuperViewRendersLineCanvas = true;
            barItem.ColorScheme = ColorScheme;


            if (!barItem.Visible)
            {
                continue;
            }

            if (StatusBarStyle)
            {
                if (index == 0)
                {
                    barItem.Border.Thickness = new Thickness (0, 0, 1, 0);
                } 

                if (index == Subviews.Count - 1)
                {
                    barItem.Border.Thickness = new Thickness (0, 0, 0, 0);
                }
            }
            else
            {
                if (index == 0)
                {
                    barItem.Border.Thickness = new Thickness (1, 1, 1, 0);
                }

                if (index == Subviews.Count - 1)
                {
                    barItem.Border.Thickness = new Thickness (1, 0, 1, 1);
                }
            }
        }
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

                    if (StatusBarStyle)
                    {
                        barItem.BorderStyle = LineStyle.Dashed;
                    }
                    else
                    {
                        barItem.BorderStyle = LineStyle.None;
                    }

                    if (index == Subviews.Count - 1)
                    {
                        barItem.Border.Thickness = new Thickness (0, 0, 0, 0);
                    }
                    else
                    {
                        barItem.Border.Thickness = new Thickness (0, 0, 1, 0);
                    }

                    barItem.X = Pos.Align (Alignment.Start, StatusBarStyle ? AlignmentModes.IgnoreFirstOrLast : 0);
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
                var totalHeight = 0;

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
                        barItem.Y = Pos.Bottom(prevBarItem);
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
                    totalHeight += barItem.Frame.Height;
                }

                foreach (Shortcut shortcut in shortcuts)
                {
                    shortcut.Width = maxBarItemWidth;
                }

                Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: totalHeight);

                break;
        }
    }


}
