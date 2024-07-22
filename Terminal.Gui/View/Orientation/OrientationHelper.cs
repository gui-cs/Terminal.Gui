namespace Terminal.Gui;

/// <summary>
///     Helper class for implementing <see cref="IOrientation"/>.
/// </summary>
public class OrientationHelper
{
    private Orientation _orientation = Orientation.Vertical;
    private readonly IOrientation _owner;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OrientationHelper"/> class.
    /// </summary>
    /// <param name="owner"></param>
    public OrientationHelper (IOrientation owner)
    {
        _owner = owner;
    }

    /// <summary>
    ///     Gets or sets the orientation of the View.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value)
            {
                return;
            }

            var args = new CancelEventArgs<Orientation> (in _orientation, ref value);
            OrientationChanging?.Invoke (_owner, args);
            if (args.Cancel)
            {
                return;
            }

            if (_owner?.OnOrientationChanging (value, _orientation) ?? false)
            {
                return;
            }

            Orientation old = _orientation;
            if (_orientation != value)
            {
                _orientation = value;

                if (_owner is { })
                {
                    _owner.Orientation = value;
                }
            }

            args = new CancelEventArgs<Orientation> (in old, ref _orientation);
            OrientationChanged?.Invoke (_owner, args);

            _owner?.OnOrientationChanged (old, _orientation);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanging;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="currentOrientation"></param>
    /// <param name="newOrientation"></param>
    /// <returns></returns>
    protected bool OnOrientationChanging (Orientation currentOrientation, Orientation newOrientation)
    {
        return _owner?.OnOrientationChanging (currentOrientation, newOrientation) ?? false;
    }

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanged;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="oldOrientation"></param>
    /// <param name="newOrientation"></param>
    /// <returns></returns>
    protected void OnOrientationChanged (Orientation oldOrientation, Orientation newOrientation)
    {
        _owner?.OnOrientationChanged (oldOrientation, newOrientation);
    }
}
