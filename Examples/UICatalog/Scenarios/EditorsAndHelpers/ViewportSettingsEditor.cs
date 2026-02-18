// ReSharper disable MoveLocalFunctionAfterJumpStatement
#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for View.ViewportSettings.
/// </summary>
public sealed class ViewportSettingsEditor : EditorBase
{
    public ViewportSettingsEditor ()
    {
        Title = "ViewportSettingsEditor";
        TabStop = TabBehavior.TabGroup;

        Initialized += ViewportSettingsEditor_Initialized;
    }

    protected override void OnViewToEditChanged ()
    {
        foreach (View subview in SubViews)
        {
            subview.Enabled = ViewToEdit is { } and not Adornment;
        }

        if (ViewToEdit is null or Adornment)
        {
            return;
        }

        //ViewToEdit.VerticalScrollBar.AutoShow = true;
        //ViewToEdit.HorizontalScrollBar.AutoShow = true;

        _contentSizeWidth!.Value = ViewToEdit.GetContentSize ().Width;
        _contentSizeHeight!.Value = ViewToEdit.GetContentSize ().Height;

        _cbAllowNegativeX!.Value = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeX) ? CheckState.Checked : CheckState.UnChecked;

        _cbAllowNegativeY!.Value = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeY) ? CheckState.Checked : CheckState.UnChecked;

        _cbAllowXGreaterThanContentWidth!.Value = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowXGreaterThanContentWidth)
                                                      ? CheckState.Checked
                                                      : CheckState.UnChecked;

        _cbAllowYGreaterThanContentHeight!.Value = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowYGreaterThanContentHeight)
                                                       ? CheckState.Checked
                                                       : CheckState.UnChecked;

        _cbAllowXPlusWidthGreaterThanContentWidth!.Value =
            ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth) ? CheckState.Checked : CheckState.UnChecked;

        _cbAllowYPlusHeightGreaterThanContentHeight!.Value =
            ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight)
                ? CheckState.Checked
                : CheckState.UnChecked;

        _cbClearContentOnly!.Value = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.ClearContentOnly)
                                         ? CheckState.Checked
                                         : CheckState.UnChecked;

        _cbClipContentOnly!.Value = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.ClipContentOnly) ? CheckState.Checked : CheckState.UnChecked;

        _cbTransparent!.Value = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent) ? CheckState.Checked : CheckState.UnChecked;

        _cbTransparentMouse!.Value = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse)
                                         ? CheckState.Checked
                                         : CheckState.UnChecked;

        _cbVerticalScrollBar!.Value = ViewToEdit.VerticalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked;
        _cbAutoShowVerticalScrollBar!.Value = ViewToEdit.VerticalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked;
        _cbHorizontalScrollBar!.Value = ViewToEdit.HorizontalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked;
        _cbAutoShowHorizontalScrollBar!.Value = ViewToEdit.HorizontalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked;
    }

    private CheckBox? _cbAllowNegativeX;
    private CheckBox? _cbAllowNegativeY;
    private CheckBox? _cbAllowXGreaterThanContentWidth;
    private CheckBox? _cbAllowYGreaterThanContentHeight;
    private CheckBox? _cbAllowXPlusWidthGreaterThanContentWidth;
    private CheckBox? _cbAllowYPlusHeightGreaterThanContentHeight;
    private NumericUpDown<int>? _contentSizeWidth;
    private NumericUpDown<int>? _contentSizeHeight;
    private CheckBox? _cbClearContentOnly;
    private CheckBox? _cbClipContentOnly;
    private CheckBox? _cbTransparent;
    private CheckBox? _cbTransparentMouse;
    private CheckBox? _cbVerticalScrollBar;
    private CheckBox? _cbAutoShowVerticalScrollBar;
    private CheckBox? _cbHorizontalScrollBar;
    private CheckBox? _cbAutoShowHorizontalScrollBar;

    private void ViewportSettingsEditor_Initialized (object? s, EventArgs e)
    {
        _cbAllowNegativeX = new CheckBox { Title = "Allow X < 0", CanFocus = true };

        Add (_cbAllowNegativeX);

        _cbAllowNegativeY = new CheckBox { Title = "Allow Y < 0", CanFocus = true };

        Add (_cbAllowNegativeY);

        _cbAllowXGreaterThanContentWidth = new CheckBox { Title = "Allow X > Content Width", Y = Pos.Bottom (_cbAllowNegativeX), CanFocus = true };

        _cbAllowNegativeX.ValueChanging += AllowNegativeXToggle;
        _cbAllowXGreaterThanContentWidth.ValueChanging += AllowXGreaterThanContentWidthToggle;

        Add (_cbAllowXGreaterThanContentWidth);

        void AllowNegativeXToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowNegativeX;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowNegativeX;
            }
        }

        void AllowXGreaterThanContentWidthToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowXGreaterThanContentWidth;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowXGreaterThanContentWidth;
            }
        }

        _cbAllowYGreaterThanContentHeight = new CheckBox
        {
            Title = "Allow Y > Content Height", X = Pos.Right (_cbAllowXGreaterThanContentWidth) + 1, Y = Pos.Bottom (_cbAllowNegativeX), CanFocus = true
        };

        _cbAllowNegativeY.ValueChanging += AllowNegativeYToggle;

        _cbAllowYGreaterThanContentHeight.ValueChanging += AllowYGreaterThanContentHeightToggle;

        Add (_cbAllowYGreaterThanContentHeight);

        void AllowNegativeYToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowNegativeY;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowNegativeY;
            }
        }

        void AllowYGreaterThanContentHeightToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowYGreaterThanContentHeight;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowYGreaterThanContentHeight;
            }
        }

        _cbAllowXPlusWidthGreaterThanContentWidth = new CheckBox
        {
            Title = "Allow X+Width > Content Width", Y = Pos.Bottom (_cbAllowXGreaterThanContentWidth), CanFocus = true
        };
        _cbAllowXPlusWidthGreaterThanContentWidth.ValueChanging += AllowXPlusWidthGreaterThanContentWidthToggle;

        Add (_cbAllowXPlusWidthGreaterThanContentWidth);

        void AllowXPlusWidthGreaterThanContentWidthToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth;
            }
        }

        _cbAllowYPlusHeightGreaterThanContentHeight = new CheckBox
        {
            Title = "Allow Y+Height > Content Height",
            X = Pos.Left (_cbAllowYGreaterThanContentHeight),
            Y = Pos.Bottom (_cbAllowYGreaterThanContentHeight),
            CanFocus = true
        };
        _cbAllowYPlusHeightGreaterThanContentHeight.ValueChanging += AllowYPlusHeightGreaterThanContentHeightToggle;

        Add (_cbAllowYPlusHeightGreaterThanContentHeight);

        void AllowYPlusHeightGreaterThanContentHeightToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight;
            }
        }

        _cbAllowNegativeY.X = Pos.Left (_cbAllowYGreaterThanContentHeight);

        var labelContentSize = new Label { Title = "ContentSize:", Y = Pos.Bottom (_cbAllowYPlusHeightGreaterThanContentHeight) };

        _contentSizeWidth = new NumericUpDown<int> { X = Pos.Right (labelContentSize) + 1, Y = Pos.Top (labelContentSize), CanFocus = true };
        _contentSizeWidth.ValueChanging += ContentSizeWidthValueChanged;

        void ContentSizeWidthValueChanged (object? sender, ValueChangingEventArgs<int> cea)
        {
            if (cea.NewValue < 0)
            {
                cea.Handled = true;

                return;
            }

            ViewToEdit!.SetContentSize (ViewToEdit.GetContentSize () with { Width = cea.NewValue });
        }

        var labelComma = new Label { Title = ",", X = Pos.Right (_contentSizeWidth), Y = Pos.Top (labelContentSize) };

        _contentSizeHeight = new NumericUpDown<int> { X = Pos.Right (labelComma) + 1, Y = Pos.Top (labelContentSize), CanFocus = true };
        _contentSizeHeight.ValueChanging += ContentSizeHeightValueChanged;

        void ContentSizeHeightValueChanged (object? sender, ValueChangingEventArgs<int> cea)
        {
            if (cea.NewValue < 0)
            {
                cea.Handled = true;

                return;
            }

            ViewToEdit?.SetContentSize (ViewToEdit.GetContentSize () with { Height = cea.NewValue });
        }

        _cbClearContentOnly = new CheckBox { Title = "ClearContentOnly", X = 0, Y = Pos.Bottom (labelContentSize), CanFocus = true };
        _cbClearContentOnly.ValueChanging += ClearContentOnlyToggle;

        void ClearContentOnlyToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.ClearContentOnly;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.ClearContentOnly;
            }
        }

        _cbClipContentOnly = new CheckBox
        {
            Title = "ClipContentOnly", X = Pos.Right (_cbClearContentOnly) + 1, Y = Pos.Bottom (labelContentSize), CanFocus = true
        };
        _cbClipContentOnly.ValueChanging += ClipContentOnlyToggle;

        void ClipContentOnlyToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.ClipContentOnly;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.ClipContentOnly;
            }
        }

        _cbTransparent = new CheckBox { Title = "Transparent", X = Pos.Right (_cbClipContentOnly) + 1, Y = Pos.Bottom (labelContentSize), CanFocus = true };
        _cbTransparent.ValueChanging += TransparentToggle;

        void TransparentToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.Transparent;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.Transparent;
            }
        }

        _cbTransparentMouse = new CheckBox
        {
            Title = "TransparentMouse", X = Pos.Right (_cbTransparent) + 1, Y = Pos.Bottom (labelContentSize), CanFocus = true
        };
        _cbTransparentMouse.ValueChanging += TransparentMouseToggle;

        void TransparentMouseToggle (object? sender, ValueChangingEventArgs<CheckState> rea)
        {
            if (rea.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~ViewportSettingsFlags.TransparentMouse;
            }
        }

        _cbVerticalScrollBar = new CheckBox { Title = "VerticalScrollBar", X = 0, Y = Pos.Bottom (_cbClearContentOnly), CanFocus = false };
        _cbVerticalScrollBar.ValueChanging += VerticalScrollBarToggle;

        void VerticalScrollBarToggle (object? sender, ValueChangingEventArgs<CheckState> rea) =>
            ViewToEdit!.VerticalScrollBar.Visible = rea.NewValue == CheckState.Checked;

        _cbAutoShowVerticalScrollBar = new CheckBox
        {
            Title = "AutoShow", X = Pos.Right (_cbVerticalScrollBar) + 1, Y = Pos.Top (_cbVerticalScrollBar), CanFocus = false
        };
        _cbAutoShowVerticalScrollBar.ValueChanging += AutoShowVerticalScrollBarToggle;

        void AutoShowVerticalScrollBarToggle (object? sender, ValueChangingEventArgs<CheckState> rea) =>
            ViewToEdit!.VerticalScrollBar.AutoShow = rea.NewValue == CheckState.Checked;

        _cbHorizontalScrollBar = new CheckBox { Title = "HorizontalScrollBar", X = 0, Y = Pos.Bottom (_cbVerticalScrollBar), CanFocus = false };
        _cbHorizontalScrollBar.ValueChanging += HorizontalScrollBarToggle;

        void HorizontalScrollBarToggle (object? sender, ValueChangingEventArgs<CheckState> rea) =>
            ViewToEdit!.HorizontalScrollBar.Visible = rea.NewValue == CheckState.Checked;

        _cbAutoShowHorizontalScrollBar = new CheckBox
        {
            Title = "AutoShow ", X = Pos.Right (_cbHorizontalScrollBar) + 1, Y = Pos.Top (_cbHorizontalScrollBar), CanFocus = false
        };
        _cbAutoShowHorizontalScrollBar.ValueChanging += AutoShowHorizontalScrollBarToggle;

        void AutoShowHorizontalScrollBarToggle (object? sender, ValueChangingEventArgs<CheckState> rea) =>
            ViewToEdit!.HorizontalScrollBar.AutoShow = rea.NewValue == CheckState.Checked;

        Add (labelContentSize,
             _contentSizeWidth,
             labelComma,
             _contentSizeHeight,
             _cbClearContentOnly,
             _cbClipContentOnly,
             _cbTransparent,
             _cbTransparentMouse,
             _cbVerticalScrollBar,
             _cbHorizontalScrollBar,
             _cbAutoShowVerticalScrollBar,
             _cbAutoShowHorizontalScrollBar);
    }
}
