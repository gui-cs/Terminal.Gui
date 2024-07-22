
namespace Terminal.Gui;
using System;

/// <summary>
///     Implement this interface to provide orientation support.
/// </summary>
public interface IOrientation
{
    /// <summary>
    ///     Gets or sets the orientation of the View.
    /// </summary>
    Orientation Orientation { get; set; }

    /// <summary>
    ///     Raised when <see cref="Orientation"/> is changing. Can be cancelled.
    /// </summary>
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanging;

    /// <summary>
    ///     Called when <see cref="Orientation"/> is changing.
    /// </summary>
    /// <param name="currentOrientation">The current orientation.</param>
    /// <param name="newOrientation">The new orientation.</param>
    /// <returns><see langword="true"/> to cancel the change.</returns>
    public bool OnOrientationChanging (Orientation currentOrientation, Orientation newOrientation) { return false; }

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanged;

    /// <summary>
    ///     Called when <see cref="Orientation"/> has been changed.
    /// </summary>
    /// <param name="oldOrientation"></param>
    /// <param name="newOrientation"></param>
    /// <returns></returns>
    public void OnOrientationChanged (Orientation oldOrientation, Orientation newOrientation) { return; }
}