namespace Terminal.Gui;

// TODO: v2 - Missing 3D effect - 3D effects will be drawn by a mechanism separate from Adornments
// TODO: v2 - If a Adornment has focus, navigation keys (e.g Command.NextView) should cycle through SubViews of the Adornments
// QUESTION: How does a user navigate out of an Adornment to another Adornment, or back into the Parent's SubViews?
/// <summary>
<<<<<<< HEAD

    ///     Adornments are a special form of <see cref="View"/> that appear outside of the <see cref="View.Bounds"/>:
    ///     <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. They are defined using the
    ///     <see cref="Thickness"/> class, which specifies the thickness of the sides of a rectangle.
    /// </summary>
    /// <remarsk>
    ///     <para>
    ///         There is no prevision for creating additional subclasses of Adornment. It is not abstract to enable unit
    ///         testing.
    ====== =

    ///         Adornments are a special form of <see cref="View" /> that appear outside of the <see cref="View.Bounds" />:
    ///         <see cref="Margin" />, <see cref="Border" />, and <see cref="Padding" />. They are defined using the
    ///         <see cref="Thickness" />
    ///         class, which specifies the thickness of the sides of a rectangle.
    /// </summary>
    /// <remarsk>
    ///         <para>
    ///                 There is no prevision for creating additional subclasses of Adornment. It is not abstract to enable
    ///                 unit testing.
    ///         </para>
    ///         <para>
    ///                 Each of <see cref="Margin" />, <see cref="Border" />, and <see cref="Padding" /> can be customized.
    >>>>>>> bDisp - v2_remove - constructors - frame - parameters_3171

/// </para>
/// <para>Each of <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> can be customized.</para>
/// </remarsk>
public class Adornment : View {
    <<<<<<< HEAD
private var _thickness = Thickness.Empty;

/// <inheritdoc/>
public Adornment () { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
}

/// <summary>Constructs a new adornment for the view specified by <paramref name="parent"/>.</summary>
====== =

var _thickness = Thickness.Empty;

/// <inheritdoc />
public Adornment () {
    /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
}

/// <summary>
///         Constructs a new adornment for the view specified by <paramref name="parent" />.
/// </summary>
>>>>>>>

bDisp - v2_remove - constructors - frame - parameters_3171

/// <param name="parent"></param>
public Adornment (View parent) {
    Parent = parent;
}
<<<<<<<

HEAD

/// <summary>Gets the rectangle that describes the inner area of the Adornment. The Location is always (0,0).</summary>
public override Rect Bounds {
get => Thickness?.
internal GetInside (new internal Rect (Point.Empty, Frame.Size)) ?? new internal Rect (Point.Empty, Frame.Size);
set => throw new internal InvalidOperationException ("It makes no sense to set Bounds of a Thickness.");
}

/// <summary>The Parent of this Adornment (the View this Adornment surrounds).</summary>
====== =

/// <summary>
///         The Parent of this Adornment (the View this Adornment surrounds).
/// </summary>
>>>>>>> bDisp - v2_remove - constructors - frame - parameters_3171

/// <remarks>
///     Adornments are distinguished from typical View classes in that they are not sub-views, but have a parent/child
///     relationship with their containing View.
/// </remarks>
public View Parent {
get;
set;
}

/// <summary>
<<<<<<< HEAD

    ///     Adornments cannot be used as sub-views (see <see cref="Parent"/>); this method always throws an
    ///     <see cref="InvalidOperationException"/>. TODO: Are we sure?
    ====== =

    ///         Adornments cannot be used as sub-views (see <see cref="Parent" />); this method always throws an
    ///         <see cref="InvalidOperationException" />.
    ///         TODO: Are we sure?
    >>>>>>> bDisp - v2_remove - constructors - frame - parameters_3171

/// </summary>
public override View SuperView {
get => null; set => throw new internal NotImplementedException ();
}

/// <summary>
<<<<<<< HEAD

    ///     Adornments only render to their <see cref="Parent"/>'s or Parent's SuperView's LineCanvas, so setting this
    ///     property throws an <see cref="InvalidOperationException"/>.
    ====== =

    ///         Adornments only render to their <see cref="Parent" />'s or Parent's SuperView's LineCanvas,
    ///         so setting this property throws an <see cref="InvalidOperationException" />.
    >>>>>>> bDisp - v2_remove - constructors - frame - parameters_3171

/// </summary>
public override bool SuperViewRendersLineCanvas {
    get => false; // throw new NotImplementedException ();
    set => throw new NotImplementedException ();
}

<<<<<<< HEAD

    /// <summary>Defines the rectangle that the <see cref="Adornment"/> will use to draw its content.</summary>
    ====== =

    /// <summary>
    ///         Defines the rectangle that the <see cref="Adornment" /> will use to draw its content.
    /// </summary>
    >>>>>>> bDisp - v2_remove - constructors - frame - parameters_3171
public Thickness Thickness {
get => _thickness;

set {
    Thickness prev = _thickness;
    _thickness = value;
    if (prev != _thickness) {
        Parent?.LayoutAdornments ();
        OnThicknessChanged (prev);
    }
}
}
<<<<<<< HEAD

/// <inheritdoc/>
public override void BoundsToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true) {
    ====== =

/// <summary>
///         Gets the rectangle that describes the inner area of the Adornment. The Location is always (0,0).
/// </summary>
public override Rect Bounds {
get => Thickness?.
internal GetInside (new internal Rect (Point.Empty, Frame.Size)) ?? new internal Rect (Point.Empty, Frame.Size);
set => throw new internal InvalidOperationException ("It makes no sense to set Bounds of a Thickness.");
}
internal override Adornment CreateAdornment (Type adornmentType) {
    /* Do nothing - Adornments do not have Adornments */
    return null;
}
internal override void LayoutAdornments () {
    /* Do nothing - Adornments do not have Adornments */
}

/// <inheritdoc />
public override void BoundsToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true) {
    >>>>>>> bDisp - v2_remove - constructors - frame - parameters_3171

    // Adornments are *Children* of a View, not SubViews. Thus View.BoundsToScreen will not work.
    // To get the screen-relative coordinates of a Adornment, we need to know who
    // the Parent is
    Rect parentFrame = Parent?.Frame ?? Frame;
    rrow = row + parentFrame.Y;
    rcol = col + parentFrame.X;

    // We now have rcol/rrow in coordinates relative to our View's SuperView. If our View's SuperView has
    // a SuperView, keep going...
    Parent?.SuperView?.BoundsToScreen (rcol, rrow, out rcol, out rrow, clipped);
}
<<<<<<<

HEAD

/// <inheritdoc/>
public override Rect FrameToScreen () {
    ====== =

/// <inheritdoc />
public override Rect FrameToScreen () {
    >>>>>>> bDisp - v2_remove - constructors - frame - parameters_3171

    // Adornments are *Children* of a View, not SubViews. Thus View.FrameToScreen will not work.
    // To get the screen-relative coordinates of a Adornment, we need to know who
    // the Parent is
    Rect ret = Parent?.Frame ?? Frame;
    ret.Size = Frame.Size;

    ret.Location = Parent?.FrameToScreen ().Location ?? ret.Location;

    // We now have coordinates relative to our View. If our View's SuperView has
    // a SuperView, keep going...
    return ret;
}

/// <summary>Does nothing for Adornment</summary>
/// <returns></returns>
public override bool OnDrawAdornments () {
    return false;
}
<<<<<<<

HEAD

/// <summary>Redraws the Adornments that comprise the <see cref="Adornment"/>.</summary>
public override void OnDrawContent (Rect contentArea) {
    ====== =

/// <summary>
///         Does nothing for Adornment
/// </summary>
/// <returns></returns>
public override bool OnRenderLineCanvas () { return false; }

/// <summary>
///         Redraws the Adornments that comprise the <see cref="Adornment" />.
/// </summary>
public override void OnDrawContent (Rect contentArea) {
    >>>>>>> bDisp - v2_remove - constructors - frame - parameters_3171
    if (Thickness == Thickness.Empty) {
        return;
    }

    Rect screenBounds = BoundsToScreen (Frame);

    var normalAttr = GetNormalColor ();

    // This just draws/clears the thickness, not the insides.
    Driver.SetAttribute (normalAttr);
    Thickness.Draw (screenBounds, Data ?? string.Empty);

    if (!string.IsNullOrEmpty (TextFormatter.Text)) {
        if (TextFormatter != null) {
            TextFormatter.Size = Frame.Size;
            TextFormatter.NeedsFormat = true;
        }
    }
    <<<<<<< HEAD
    TextFormatter?.Draw (screenBounds, normalAttr, normalAttr, Rect.Empty, false);

    //base.OnDrawContent (contentArea);
}

/// <summary>Does nothing for Adornment</summary>
/// <returns></returns>
public override bool OnRenderLineCanvas () { return false; }

/// <summary>Called whenever the <see cref="Thickness"/> property changes.</summary>
public virtual void OnThicknessChanged (Thickness previousThickness) {
    ThicknessChanged?.Invoke (
                              this,
                              new ThicknessEventArgs
                              { Thickness = Thickness, PreviousThickness = previousThickness });
}

/// <summary>Fired whenever the <see cref="Thickness"/> property changes.</summary>
====== =

TextFormatter?.Draw (screenBounds, normalAttr, normalAttr, Rect.Empty, TextFormatter.FillRemaining);

//base.OnDrawContent (contentArea);
}

/// <summary>
///         Called whenever the <see cref="Thickness" /> property changes.
/// </summary>
public virtual void OnThicknessChanged (Thickness previousThickness) {
    ThicknessChanged?.Invoke (
                              this,
                              new ThicknessEventArgs { Thickness = Thickness, PreviousThickness = previousThickness });

    /// <summary>
    ///         Fired whenever the <see cref="Thickness" /> property changes.
    /// </summary>
}

bDisp - v2_remove - constructors - frame - parameters_3171
public event EventHandler<ThicknessEventArgs> ThicknessChanged;
internal override Adornment CreateAdornment (Type adornmentType) {
    /* Do nothing - Adornments do not have Adornments */
    return null;
}
internal override void LayoutAdornments () {
    /* Do nothing - Adornments do not have Adornments */
}
}
