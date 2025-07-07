#nullable enable
using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

// Border Arrange Mode

public partial class Border
{
    internal ViewArrangement Arranging { get; set; }

    private Button? _moveButton; // always top-left
    private Button? _allSizeButton;
    private Button? _leftSizeButton;
    private Button? _rightSizeButton;
    private Button? _topSizeButton;
    private Button? _bottomSizeButton;

    /// <summary>
    ///     Starts "Arrange Mode" where <see cref="Adornment.Parent"/> can be moved and/or resized using the mouse
    ///     or keyboard. If <paramref name="arrangement"/> is <see cref="ViewArrangement.Fixed"/> keyboard mode is enabled.
    /// </summary>
    /// <remarks>
    ///     Arrange Mode is exited by the user pressing <see cref="Application.ArrangeKey"/>, <see cref="Key.Esc"/>, or by
    ///     clicking the mouse out of the <see cref="Adornment.Parent"/>'s Frame.
    /// </remarks>
    /// <returns></returns>
    public bool? EnterArrangeMode (ViewArrangement arrangement)
    {
        Debug.Assert (Arranging == ViewArrangement.Fixed);

        if (!HasAnyArrangementOptions ())
        {
            return false;
        }

        MouseState |= MouseState.Pressed;

        // Add Commands and KeyBindings - Note it's ok these get added each time. KeyBindings are cleared in EndArrange()
        AddArrangeModeKeyBindings ();

        Application.MouseEvent += ApplicationOnMouseEvent;

        // Create all necessary arrangement buttons
        CreateArrangementButtons ();

        if (arrangement == ViewArrangement.Fixed)
        {
            // Keyboard mode
            SetVisibilityForKeyboardMode ();
            Arranging = ViewArrangement.Movable;
            CanFocus = true;
            SetFocus ();
        }
        else
        {
            // Mouse mode
            Arranging = arrangement;
            SetVisibilityForMouseMode (arrangement);
        }

        if (Arranging != ViewArrangement.Fixed)
        {
            if (arrangement == ViewArrangement.Fixed)
            {
                // Keyboard mode - enable nav
                // TODO: Keyboard mode only supports sizing from bottom/right.
                Arranging = (ViewArrangement)(Focused?.Data ?? ViewArrangement.Fixed);
            }

            return true;
        }

        // Hack for now
        EndArrangeMode ();

        return false;
    }

    /// <summary>
    /// Checks if the parent view has any arrangement options enabled
    /// </summary>
    private bool HasAnyArrangementOptions ()
    {
        return Parent!.Arrangement.HasFlag (ViewArrangement.Movable)
            || Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable)
            || Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable)
            || Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable)
            || Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable);
    }

    /// <summary>
    /// Creates all the buttons required for the arrange mode based on allowed arrangement options
    /// </summary>
    private void CreateArrangementButtons ()
    {
        if (Parent!.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            _moveButton = CreateArrangementButton ("moveButton", Glyphs.Move, 0, 0, ViewArrangement.Movable);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.Resizable))
        {
            _allSizeButton = CreateArrangementButton (
                "allSizeButton",
                Glyphs.SizeBottomRight,
                Pos.AnchorEnd (),
                Pos.AnchorEnd (),
                ViewArrangement.Resizable);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable))
        {
            _topSizeButton = CreateArrangementButton (
                "topSizeButton",
                Glyphs.SizeVertical,
                Pos.Center () + Parent!.Margin!.Thickness.Horizontal,
                0,
                ViewArrangement.TopResizable);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable))
        {
            _rightSizeButton = CreateArrangementButton (
                "rightSizeButton",
                Glyphs.SizeHorizontal,
                Pos.AnchorEnd (),
                Pos.Center () + Parent!.Margin!.Thickness.Vertical / 2,
                ViewArrangement.RightResizable);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            _leftSizeButton = CreateArrangementButton (
                "leftSizeButton",
                Glyphs.SizeHorizontal,
                0,
                Pos.Center () + Parent!.Margin!.Thickness.Vertical / 2,
                ViewArrangement.LeftResizable);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable))
        {
            _bottomSizeButton = CreateArrangementButton (
                "bottomSizeButton",
                Glyphs.SizeVertical,
                Pos.Center () + Parent!.Margin!.Thickness.Horizontal / 2,
                Pos.AnchorEnd (),
                ViewArrangement.BottomResizable);
        }
    }

    /// <summary>
    /// Factory method to create a standardized arrangement button
    /// </summary>
    private Button CreateArrangementButton (string id, Rune glyph, Pos x, Pos y, ViewArrangement arrangement)
    {
        var button = new Button
        {
            Id = id,
            CanFocus = true,
            Width = 1,
            Height = 1,
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyle.None,
            Text = $"{glyph}",
            X = x,
            Y = y,
            Visible = false,
            Data = arrangement
        };

        Add (button);
        return button;
    }

    /// <summary>
    /// Sets button visibility for keyboard arrangement mode
    /// </summary>
    private void SetVisibilityForKeyboardMode ()
    {
        if (_moveButton != null)
        {
            _moveButton.Visible = true;
        }

        if (_allSizeButton != null)
        {
            _allSizeButton.Visible = true;
        }
    }

    /// <summary>
    /// Sets button visibility based on the specified mouse arrangement mode
    /// </summary>
    private void SetVisibilityForMouseMode (ViewArrangement arrangement)
    {
        switch (arrangement)
        {
            case ViewArrangement.Movable:
                SetVisibleButton (_moveButton);
                break;

            case ViewArrangement.RightResizable | ViewArrangement.BottomResizable:
            case ViewArrangement.Resizable:
                SetVisibleButton (_rightSizeButton);
                SetVisibleButton (_bottomSizeButton);
                if (_allSizeButton != null)
                {
                    _allSizeButton.X = Pos.AnchorEnd ();
                    _allSizeButton.Y = Pos.AnchorEnd ();
                    _allSizeButton.Visible = true;
                }
                break;

            case ViewArrangement.LeftResizable:
                SetVisibleButton (_leftSizeButton);
                break;

            case ViewArrangement.RightResizable:
                SetVisibleButton (_rightSizeButton);
                break;

            case ViewArrangement.TopResizable:
                SetVisibleButton (_topSizeButton);
                break;

            case ViewArrangement.BottomResizable:
                SetVisibleButton (_bottomSizeButton);
                break;

            case ViewArrangement.LeftResizable | ViewArrangement.BottomResizable:
                SetVisibleButton (_leftSizeButton);
                SetVisibleButton (_bottomSizeButton);
                if (_allSizeButton != null)
                {
                    _allSizeButton.X = 0;
                    _allSizeButton.Y = Pos.AnchorEnd ();
                    _allSizeButton.Visible = true;
                }
                break;

            case ViewArrangement.LeftResizable | ViewArrangement.TopResizable:
                SetVisibleButton (_leftSizeButton);
                SetVisibleButton (_topSizeButton);
                break;

            case ViewArrangement.RightResizable | ViewArrangement.TopResizable:
                SetVisibleButton (_rightSizeButton);
                SetVisibleButton (_topSizeButton);
                if (_allSizeButton != null)
                {
                    _allSizeButton.X = Pos.AnchorEnd ();
                    _allSizeButton.Y = 0;
                    _allSizeButton.Visible = true;
                }
                break;
        }
    }

    /// <summary>
    /// Helper method to make a button visible if it's not null
    /// </summary>
    private void SetVisibleButton (Button? button)
    {
        if (button != null)
        {
            button.Visible = true;
        }
    }

    private void AddArrangeModeKeyBindings ()
    {
        AddCommand (Command.Quit, EndArrangeMode);

        AddCommand (
                    Command.Up,
                    () =>
                    {
                        if (Parent is null)
                        {
                            return false;
                        }

                        if (Arranging == ViewArrangement.Movable)
                        {
                            Parent.Y = Parent.Y - 1;
                        }

                        if (Arranging == ViewArrangement.Resizable)
                        {
                            if (Parent.Viewport.Height > 0)
                            {
                                Parent.Height = Parent.Height! - 1;
                            }
                        }

                        return true;
                    });

        AddCommand (
                    Command.Down,
                    () =>
                    {
                        if (Parent is null)
                        {
                            return false;
                        }

                        if (Arranging == ViewArrangement.Movable)
                        {
                            Parent.Y = Parent.Y + 1;
                        }

                        if (Arranging == ViewArrangement.Resizable)
                        {
                            Parent.Height = Parent.Height! + 1;
                        }

                        return true;
                    });

        AddCommand (
                    Command.Left,
                    () =>
                    {
                        if (Parent is null)
                        {
                            return false;
                        }

                        if (Arranging == ViewArrangement.Movable)
                        {
                            Parent.X = Parent.X - 1;
                        }

                        if (Arranging == ViewArrangement.Resizable)
                        {
                            if (Parent.Viewport.Width > 0)
                            {
                                Parent.Width = Parent.Width! - 1;
                            }
                        }

                        return true;
                    });

        AddCommand (
                    Command.Right,
                    () =>
                    {
                        if (Parent is null)
                        {
                            return false;
                        }

                        if (Arranging == ViewArrangement.Movable)
                        {
                            Parent.X = Parent.X + 1;
                        }

                        if (Arranging == ViewArrangement.Resizable)
                        {
                            Parent.Width = Parent.Width! + 1;
                        }

                        return true;
                    });

        AddCommand (
                    Command.Tab,
                    () =>
                    {
                        // BUGBUG: If an arrangeable view has only arrangeable subviews, it's not possible to activate
                        // BUGBUG: ArrangeMode with keyboard for the superview.
                        // BUGBUG: AdvanceFocus should be wise to this and when in ArrangeMode, should move across
                        // BUGBUG: the view hierarchy.

                        AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
                        Arranging = (ViewArrangement)(Focused?.Data ?? ViewArrangement.Fixed);

                        return true; // Always eat
                    });

        AddCommand (
                    Command.BackTab,
                    () =>
                    {
                        AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
                        Arranging = (ViewArrangement)(Focused?.Data ?? ViewArrangement.Fixed);

                        return true; // Always eat
                    });

        HotKeyBindings.Add (Key.Esc, Command.Quit);
        HotKeyBindings.Add (Application.ArrangeKey, Command.Quit);
        HotKeyBindings.Add (Key.CursorUp, Command.Up);
        HotKeyBindings.Add (Key.CursorDown, Command.Down);
        HotKeyBindings.Add (Key.CursorLeft, Command.Left);
        HotKeyBindings.Add (Key.CursorRight, Command.Right);

        HotKeyBindings.Add (Key.Tab, Command.Tab);
        HotKeyBindings.Add (Key.Tab.WithShift, Command.BackTab);
    }

    private void ApplicationOnMouseEvent (object? sender, MouseEventArgs e)
    {
        if (e.Flags != MouseFlags.Button1Clicked)
        {
            return;
        }

        // If mouse click is outside of Border.Thickness then exit Arrange Mode
        // e.Position is screen relative
        Point framePos = ScreenToFrame (e.ScreenPosition);

        if (!Thickness.Contains (Frame, framePos))
        {
            EndArrangeMode ();
        }
    }

    private bool? EndArrangeMode ()
    {
        // Debug.Assert (_arranging != ViewArrangement.Fixed);
        Arranging = ViewArrangement.Fixed;

        MouseState &= ~MouseState.Pressed;

        Application.MouseEvent -= ApplicationOnMouseEvent;

        if (Application.MouseGrabHandler.MouseGrabView == this && _dragPosition.HasValue)
        {
            Application.MouseGrabHandler.UngrabMouse ();
        }

        // Clean up all arrangement buttons
        DisposeSizeButton (ref _moveButton);
        DisposeSizeButton (ref _allSizeButton);
        DisposeSizeButton (ref _leftSizeButton);
        DisposeSizeButton (ref _rightSizeButton);
        DisposeSizeButton (ref _topSizeButton);
        DisposeSizeButton (ref _bottomSizeButton);

        HotKeyBindings.Clear ();

        if (CanFocus)
        {
            CanFocus = false;
        }

        return true;
    }

    /// <summary>
    /// Helper method to dispose and remove a button
    /// </summary>
    private void DisposeSizeButton (ref Button? button)
    {
        if (button != null)
        {
            Remove (button);
            button.Dispose ();
            button = null;
        }
    }


    #region Mouse Support

    private Point? _dragPosition;
    private Point _startGrabPoint;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/3312
        if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            Parent!.SetFocus ();

            if (!HasAnyArrangementOptions ())
            {
                return false;
            }

            // Only start grabbing if the user clicks in the Thickness area
            // Adornment.Contains takes Parent SuperView=relative coords.
            if (Contains (new (mouseEvent.Position.X + Parent.Frame.X + Frame.X, mouseEvent.Position.Y + Parent.Frame.Y + Frame.Y)))
            {
                if (Arranging != ViewArrangement.Fixed)
                {
                    EndArrangeMode ();
                }

                // Set the start grab point to the Frame coords
                _startGrabPoint = new (mouseEvent.Position.X + Frame.X, mouseEvent.Position.Y + Frame.Y);
                _dragPosition = mouseEvent.Position;
                Application.MouseGrabHandler.GrabMouse (this);

                // Determine the mode based on where the click occurred
                ViewArrangement arrangeMode = DetermineArrangeModeFromClick ();
                EnterArrangeMode (arrangeMode);

                // BUGBUG: Should we return the result of EnterArrangeMode?
                return true;
            }

            return true;
        }

        if (mouseEvent.Flags is (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && Application.MouseGrabHandler.MouseGrabView == this)
        {
            if (_dragPosition.HasValue)
            {
                HandleDragOperation (mouseEvent);
                return true;
            }
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && _dragPosition.HasValue)
        {
            _dragPosition = null;
            Application.MouseGrabHandler.UngrabMouse ();

            EndArrangeMode ();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines the arrangement mode based on where the mouse was clicked
    /// </summary>
    internal ViewArrangement DetermineArrangeModeFromClick ()
    {
        Rectangle sideRect;

        // Check for left resizable region
        if (Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            sideRect = new (Frame.X, Frame.Y + Thickness.Top, Thickness.Left, Frame.Height - Thickness.Top - Thickness.Bottom);
            if (sideRect.Contains (_startGrabPoint))
            {
                return ViewArrangement.LeftResizable;
            }
        }

        // Check for right resizable region
        if (Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable))
        {
            sideRect = new (
                Frame.X + Frame.Width - Thickness.Right,
                Frame.Y + Thickness.Top,
                Thickness.Right,
                Frame.Height - Thickness.Top - Thickness.Bottom);

            if (sideRect.Contains (_startGrabPoint))
            {
                return (ViewArrangement.RightResizable);
            }
        }

        // Check for top resizable region (only if not movable)
        if (Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable) && !Parent!.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            sideRect = new (Frame.X + Thickness.Left, Frame.Y, Frame.Width - Thickness.Left - Thickness.Right, Thickness.Top);

            if (sideRect.Contains (_startGrabPoint))
            {
                return (ViewArrangement.TopResizable);
            }
        }

        // Check for bottom resizable region
        if (Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable))
        {
            sideRect = new (
                Frame.X + Thickness.Left,
                Frame.Y + Frame.Height - Thickness.Bottom,
                Frame.Width - Thickness.Left - Thickness.Right,
                Thickness.Bottom);

            if (sideRect.Contains (_startGrabPoint))
            {
                return (ViewArrangement.BottomResizable);
            }
        }

        // Check for bottom-left corner region
        if (Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable) && Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            sideRect = new (Frame.X, Frame.Height - Thickness.Top, Thickness.Left, Thickness.Bottom);

            if (sideRect.Contains (_startGrabPoint))
            {
                return (ViewArrangement.BottomResizable | ViewArrangement.LeftResizable);
            }
        }

        // Check for bottom-right corner region
        if (Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable) && Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable))
        {
            sideRect = new (Frame.X + Frame.Width - Thickness.Right, Frame.Height - Thickness.Top, Thickness.Right, Thickness.Bottom);

            if (sideRect.Contains (_startGrabPoint))
            {
                return (ViewArrangement.BottomResizable | ViewArrangement.RightResizable);
            }
        }

        // Check for top-right corner region
        if (Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable) && Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable))
        {
            sideRect = new (Frame.X + Frame.Width - Thickness.Right, Frame.Y, Thickness.Right, Thickness.Top);

            if (sideRect.Contains (_startGrabPoint))
            {
                return (ViewArrangement.TopResizable | ViewArrangement.RightResizable);
            }
        }

        // Check for top-left corner region
        if (Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable) && Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            sideRect = new (Frame.X, Frame.Y, Thickness.Left, Thickness.Top);

            if (sideRect.Contains (_startGrabPoint))
            {
                return (ViewArrangement.TopResizable | ViewArrangement.LeftResizable);
            }
        }

        // Default to movable if enabled
        if (Parent!.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            return ViewArrangement.Movable;
        }

        return ViewArrangement.Fixed;
    }

    /// <summary>
    /// Handles drag operations for moving and resizing
    /// </summary>
    internal void HandleDragOperation (MouseEventArgs mouseEvent)
    {
        if (Parent!.SuperView is null)
        {
            // Redraw the entire app window.
            Application.Top!.SetNeedsDraw ();
        }
        else
        {
            Parent.SuperView.SetNeedsDraw ();
        }

        _dragPosition = mouseEvent.Position;

        Point parentLoc = Parent!.SuperView?.ScreenToViewport (new (mouseEvent.ScreenPosition.X, mouseEvent.ScreenPosition.Y))
                          ?? mouseEvent.ScreenPosition;

        int minHeight = Thickness.Vertical + Parent!.Margin!.Thickness.Bottom;
        int minWidth = Thickness.Horizontal + Parent!.Margin!.Thickness.Right;

        switch (Arranging)
        {
            case ViewArrangement.Movable:
                Parent.X = parentLoc.X - _startGrabPoint.X;
                Parent.Y = parentLoc.Y - _startGrabPoint.Y;
                break;

            case ViewArrangement.TopResizable:
                // Get how much the mouse has moved since the start of the drag
                // and adjust the height of the parent by that amount
                int deltaY = parentLoc.Y - Parent.Frame.Y;
                int newHeight = Math.Max (minHeight, Parent.Frame.Height - deltaY);

                if (newHeight != Parent.Frame.Height)
                {
                    Parent.Height = newHeight;
                    Parent.Y = parentLoc.Y - _startGrabPoint.Y;
                }
                break;

            case ViewArrangement.BottomResizable:
                Parent.Height = Math.Max (minHeight, parentLoc.Y - Parent.Frame.Y + Parent!.Margin.Thickness.Bottom + 1);
                break;

            case ViewArrangement.LeftResizable:
                // Get how much the mouse has moved since the start of the drag
                // and adjust the width of the parent by that amount
                int deltaX = parentLoc.X - Parent.Frame.X;
                int newWidth = Math.Max (minWidth, Parent.Frame.Width - deltaX);

                if (newWidth != Parent.Frame.Width)
                {
                    Parent.Width = newWidth;
                    Parent.X = parentLoc.X - _startGrabPoint.X;
                }
                break;

            case ViewArrangement.RightResizable:
                Parent.Width = Math.Max (minWidth, parentLoc.X - Parent.Frame.X + Parent!.Margin.Thickness.Right + 1);
                break;

            case ViewArrangement.BottomResizable | ViewArrangement.RightResizable:
                Parent.Width = Math.Max (minWidth, parentLoc.X - Parent.Frame.X + Parent!.Margin.Thickness.Right + 1);
                Parent.Height = Math.Max (minHeight, parentLoc.Y - Parent.Frame.Y + Parent!.Margin.Thickness.Bottom + 1);
                break;

            case ViewArrangement.BottomResizable | ViewArrangement.LeftResizable:
                int dX = parentLoc.X - Parent.Frame.X;
                int newW = Math.Max (minWidth, Parent.Frame.Width - dX);

                if (newW != Parent.Frame.Width)
                {
                    Parent.Width = newW;
                    Parent.X = parentLoc.X - _startGrabPoint.X;
                }

                Parent.Height = Math.Max (minHeight, parentLoc.Y - Parent.Frame.Y + Parent!.Margin.Thickness.Bottom + 1);
                break;

            case ViewArrangement.TopResizable | ViewArrangement.RightResizable:
                int dY = parentLoc.Y - Parent.Frame.Y;
                int newH = Math.Max (minHeight, Parent.Frame.Height - dY);

                if (newH != Parent.Frame.Height)
                {
                    Parent.Height = newH;
                    Parent.Y = parentLoc.Y - _startGrabPoint.Y;
                }

                Parent.Width = Math.Max (minWidth, parentLoc.X - Parent.Frame.X + Parent!.Margin.Thickness.Right + 1);
                break;

            case ViewArrangement.TopResizable | ViewArrangement.LeftResizable:
                int dY2 = parentLoc.Y - Parent.Frame.Y;
                int newH2 = Math.Max (minHeight, Parent.Frame.Height - dY2);

                if (newH2 != Parent.Frame.Height)
                {
                    Parent.Height = newH2;
                    Parent.Y = parentLoc.Y - _startGrabPoint.Y;
                }

                int dX2 = parentLoc.X - Parent.Frame.X;
                int newW2 = Math.Max (minWidth, Parent.Frame.Width - dX2);

                if (newW2 != Parent.Frame.Width)
                {
                    Parent.Width = newW2;
                    Parent.X = parentLoc.X - _startGrabPoint.X;
                }
                break;
        }
    }

    private void Application_GrabbingMouse (object? sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabHandler.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }

    private void Application_UnGrabbingMouse (object? sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabHandler.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }

    #endregion Mouse Support



    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Application.MouseGrabHandler.GrabbingMouse -= Application_GrabbingMouse;
        Application.MouseGrabHandler.UnGrabbingMouse -= Application_UnGrabbingMouse;

        _dragPosition = null;
        base.Dispose (disposing);
    }
}
