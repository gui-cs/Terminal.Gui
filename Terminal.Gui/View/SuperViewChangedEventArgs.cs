namespace Terminal.Gui;

/// <summary>
///     EventArgs for events where the state of the <see cref="View.SuperView"/> of a <see cref="View"/> is changing (e.g.
///     <see cref="View.Removed"/> / <see cref="View.Added"/> events).
/// </summary>
public class SuperViewChangedEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="SuperViewChangedEventArgs"/> class.</summary>
    /// <param name="superView"></param>
    /// <param name="subView"></param>
    public SuperViewChangedEventArgs (View superView, View subView)
    {
        SuperView = superView;
        SubView = subView;
    }

    /// <summary>The SubView that is either being added or removed from <see cref="Parent"/>.</summary>
    public View SubView { get; }

    /// <summary>
    ///     The SuperView that is changing state. For <see cref="View.Removed"/> this is the SuperView <see cref="SubView"/> is being removed from. For
    ///     <see cref="View.Added"/> it is the SuperView <see cref="SubView"/> is being added to.
    /// </summary>
    public View SuperView { get; }
}
