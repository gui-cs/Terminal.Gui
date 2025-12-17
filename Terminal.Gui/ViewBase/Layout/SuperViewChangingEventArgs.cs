namespace Terminal.Gui.ViewBase;

/// <summary>
///     Args for events where the <see cref="View.SuperView"/> of a <see cref="View"/> is about to be changed (e.g.
///     before <see cref="View.Remove(View?)"/>).
/// </summary>
/// <remarks>
///     This event is raised before the <see cref="View.SuperView"/> property is changed, allowing access to the
///     current SuperView and its associated resources (such as <see cref="View.App"/>) for cleanup purposes.
/// </remarks>
public class SuperViewChangingEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="SuperViewChangingEventArgs"/> class.</summary>
    /// <param name="oldSuperView">The current SuperView before the change.</param>
    /// <param name="newSuperView">The new SuperView that will be set, or <see langword="null"/> if being removed.</param>
    /// <param name="subView">The view whose SuperView is changing.</param>
    public SuperViewChangingEventArgs (View? oldSuperView, View? newSuperView, View? subView)
    {
        OldSuperView = oldSuperView;
        NewSuperView = newSuperView;
        SubView = subView;
    }

    /// <summary>The current SuperView before the change.</summary>
    public View? OldSuperView { get; }

    /// <summary>The new SuperView that will be set, or <see langword="null"/> if being removed.</summary>
    public View? NewSuperView { get; }

    /// <summary>The view that is having its <see cref="View.SuperView"/> changed.</summary>
    public View? SubView { get; }
}
