namespace Terminal.Gui.ViewBase;

/// <summary>
///     Helper class that encapsulates view manipulation operations for arrangement (move/resize).
///     Provides methods to move and resize a view while respecting minimum size constraints.
/// </summary>
internal sealed class ViewManipulator
{
    private readonly View _view;
    private readonly Point _grabPoint;
    private readonly int _minWidth;
    private readonly int _minHeight;

    /// <summary>
    ///     Creates a new ViewManipulator for the specified view.
    /// </summary>
    /// <param name="view">The view to manipulate.</param>
    /// <param name="grabPoint">The point where the drag started (in frame coordinates).</param>
    /// <param name="minWidth">Minimum allowed width.</param>
    /// <param name="minHeight">Minimum allowed height.</param>
    internal ViewManipulator (View view, Point grabPoint, int minWidth, int minHeight)
    {
        _view = view;
        _grabPoint = grabPoint;
        _minWidth = minWidth;
        _minHeight = minHeight;
    }

    /// <summary>
    ///     Moves the view to follow the mouse position.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    internal void Move (Point location)
    {
        _view.X = location.X - _grabPoint.X;
        _view.Y = location.Y - _grabPoint.Y;
    }

    /// <summary>
    ///     Resizes view from the top edge, adjusting Y position and height.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    internal void ResizeTop (Point location)
    {
        int deltaY = location.Y - _view.Frame.Y;
        int newHeight = Math.Max (_minHeight, _view.Frame.Height - deltaY);

        if (newHeight != _view.Frame.Height)
        {
            _view.Height = newHeight;
            _view.Y = location.Y - _grabPoint.Y;
        }
    }

    /// <summary>
    ///     Resizes view from the bottom edge, adjusting height only.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    internal void ResizeBottom (Point location)
    {
        _view.Height = Math.Max (_minHeight, location.Y - _view.Frame.Y + _view.Margin!.Thickness.Bottom + 1);
    }

    /// <summary>
    ///     Resizes view from the left edge, adjusting X position and width.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    internal void ResizeLeft (Point location)
    {
        int deltaX = location.X - _view.Frame.X;
        int newWidth = Math.Max (_minWidth, _view.Frame.Width - deltaX);

        if (newWidth != _view.Frame.Width)
        {
            _view.Width = newWidth;
            _view.X = location.X - _grabPoint.X;
        }
    }

    /// <summary>
    ///     Resizes view from the right edge, adjusting width only.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    internal void ResizeRight (Point location)
    {
        _view.Width = Math.Max (_minWidth, location.X - _view.Frame.X + _view.Margin!.Thickness.Right + 1);
    }
}
