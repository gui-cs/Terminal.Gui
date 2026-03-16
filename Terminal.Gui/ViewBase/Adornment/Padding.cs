namespace Terminal.Gui.ViewBase;

/// <summary>
///     The lightweight Padding settings for a <see cref="View"/>. Accessed via <see cref="View.Padding"/>.
///     A <see cref="PaddingView"/> is created lazily when SubViews are added.
/// </summary>
/// <remarks>
///     <para>See the <see cref="AdornmentImpl"/> class.</para>
/// </remarks>
public class Padding : AdornmentImpl
{
    /// <summary>
    ///     Adds a SubView to the Padding. Forces creation of <see cref="PaddingView"/>.
    /// </summary>
    public override void Add (View view) => ((PaddingView)EnsureView ()).Add (view);

    /// <inheritdoc/>
    protected override AdornmentView CreateView () => new PaddingView (Parent);
}
