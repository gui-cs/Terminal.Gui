namespace Terminal.Gui.ViewBase;

/// <summary>
///     Manages the arrangement (move/resize) functionality for a view through its border.
///     This class handles both keyboard and mouse-based arrangement operations.
/// </summary>
internal sealed class Arranger : IDisposable
{
    // TODO: Simplify this by having _border be of type Border (IAdornment)
    private readonly BorderView _border;

    /// <summary>
    ///     Creates a new Arranger for the specified border.
    /// </summary>
    /// <param name="border">The border adornment to manage arrangement for.</param>
    internal Arranger (BorderView border) => _border = border;

    /// <summary>
    ///     Gets the current arrangement mode.
    /// </summary>
    internal ViewArrangement Arranging { get; private set; }

    /// <summary>
    ///     Gets whether arrangement mode is active.
    /// </summary>
    internal bool IsArranging => Arranging != ViewArrangement.Fixed;

    private Point? _dragPosition;

    /// <summary>
    ///     Gets whether a mouse drag operation is in progress.
    /// </summary>
    internal bool IsDragging => _dragPosition.HasValue;

    /// <summary>
    ///     Starts "Arrange Mode" where <see cref="IAdornment.Parent"/> of a <see cref="Border"/> can be moved and/or resized
    ///     using the mouse
    ///     or keyboard.
    /// </summary>
    /// <remarks>
    ///     Arrange Mode is exited by the user pressing the Arrange key, <see cref="Key.Esc"/>, or by
    ///     clicking the mouse out of the <see cref="IAdornment.Parent"/>'s Frame.
    /// </remarks>
    /// <returns></returns>
    internal bool EnterArrangeMode (ViewArrangement arrangement)
    {
        if (!HasAnyArrangementOptions ())
        {
            return false;
        }

        if (_border.App is { })
        {
            _border.App.Mouse.MouseEvent += ApplicationOnMouseEvent;
            _border.App.Mouse.GrabbingMouse += ApplicationOnGrabbingMouse;
        }

        bool mouseMode = _border.App is { } && _border.App.Mouse.IsGrabbed (_border);

        _border.HotKeyBindings.Add (Key.Esc, Command.Quit);

        Key arrangeKey = Application.GetDefaultKey (Command.Arrange);

        if (arrangeKey != Key.Empty)
        {
            _border.HotKeyBindings.Add (arrangeKey, Command.Quit);
        }
        _border.HotKeyBindings.Add (Key.CursorUp, Command.Up);
        _border.HotKeyBindings.Add (Key.CursorDown, Command.Down);
        _border.HotKeyBindings.Add (Key.CursorLeft, Command.Left);
        _border.HotKeyBindings.Add (Key.CursorRight, Command.Right);
        _border.HotKeyBindings.Add (Key.Tab, Command.NextTabStop);
        _border.HotKeyBindings.Add (Key.Tab.WithShift, Command.PreviousTabStop);

        CreateArrangementButtons ();

        _border.MouseState |= MouseState.Pressed;

        if (mouseMode)
        {
            Arranging = arrangement;
            SetVisibilityForMouseMode (arrangement);
        }
        else
        {
            SetVisibilityForKeyboardMode ();
            _border.CanFocus = true;
            _border.SetFocus ();

            // Strip off overlapped
            Arranging = _border.Adornment!.Parent!.Arrangement & ~ViewArrangement.Overlapped;
        }

        return true;
    }

    private void ApplicationOnMouseEvent (object? sender, Mouse mouse)
    {
        if (mouse.Flags != MouseFlags.LeftButtonClicked)
        {
            return;
        }

        // If mouse click is outside of Border.Thickness then exit Arrange Mode
        Point framePos = _border.ScreenToFrame (mouse.ScreenPosition);

        if (!_border.Adornment!.Thickness.Contains (_border.Frame, framePos))
        {
            ExitArrangeMode ();
        }
    }

    /// <summary>
    ///     Cancels <see cref="IMouseGrabHandler.GrabbingMouse"/> events during an active drag to prevent other views from
    ///     stealing the mouse grab mid-operation.
    /// </summary>
    /// <remarks>
    ///     During an Arrange Mode drag, Border owns the mouse grab and
    ///     must receive all mouse events until Button1Released. If another view (e.g., scrollbar, slider) were allowed
    ///     to grab the mouse, the drag would freeze, leaving Border in an inconsistent state with no cleanup.
    ///     Canceling follows the CWP pattern, ensuring Border maintains exclusive mouse control until it explicitly
    ///     releases via <see cref="IMouseGrabHandler.UngrabMouse"/> in <see cref="View.OnMouseEvent"/>.
    /// </remarks>
    private void ApplicationOnGrabbingMouse (object? sender, GrabMouseEventArgs e)
    {
        if (_border.App is { } && _border.App.Mouse.IsGrabbed (_border) && IsDragging)
        {
            e.Cancel = true;
        }
    }

    /// <summary>
    ///     Exits arrangement mode and cleans up resources.
    /// </summary>
    internal bool? ExitArrangeMode ()
    {
        if (_border.App is { })
        {
            _border.App.Mouse.MouseEvent -= ApplicationOnMouseEvent;
            _border.App.Mouse.GrabbingMouse -= ApplicationOnGrabbingMouse;

            if (_border.App.Mouse.IsGrabbed (_border))
            {
                _border.App.Mouse.UngrabMouse ();
            }
        }

        _border.MouseState &= ~MouseState.Pressed;

        if (_border.CanFocus)
        {
            _border.CanFocus = false;
        }

        Arranging = ViewArrangement.Fixed;
        _dragPosition = null;

        _border.HotKeyBindings.Clear ();

        // Clean up all arrangement buttons
        DisposeSizeButton (ref _moveButton);
        DisposeSizeButton (ref _allSizeButton);
        DisposeSizeButton (ref _leftSizeButton);
        DisposeSizeButton (ref _rightSizeButton);
        DisposeSizeButton (ref _topSizeButton);
        DisposeSizeButton (ref _bottomSizeButton);

        return true;
    }

    /// <summary>
    ///     Checks if the border's parent view has any arrangement options enabled.
    /// </summary>
    internal bool HasAnyArrangementOptions ()
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return false;
        }

        return parent.Arrangement.HasFlag (ViewArrangement.Movable)
               || parent.Arrangement.HasFlag (ViewArrangement.BottomResizable)
               || parent.Arrangement.HasFlag (ViewArrangement.TopResizable)
               || parent.Arrangement.HasFlag (ViewArrangement.LeftResizable)
               || parent.Arrangement.HasFlag (ViewArrangement.RightResizable);
    }

    #region Button Management

    private ArrangerButton? _moveButton;
    private ArrangerButton? _allSizeButton;
    private ArrangerButton? _leftSizeButton;
    private ArrangerButton? _rightSizeButton;
    private ArrangerButton? _topSizeButton;
    private ArrangerButton? _bottomSizeButton;

    /// <summary>
    ///     Creates all the arrangement buttons based on parent's arrangement options.
    /// </summary>
    private void CreateArrangementButtons ()
    {
        ViewArrangement parentArrangement = _border.Adornment!.Parent!.Arrangement;

        if (parentArrangement.HasFlag (ViewArrangement.Movable))
        {
            _moveButton = CreateArrangerButton (ArrangeButtons.Move, 0, 0);
        }

        if (parentArrangement.HasFlag (ViewArrangement.Resizable))
        {
            _allSizeButton = CreateArrangerButton (ArrangeButtons.AllSize, Pos.AnchorEnd (), Pos.AnchorEnd ());
        }

        if (parentArrangement.HasFlag (ViewArrangement.TopResizable))
        {
            _topSizeButton = CreateArrangerButton (ArrangeButtons.TopSize, Pos.Center () + _border.Adornment.Parent!.Margin.Thickness.Horizontal, 0);
        }

        if (parentArrangement.HasFlag (ViewArrangement.RightResizable))
        {
            _rightSizeButton = CreateArrangerButton (ArrangeButtons.RightSize,
                                                     Pos.AnchorEnd (),
                                                     Pos.Center () + _border.Adornment.Parent!.Margin.Thickness.Vertical / 2);
        }

        if (parentArrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            _leftSizeButton = CreateArrangerButton (ArrangeButtons.LeftSize, 0, Pos.Center () + _border.Adornment.Parent!.Margin.Thickness.Vertical / 2);
        }

        if (parentArrangement.HasFlag (ViewArrangement.BottomResizable))
        {
            _bottomSizeButton = CreateArrangerButton (ArrangeButtons.BottomSize,
                                                      Pos.Center () + _border.Adornment.Parent!.Margin.Thickness.Horizontal / 2,
                                                      Pos.AnchorEnd ());
        }
    }

    /// <summary>
    ///     Factory method to create a standardized arrangement button.
    /// </summary>
    private ArrangerButton CreateArrangerButton (ArrangeButtons buttonType, Pos x, Pos y)
    {
        ArrangerButton button = new ()
        {
            ButtonType = buttonType,
#if DEBUG
            Id = buttonType.ToString (),
#endif
            X = x,
            Y = y,
            Visible = false
        };

        button.KeyBindings.Remove (Key.Space);
        button.KeyBindings.Remove (Key.Enter);

        _border.Add (button);

        return button;
    }

    /// <summary>
    ///     Sets button visibility for keyboard arrangement mode.
    /// </summary>
    private void SetVisibilityForKeyboardMode ()
    {
        ViewArrangement parentArrangement = _border.Adornment!.Parent!.Arrangement;

        if (parentArrangement.HasFlag (ViewArrangement.Movable))
        {
            SetVisibleButton (_moveButton);
        }

        if (parentArrangement.HasFlag (ViewArrangement.Resizable))
        {
            SetVisibleButton (_allSizeButton);
        }

        ShowResizableButtons (parentArrangement.HasFlag (ViewArrangement.LeftResizable),
                              parentArrangement.HasFlag (ViewArrangement.RightResizable),
                              parentArrangement.HasFlag (ViewArrangement.TopResizable),
                              parentArrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    /// <summary>
    ///     Sets button visibility based on the specified mouse arrangement mode.
    /// </summary>
    private void SetVisibilityForMouseMode (ViewArrangement arrangement)
    {
        switch (arrangement)
        {
            case ViewArrangement.Movable:
                SetVisibleButton (_moveButton);

                break;

            case ViewArrangement.Resizable:
            case ViewArrangement.RightResizable | ViewArrangement.BottomResizable:
                ShowResizableButtons (right: true, bottom: true);
                ShowAllSizeButton (Pos.AnchorEnd (), Pos.AnchorEnd ());

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
                ShowResizableButtons (true, bottom: true);
                ShowAllSizeButton (0, Pos.AnchorEnd ());

                break;

            case ViewArrangement.LeftResizable | ViewArrangement.TopResizable:
                ShowResizableButtons (true, top: true);

                break;

            case ViewArrangement.RightResizable | ViewArrangement.TopResizable:
                ShowResizableButtons (right: true, top: true);
                ShowAllSizeButton (Pos.AnchorEnd (), 0);

                break;
        }
    }

    /// <summary>
    ///     Shows the specified directional resize buttons.
    /// </summary>
    private void ShowResizableButtons (bool left = false, bool right = false, bool top = false, bool bottom = false)
    {
        if (left)
        {
            SetVisibleButton (_leftSizeButton);
        }

        if (right)
        {
            SetVisibleButton (_rightSizeButton);
        }

        if (top)
        {
            SetVisibleButton (_topSizeButton);
        }

        if (bottom)
        {
            SetVisibleButton (_bottomSizeButton);
        }
    }

    /// <summary>
    ///     Shows and positions the all-size button at the specified location.
    /// </summary>
    private void ShowAllSizeButton (Pos x, Pos y)
    {
        if (_allSizeButton == null)
        {
            return;
        }
        _allSizeButton.X = x;
        _allSizeButton.Y = y;
        _allSizeButton.Visible = true;
    }

    /// <summary>
    ///     Helper method to make a button visible if it's not null.
    /// </summary>
    private void SetVisibleButton (Button? button) => button?.Visible = true;

    /// <summary>
    ///     Helper method to dispose and remove a button.
    /// </summary>
    private void DisposeSizeButton (ref ArrangerButton? button)
    {
        if (button is null)
        {
            return;
        }

        _border.Remove (button);
        button.Dispose ();
        button = null;
    }

    #endregion Button Management

    #region Keyboard Arrangement

    /// <summary>
    ///     Maps an <see cref="ArrangeButtons"/> to its corresponding <see cref="ViewArrangement"/>.
    /// </summary>
    private static ViewArrangement GetArrangementForButton (ArrangeButtons button) =>
        button switch
        {
            ArrangeButtons.Move => ViewArrangement.Movable,
            ArrangeButtons.AllSize => ViewArrangement.Resizable,
            ArrangeButtons.LeftSize => ViewArrangement.LeftResizable,
            ArrangeButtons.RightSize => ViewArrangement.RightResizable,
            ArrangeButtons.TopSize => ViewArrangement.TopResizable,
            ArrangeButtons.BottomSize => ViewArrangement.BottomResizable,
            _ => ViewArrangement.Fixed
        };

    /// <summary>
    ///     Gets the arrangement type from the border's currently focused button.
    /// </summary>
    internal ViewArrangement GetFocusedArrangement ()
    {
        if (_border.Focused is ArrangerButton focusedButton)
        {
            return GetArrangementForButton (focusedButton.ButtonType);
        }

        return ViewArrangement.Fixed;
    }

    /// <summary>
    ///     Handles Up arrow key in arrange mode.
    /// </summary>
    internal bool HandleArrangeModeUp ()
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return false;
        }

        int minHeight = _border.Adornment!.Thickness.Vertical + parent.Margin.Thickness.Bottom;
        int minWidth = _border.Adornment!.Thickness.Horizontal + parent.Margin.Thickness.Right;
        ViewManipulator manipulator = new (parent, minWidth, minHeight);
        var handled = false;

        if (Arranging.HasFlag (ViewArrangement.Movable))
        {
            manipulator.AdjustY (-1);
            handled = true;
        }

        if (Arranging == ViewArrangement.Resizable || GetFocusedArrangement ().HasFlag (ViewArrangement.BottomResizable))
        {
            handled |= manipulator.AdjustHeight (-1);
        }

        if (GetFocusedArrangement () == ViewArrangement.TopResizable)
        {
            handled |= manipulator.ResizeFromTop (-1);
        }

        return handled;
    }

    /// <summary>
    ///     Handles Down arrow key in arrange mode.
    /// </summary>
    internal bool HandleArrangeModeDown ()
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return false;
        }

        int minHeight = _border.Adornment!.Thickness.Vertical + parent.Margin.Thickness.Bottom;
        int minWidth = _border.Adornment!.Thickness.Horizontal + parent.Margin.Thickness.Right;
        ViewManipulator manipulator = new (parent, minWidth, minHeight);
        var handled = false;

        if (Arranging.HasFlag (ViewArrangement.Movable))
        {
            manipulator.AdjustY (1);
            handled = true;
        }

        if (Arranging == ViewArrangement.Resizable || GetFocusedArrangement ().HasFlag (ViewArrangement.BottomResizable))
        {
            handled |= manipulator.AdjustHeight (1);
        }

        if (GetFocusedArrangement () == ViewArrangement.TopResizable)
        {
            handled |= manipulator.ResizeFromTop (1);
        }

        return handled;
    }

    /// <summary>
    ///     Handles Left arrow key in arrange mode.
    /// </summary>
    internal bool HandleArrangeModeLeft ()
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return false;
        }

        int minHeight = _border.Adornment!.Thickness.Vertical + parent.Margin.Thickness.Bottom;
        int minWidth = _border.Adornment!.Thickness.Horizontal + parent.Margin.Thickness.Right;
        ViewManipulator manipulator = new (parent, minWidth, minHeight);
        var handled = false;

        if (Arranging.HasFlag (ViewArrangement.Movable))
        {
            manipulator.AdjustX (-1);
            handled = true;
        }

        if (Arranging == ViewArrangement.Resizable || GetFocusedArrangement ().HasFlag (ViewArrangement.RightResizable))
        {
            handled |= manipulator.AdjustWidth (-1);
        }

        if (GetFocusedArrangement () == ViewArrangement.LeftResizable)
        {
            handled |= manipulator.ResizeFromLeft (-1);
        }

        return handled;
    }

    /// <summary>
    ///     Handles Right arrow key in arrange mode.
    /// </summary>
    internal bool HandleArrangeModeRight ()
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return false;
        }

        int minHeight = _border.Adornment!.Thickness.Vertical + parent.Margin.Thickness.Bottom;
        int minWidth = _border.Adornment!.Thickness.Horizontal + parent.Margin.Thickness.Right;
        ViewManipulator manipulator = new (parent, minWidth, minHeight);
        var handled = false;

        if (Arranging.HasFlag (ViewArrangement.Movable))
        {
            manipulator.AdjustX (1);
            handled = true;
        }

        if (Arranging == ViewArrangement.Resizable || GetFocusedArrangement ().HasFlag (ViewArrangement.RightResizable))
        {
            handled |= manipulator.AdjustWidth (1);
        }

        if (GetFocusedArrangement () == ViewArrangement.LeftResizable)
        {
            handled |= manipulator.ResizeFromLeft (1);
        }

        return handled;
    }

    /// <summary>
    ///     Handles Tab key to advance focus to next arrangement button.
    /// </summary>
    internal bool? HandleArrangeModeTab ()
    {
        _border.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Arranging = GetFocusedArrangement ();

        return true;
    }

    /// <summary>
    ///     Handles Shift+Tab key to advance focus to previous arrangement button.
    /// </summary>
    internal bool? HandleArrangeModeBackTab ()
    {
        _border.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
        Arranging = GetFocusedArrangement ();

        return true;
    }

    #endregion Keyboard Arrangement

    #region Mouse Arrangement

    /// <summary>
    ///     Handles mouse events for arrangement operations.
    /// </summary>
    /// <returns>True if the event was handled.</returns>
    internal bool HandleMouseEvent (Mouse mouseEvent)
    {
        // Button pressed - start potential drag
        if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            return HandleMousePressed (mouseEvent);
        }

        // Dragging - update position
        if (mouseEvent.Flags is (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport)
            && _border.App is { }
            && _border.App.Mouse.IsGrabbed (_border)
            && _dragPosition.HasValue)
        {
            HandleMouseDrag (mouseEvent);

            return true;
        }

        // Button released - end drag
        if (mouseEvent.Flags.HasFlag (MouseFlags.LeftButtonReleased) && _dragPosition.HasValue)
        {
            return ExitArrangeMode () is true;
        }

        return false;
    }

    /// <summary>
    ///     Handles mouse button press to potentially start arrangement.
    /// </summary>
    private bool HandleMousePressed (Mouse mouseEvent)
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return false;
        }

        parent.SetFocus ();

        if (!HasAnyArrangementOptions ())
        {
            return false;
        }

        // Only start grabbing if the user clicks in the Thickness area
        // Adornment.Contains takes Parent SuperView=relative coords.
        Point clickPoint = new (mouseEvent.Position!.Value.X + parent.Frame.X + _border.Frame.X,
                                mouseEvent.Position!.Value.Y + parent.Frame.Y + _border.Frame.Y);

        if (!_border.Contains (clickPoint))
        {
            return false;
        }

        // If already arranging, exit first
        if (IsArranging)
        {
            ExitArrangeMode ();
        }

        // Set the start grab point to the Frame coords
        GrabPoint = new Point (mouseEvent.Position!.Value.X + _border.Frame.X, mouseEvent.Position!.Value.Y + _border.Frame.Y);
        _dragPosition = mouseEvent.Position;

        // Grab mouse
        _border.App?.Mouse.GrabMouse (_border);

        // Determine the arrangement mode and request entry
        ViewArrangement arrangeMode = DetermineArrangeModeFromClick (GrabPoint);

        return EnterArrangeMode (arrangeMode);
    }

    /// <summary>
    ///     Handles mouse drag to update parent position/size.
    /// </summary>
    private void HandleMouseDrag (Mouse mouseEvent)
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return;
        }

        _dragPosition = mouseEvent.Position;

        Point parentLoc = parent.SuperView?.ScreenToViewport (new Point (mouseEvent.ScreenPosition.X, mouseEvent.ScreenPosition.Y))
                          ?? mouseEvent.ScreenPosition;

        HandleDragOperation (parentLoc);
    }

    /// <summary>
    ///     Gets the grab point for the current drag operation.
    ///     INTERNAL: Exposed for testing purposes.
    /// </summary>
    internal Point GrabPoint { get; private set; }

    /// <summary>
    ///     Starts a mouse drag operation.
    ///     INTERNAL: Exposed for testing purposes.
    /// </summary>
    internal void StartDrag (Point grabPoint, Point dragPosition)
    {
        GrabPoint = grabPoint;
        _dragPosition = dragPosition;
    }

    /// <summary>
    ///     Ends the current drag operation.
    ///     INTERNAL: Exposed for testing purposes.
    /// </summary>
    internal void EndDrag () => _dragPosition = null;

    /// <summary>
    ///     Determines the arrangement mode based on where the mouse was clicked.
    ///     INTERNAL: Exposed for testing purposes.
    /// </summary>
    internal ViewArrangement DetermineArrangeModeFromClick (Point clickPoint)
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return ViewArrangement.Fixed;
        }

        ViewArrangement parentArrangement = parent.Arrangement;
        Rectangle frame = _border.Frame;
        Thickness thickness = _border.Adornment!.Thickness;

        // Check edges first (larger hit areas)
        // Left edge
        if (parentArrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            Rectangle leftRect = new (frame.X, frame.Y + thickness.Top, thickness.Left, frame.Height - thickness.Top - thickness.Bottom);

            if (leftRect.Contains (clickPoint))
            {
                return ViewArrangement.LeftResizable;
            }
        }

        // Right edge
        if (parentArrangement.HasFlag (ViewArrangement.RightResizable))
        {
            Rectangle rightRect = new (frame.X + frame.Width - thickness.Right,
                                       frame.Y + thickness.Top,
                                       thickness.Right,
                                       frame.Height - thickness.Top - thickness.Bottom);

            if (rightRect.Contains (clickPoint))
            {
                return ViewArrangement.RightResizable;
            }
        }

        // Top edge (only if not movable)
        if (parentArrangement.HasFlag (ViewArrangement.TopResizable) && !parentArrangement.HasFlag (ViewArrangement.Movable))
        {
            Rectangle topRect = new (frame.X + thickness.Left, frame.Y, frame.Width - thickness.Left - thickness.Right, thickness.Top);

            if (topRect.Contains (clickPoint))
            {
                return ViewArrangement.TopResizable;
            }
        }

        // Bottom edge
        if (parentArrangement.HasFlag (ViewArrangement.BottomResizable))
        {
            Rectangle bottomRect = new (frame.X + thickness.Left,
                                        frame.Y + frame.Height - thickness.Bottom,
                                        frame.Width - thickness.Left - thickness.Right,
                                        thickness.Bottom);

            if (bottomRect.Contains (clickPoint))
            {
                return ViewArrangement.BottomResizable;
            }
        }

        // Check corners
        // Bottom-left
        if (parentArrangement.HasFlag (ViewArrangement.BottomResizable) && parentArrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            Rectangle corner = new (frame.X, frame.Height - thickness.Top, thickness.Left, thickness.Bottom);

            if (corner.Contains (clickPoint))
            {
                return ViewArrangement.BottomResizable | ViewArrangement.LeftResizable;
            }
        }

        // Bottom-right
        if (parentArrangement.HasFlag (ViewArrangement.BottomResizable) && parentArrangement.HasFlag (ViewArrangement.RightResizable))
        {
            Rectangle corner = new (frame.X + frame.Width - thickness.Right, frame.Height - thickness.Top, thickness.Right, thickness.Bottom);

            if (corner.Contains (clickPoint))
            {
                return ViewArrangement.BottomResizable | ViewArrangement.RightResizable;
            }
        }

        // Top-right
        if (parentArrangement.HasFlag (ViewArrangement.TopResizable) && parentArrangement.HasFlag (ViewArrangement.RightResizable))
        {
            Rectangle corner = new (frame.X + frame.Width - thickness.Right, frame.Y, thickness.Right, thickness.Top);

            if (corner.Contains (clickPoint))
            {
                return ViewArrangement.TopResizable | ViewArrangement.RightResizable;
            }
        }

        // Top-left
        if (parentArrangement.HasFlag (ViewArrangement.TopResizable) && parentArrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            Rectangle corner = frame with { Width = thickness.Left, Height = thickness.Top };

            if (corner.Contains (clickPoint))
            {
                return ViewArrangement.TopResizable | ViewArrangement.LeftResizable;
            }
        }

        // Default to movable if enabled
        if (parentArrangement.HasFlag (ViewArrangement.Movable))
        {
            return ViewArrangement.Movable;
        }

        return ViewArrangement.Fixed;
    }

    /// <summary>
    ///     Handles drag operations for moving and resizing the parent view.
    ///     INTERNAL: Test compatibility wrapper - extracts Point from Mouse event.
    /// </summary>
    /// <param name="mouseEvent">The mouse event containing screen position information.</param>
    internal void HandleDragOperation (Mouse mouseEvent)
    {
        Point targetLocation = _border.Adornment?.Parent!.SuperView?.ScreenToViewport (new Point (mouseEvent.ScreenPosition.X, mouseEvent.ScreenPosition.Y))
                               ?? mouseEvent.ScreenPosition;

        HandleDragOperation (targetLocation);
    }

    /// <summary>
    ///     Handles drag operations for moving and resizing the parent view based on mouse position.
    ///     Uses <see cref="ViewManipulator"/> to apply the appropriate transformation (move or resize)
    ///     based on the current <see cref="Arranging"/> mode.
    /// </summary>
    /// <param name="targetLocation">
    ///     The target mouse position in the parent's SuperView coordinate space.
    ///     This is typically obtained by converting screen coordinates to the parent's SuperView viewport.
    ///     For views without a SuperView, this is the screen position directly.
    /// </param>
    internal void HandleDragOperation (Point targetLocation)
    {
        View? parent = _border.Adornment?.Parent;

        if (parent is null)
        {
            return;
        }

        int minHeight = _border.Adornment!.Thickness.Vertical + parent.Margin.Thickness.Bottom;
        int minWidth = _border.Adornment!.Thickness.Horizontal + parent.Margin.Thickness.Right;

        ViewManipulator manipulator = new (parent, GrabPoint, minWidth, minHeight);

        switch (Arranging)
        {
            case ViewArrangement.Movable:
                manipulator.Move (targetLocation);

                break;

            case ViewArrangement.TopResizable:
                manipulator.ResizeTop (targetLocation);

                break;

            case ViewArrangement.BottomResizable:
                manipulator.ResizeBottom (targetLocation);

                break;

            case ViewArrangement.LeftResizable:
                manipulator.ResizeLeft (targetLocation);

                break;

            case ViewArrangement.RightResizable:
                manipulator.ResizeRight (targetLocation);

                break;

            case ViewArrangement.BottomResizable | ViewArrangement.RightResizable:
                manipulator.ResizeRight (targetLocation);
                manipulator.ResizeBottom (targetLocation);

                break;

            case ViewArrangement.BottomResizable | ViewArrangement.LeftResizable:
                manipulator.ResizeLeft (targetLocation);
                manipulator.ResizeBottom (targetLocation);

                break;

            case ViewArrangement.TopResizable | ViewArrangement.RightResizable:
                manipulator.ResizeTop (targetLocation);
                manipulator.ResizeRight (targetLocation);

                break;

            case ViewArrangement.TopResizable | ViewArrangement.LeftResizable:
                manipulator.ResizeTop (targetLocation);
                manipulator.ResizeLeft (targetLocation);

                break;
        }
    }

    #endregion Mouse Arrangement

    /// <inheritdoc/>
    public void Dispose ()
    {
        // Ungrab mouse if we're still holding it
        if (IsDragging && _border.App is { } && _border.App.Mouse.IsGrabbed (_border))
        {
            _border.App.Mouse.UngrabMouse ();
        }

        ExitArrangeMode ();
    }
}
