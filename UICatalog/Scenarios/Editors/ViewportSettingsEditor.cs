#nullable enable
using System;
using Terminal.Gui;

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

    protected override void OnUpdateSettings ()
    {
        foreach (View subview in Subviews)
        {
            subview.Enabled = ViewToEdit is not Adornment;
        }

        if (ViewToEdit is null)
        { }
    }

    protected override void OnViewToEditChanged ()
    {
        if (ViewToEdit is { } and not Adornment)
        {
            //ViewToEdit.VerticalScrollBar.AutoShow = true;
            //ViewToEdit.HorizontalScrollBar.AutoShow = true;

            _contentSizeWidth!.Value = ViewToEdit.GetContentSize ().Width;
            _contentSizeHeight!.Value = ViewToEdit.GetContentSize ().Height;

            _cbAllowNegativeX!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (Terminal.Gui.ViewportSettings.AllowNegativeX)
                                                  ? CheckState.Checked
                                                  : CheckState.UnChecked;

            _cbAllowNegativeY!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (Terminal.Gui.ViewportSettings.AllowNegativeY)
                                                  ? CheckState.Checked
                                                  : CheckState.UnChecked;

            _cbAllowXGreaterThanContentWidth!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (Terminal.Gui.ViewportSettings.AllowXGreaterThanContentWidth)
                                                                 ? CheckState.Checked
                                                                 : CheckState.UnChecked;

            _cbAllowYGreaterThanContentHeight!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (Terminal.Gui.ViewportSettings.AllowYGreaterThanContentHeight)
                                                                  ? CheckState.Checked
                                                                  : CheckState.UnChecked;

            _cbClearContentOnly!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (Terminal.Gui.ViewportSettings.ClearContentOnly)
                                                    ? CheckState.Checked
                                                    : CheckState.UnChecked;

            _cbClipContentOnly!.CheckedState = ViewToEdit.ViewportSettings.HasFlag (Terminal.Gui.ViewportSettings.ClipContentOnly)
                                                   ? CheckState.Checked
                                                   : CheckState.UnChecked;

            _cbTransparent!.CheckedState = ViewToEdit.ViewportSettings.HasFlag(Terminal.Gui.ViewportSettings.Transparent)
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
    private CheckBox? _cbVerticalScrollBar;
    private CheckBox? _cbAutoShowVerticalScrollBar;
    private CheckBox? _cbHorizontalScrollBar;
    private CheckBox? _cbAutoShowHorizontalScrollBar;

    private void ViewportSettingsEditor_Initialized (object? s, EventArgs e)
    {
        _cbAllowNegativeX = new()
        {
            Title = "Allow X < 0",
            CanFocus = true
        };

        Add (_cbAllowNegativeX);

        _cbAllowNegativeY = new()
        {
            Title = "Allow Y < 0",
            CanFocus = true
        };

        Add (_cbAllowNegativeY);

        _cbAllowXGreaterThanContentWidth = new()
        {
            Title = "Allow X > Content Width",
            Y = Pos.Bottom (_cbAllowNegativeX),
            CanFocus = true
        };

        _cbAllowNegativeX.CheckedStateChanging += AllowNegativeXToggle;
        _cbAllowXGreaterThanContentWidth.CheckedStateChanging += AllowXGreaterThanContentWidthToggle;

        Add (_cbAllowXGreaterThanContentWidth);

        void AllowNegativeXToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowNegativeX;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowNegativeX;
            }
        }

        void AllowXGreaterThanContentWidthToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowXGreaterThanContentWidth;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowXGreaterThanContentWidth;
            }
        }

        _cbAllowYGreaterThanContentHeight = new()
        {
            Title = "Allow Y > Content Height",
            X = Pos.Right (_cbAllowXGreaterThanContentWidth) + 1,
            Y = Pos.Bottom (_cbAllowNegativeX),
            CanFocus = true
        };

        _cbAllowNegativeY.CheckedStateChanging += AllowNegativeYToggle;

        _cbAllowYGreaterThanContentHeight.CheckedStateChanging += AllowYGreaterThanContentHeightToggle;

        Add (_cbAllowYGreaterThanContentHeight);

        void AllowNegativeYToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowNegativeY;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowNegativeY;
            }
        }

        void AllowYGreaterThanContentHeightToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowYGreaterThanContentHeight;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowYGreaterThanContentHeight;
            }
        }

        _cbAllowNegativeY.X = Pos.Left (_cbAllowYGreaterThanContentHeight);

        var labelContentSize = new Label
        {
            Title = "ContentSize:",
            Y = Pos.Bottom (_cbAllowYGreaterThanContentHeight)
        };

        _contentSizeWidth = new()
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

        _contentSizeHeight = new()
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

        _cbClearContentOnly = new()
        {
            Title = "ClearContentOnly",
            X = 0,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = true
        };
        _cbClearContentOnly.CheckedStateChanging += ClearContentOnlyToggle;

        void ClearContentOnlyToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewportSettings.ClearContentOnly;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewportSettings.ClearContentOnly;
            }
        }

        _cbClipContentOnly = new()
        {
            Title = "ClipContentOnly",
            X = Pos.Right (_cbClearContentOnly) + 1,
            Y = Pos.Bottom (labelContentSize),
            CanFocus = true
        };
        _cbClipContentOnly.CheckedStateChanging += ClipContentOnlyToggle;

        void ClipContentOnlyToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewportSettings.ClipContentOnly;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewportSettings.ClipContentOnly;
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

        void TransparentToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                ViewToEdit!.ViewportSettings |= Terminal.Gui.ViewportSettings.Transparent;
            }
            else
            {
                ViewToEdit!.ViewportSettings &= ~Terminal.Gui.ViewportSettings.Transparent;
            }
        }

        _cbVerticalScrollBar = new()
        {
            Title = "VerticalScrollBar",
            X = 0,
            Y = Pos.Bottom (_cbClearContentOnly),
            CanFocus = false
        };
        _cbVerticalScrollBar.CheckedStateChanging += VerticalScrollBarToggle;

        void VerticalScrollBarToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            ViewToEdit!.VerticalScrollBar.Visible = e.NewValue == CheckState.Checked;
        }

        _cbAutoShowVerticalScrollBar = new()
        {
            Title = "AutoShow",
            X = Pos.Right (_cbVerticalScrollBar) + 1,
            Y = Pos.Top (_cbVerticalScrollBar),
            CanFocus = false
        };
        _cbAutoShowVerticalScrollBar.CheckedStateChanging += AutoShowVerticalScrollBarToggle;

        void AutoShowVerticalScrollBarToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            ViewToEdit!.VerticalScrollBar.AutoShow = e.NewValue == CheckState.Checked;
        }

        _cbHorizontalScrollBar = new()
        {
            Title = "HorizontalScrollBar",
            X = 0,
            Y = Pos.Bottom (_cbVerticalScrollBar),
            CanFocus = false
        };
        _cbHorizontalScrollBar.CheckedStateChanging += HorizontalScrollBarToggle;

        void HorizontalScrollBarToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            ViewToEdit!.HorizontalScrollBar.Visible = e.NewValue == CheckState.Checked;
        }

        _cbAutoShowHorizontalScrollBar = new()
        {
            Title = "AutoShow ",
            X = Pos.Right (_cbHorizontalScrollBar) + 1,
            Y = Pos.Top (_cbHorizontalScrollBar),
            CanFocus = false
        };
        _cbAutoShowHorizontalScrollBar.CheckedStateChanging += AutoShowHorizontalScrollBarToggle;

        void AutoShowHorizontalScrollBarToggle (object? sender, CancelEventArgs<CheckState> e)
        {
            ViewToEdit!.HorizontalScrollBar.AutoShow = e.NewValue == CheckState.Checked;
        }

        Add (
             labelContentSize,
             _contentSizeWidth,
             labelComma,
             _contentSizeHeight,
             _cbClearContentOnly,
             _cbClipContentOnly,
             _cbTransparent,
             _cbVerticalScrollBar,
             _cbHorizontalScrollBar,
             _cbAutoShowVerticalScrollBar,
             _cbAutoShowHorizontalScrollBar);
    }
}
