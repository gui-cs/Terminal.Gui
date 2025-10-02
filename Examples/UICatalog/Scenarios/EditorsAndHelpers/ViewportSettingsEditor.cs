#nullable enable
using System;

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
            subview.Enabled = ViewToEdit is not Adornment;
        }

        if (ViewToEdit is { } and not Adornment)
        {
            //ViewToEdit.VerticalScrollBar.AutoShow = true;
            //ViewToEdit.HorizontalScrollBar.AutoShow = true;

            _contentSizeWidth!.Value = ViewToEdit.GetContentSize ().Width;
            _contentSizeHeight!.Value = ViewToEdit.GetContentSize ().Height;

            _cbAllowNegativeX!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeX)
                                                  ? CheckState.Checked
                                                  : CheckState.UnChecked;

            _cbAllowNegativeY!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeY)
                                                  ? CheckState.Checked
                                                  : CheckState.UnChecked;

            _cbAllowXGreaterThanContentWidth!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowXGreaterThanContentWidth)
                                                                 ? CheckState.Checked
                                                                 : CheckState.UnChecked;

            _cbAllowYGreaterThanContentHeight!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.AllowYGreaterThanContentHeight)
                                                                  ? CheckState.Checked
                                                                  : CheckState.UnChecked;

            _cbClearContentOnly!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.ClearContentOnly)
                                                    ? CheckState.Checked
                                                    : CheckState.UnChecked;

            _cbClipContentOnly!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.ClipContentOnly)
                                                   ? CheckState.Checked
                                                   : CheckState.UnChecked;

            _cbTransparent!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent)
                                               ? CheckState.Checked
                                               : CheckState.UnChecked;

            _cbTransparentMouse!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse)
                                               ? CheckState.Checked
                                               : CheckState.UnChecked;

            _cbVerticalScrollBar!.CheckedState = ViewToEdit.VerticalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked;
            _cbAutoShowVerticalScrollBar!.CheckedState = ViewToEdit.VerticalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked;
            _cbHorizontalScrollBar!.CheckedState = ViewToEdit.HorizontalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked;
            _cbAutoShowHorizontalScrollBar!.CheckedState = ViewToEdit.HorizontalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked;
        }
    }

    private CheckBox? _cbAllowNegativeX;
    private CheckBox? _cbAllowNegativeY;
    private CheckBox? _cbAllowXGreaterThanContentWidth;
    private CheckBox? _cbAllowYGreaterThanContentHeight;
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
        _cbAllowNegativeX = new ()
        {
            Title = "Allow X < 0",
            CanFocus = true
        };

        Add (_cbAllowNegativeX);

        _cbAllowNegativeY = new ()
        {
            Title = "Allow Y < 0",
            CanFocus = true
        };

        Add (_cbAllowNegativeY);

        _cbAllowXGreaterThanContentWidth = new ()
        {
            Title = "Allow X > Content Width",
            Y = Pos.Bottom (_cbAllowNegativeX),
            CanFocus = true
        };

        _cbAllowNegativeX.CheckedStateChanging += AllowNegativeXToggle;
        _cbAllowXGreaterThanContentWidth.CheckedStateChanging += AllowXGreaterThanContentWidthToggle;

        Add (_cbAllowXGreaterThanContentWidth);

        void AllowNegativeXToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            if (e.Result == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.AllowNegativeX;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewBase.ViewportSettingsFlags.AllowNegativeX;
            }
        }

        void AllowXGreaterThanContentWidthToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            if (e.Result == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.AllowXGreaterThanContentWidth;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewBase.ViewportSettingsFlags.AllowXGreaterThanContentWidth;
            }
        }

        _cbAllowYGreaterThanContentHeight = new ()
        {
            Title = "Allow Y > Content Height",
            X = Pos.Right (_cbAllowXGreaterThanContentWidth) + 1,
            Y = Pos.Bottom (_cbAllowNegativeX),
            CanFocus = true
        };

        _cbAllowNegativeY.CheckedStateChanging += AllowNegativeYToggle;

        _cbAllowYGreaterThanContentHeight.CheckedStateChanging += AllowYGreaterThanContentHeightToggle;

        Add (_cbAllowYGreaterThanContentHeight);

        void AllowNegativeYToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            if (e.Result == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.AllowNegativeY;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewBase.ViewportSettingsFlags.AllowNegativeY;
            }
        }

        void AllowYGreaterThanContentHeightToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            if (e.Result == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.AllowYGreaterThanContentHeight;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewBase.ViewportSettingsFlags.AllowYGreaterThanContentHeight;
            }
        }

        _cbAllowNegativeY.X = Pos.Left (_cbAllowYGreaterThanContentHeight);

        var labelContentSize = new Label
        {
            Title = "ContentSize:",
            Y = Pos.Bottom (_cbAllowYGreaterThanContentHeight)
        };

        _contentSizeWidth = new ()
        {
            X = Pos.Right (labelContentSize) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = true
        };
        _contentSizeWidth.ValueChanging += ContentSizeWidthValueChanged;

        void ContentSizeWidthValueChanged (object? sender, CancelEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }

            // BUGBUG: set_ContentSize is supposed to be `protected`. 
            ViewToEdit!.SetContentSize (ViewToEdit.GetContentSize () with { Width = e.NewValue });
        }

        var labelComma = new Label
        {
            Title = ",",
            X = Pos.Right (_contentSizeWidth),
            Y = Pos.Top (labelContentSize)
        };

        _contentSizeHeight = new ()
        {
            X = Pos.Right (labelComma) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = true
        };
        _contentSizeHeight.ValueChanging += ContentSizeHeightValueChanged;

        void ContentSizeHeightValueChanged (object? sender, CancelEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }

            // BUGBUG: set_ContentSize is supposed to be `protected`. 
            ViewToEdit?.SetContentSize (ViewToEdit.GetContentSize () with { Height = e.NewValue });
        }

        _cbClearContentOnly = new ()
        {
            Title = "ClearContentOnly",
            X = 0,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = true
        };
        _cbClearContentOnly.CheckedStateChanging += ClearContentOnlyToggle;

        void ClearContentOnlyToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            if (e.Result == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.ClearContentOnly;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewBase.ViewportSettingsFlags.ClearContentOnly;
            }
        }

        _cbClipContentOnly = new ()
        {
            Title = "ClipContentOnly",
            X = Pos.Right (_cbClearContentOnly) + 1,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = true
        };
        _cbClipContentOnly.CheckedStateChanging += ClipContentOnlyToggle;

        void ClipContentOnlyToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            if (e.Result == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.ClipContentOnly;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewBase.ViewportSettingsFlags.ClipContentOnly;
            }
        }

        _cbTransparent = new ()
        {
            Title = "Transparent",
            X = Pos.Right (_cbClipContentOnly) + 1,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = true
        };
        _cbTransparent.CheckedStateChanging += TransparentToggle;

        void TransparentToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            if (e.Result == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.Transparent;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewBase.ViewportSettingsFlags.Transparent;
            }
        }

        _cbTransparentMouse = new ()
        {
            Title = "TransparentMouse",
            X = Pos.Right (_cbTransparent) + 1,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = true
        };
        _cbTransparentMouse.CheckedStateChanging += TransparentMouseToggle;

        void TransparentMouseToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            if (e.Result == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewBase.ViewportSettingsFlags.TransparentMouse;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewBase.ViewportSettingsFlags.TransparentMouse;
            }
        }

        _cbVerticalScrollBar = new ()
        {
            Title = "VerticalScrollBar",
            X = 0,
            Y = Pos.Bottom (_cbClearContentOnly),
            CanFocus = false
        };
        _cbVerticalScrollBar.CheckedStateChanging += VerticalScrollBarToggle;

        void VerticalScrollBarToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            ViewToEdit!.VerticalScrollBar.Visible = e.Result == CheckState.Checked;
        }

        _cbAutoShowVerticalScrollBar = new ()
        {
            Title = "AutoShow",
            X = Pos.Right (_cbVerticalScrollBar) + 1,
            Y = Pos.Top (_cbVerticalScrollBar),
            CanFocus = false
        };
        _cbAutoShowVerticalScrollBar.CheckedStateChanging += AutoShowVerticalScrollBarToggle;

        void AutoShowVerticalScrollBarToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            ViewToEdit!.VerticalScrollBar.AutoShow = e.Result == CheckState.Checked;
        }

        _cbHorizontalScrollBar = new ()
        {
            Title = "HorizontalScrollBar",
            X = 0,
            Y = Pos.Bottom (_cbVerticalScrollBar),
            CanFocus = false
        };
        _cbHorizontalScrollBar.CheckedStateChanging += HorizontalScrollBarToggle;

        void HorizontalScrollBarToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            ViewToEdit!.HorizontalScrollBar.Visible = e.Result == CheckState.Checked;
        }

        _cbAutoShowHorizontalScrollBar = new ()
        {
            Title = "AutoShow ",
            X = Pos.Right (_cbHorizontalScrollBar) + 1,
            Y = Pos.Top (_cbHorizontalScrollBar),
            CanFocus = false
        };
        _cbAutoShowHorizontalScrollBar.CheckedStateChanging += AutoShowHorizontalScrollBarToggle;

        void AutoShowHorizontalScrollBarToggle (object? sender, ResultEventArgs<CheckState> e)
        {
            ViewToEdit!.HorizontalScrollBar.AutoShow = e.Result == CheckState.Checked;
        }

        Add (
             labelContentSize,
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
