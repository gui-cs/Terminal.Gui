﻿namespace Terminal.Gui;

// TODO: v2 - Missing 3D effect - 3D effects will be drawn by a mechanism separate from Adornments
// TODO: v2 - If a Adornment has focus, navigation keys (e.g Command.NextView) should cycle through SubViews of the Adornments
// QUESTION: How does a user navigate out of an Adornment to another Adornment, or back into the Parent's SubViews?

/// <summary>
///     Adornments are a special form of <see cref="View"/> that appear outside of the <see cref="View.ContentArea"/>:
///     <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. They are defined using the
///     <see cref="Thickness"/> class, which specifies the thickness of the sides of a rectangle.
/// </summary>
/// <remarsk>
///     <para>
///         There is no prevision for creating additional subclasses of Adornment. It is not abstract to enable unit
///         testing.
///     </para>
///     <para>Each of <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> can be customized.</para>
/// </remarsk>
public class Adornment : View
{
    private Thickness _thickness = Thickness.Empty;

    /// <inheritdoc/>
    public Adornment ()
    {
        /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <summary>Constructs a new adornment for the view specified by <paramref name="parent"/>.</summary>
    /// <param name="parent"></param>
    public Adornment (View parent) { Parent = parent; }

    /// <summary>
    ///     Gets the rectangle that describes the area of the Adornment. The Location is always (0,0).
    ///     The size is the size of the Frame 
    /// </summary>
    public override Rectangle ContentArea
    {
        get => Frame with { Location = Point.Empty };
        set => throw new InvalidOperationException ("It makes no sense to set Bounds of a Thickness.");
    }

    /// <summary>The Parent of this Adornment (the View this Adornment surrounds).</summary>
    /// <remarks>
    ///     Adornments are distinguished from typical View classes in that they are not sub-views, but have a parent/child
    ///     relationship with their containing View.
    /// </remarks>
    public View Parent { get; set; }

    /// <summary>
    ///     Adornments cannot be used as sub-views (see <see cref="Parent"/>); this method always throws an
    ///     <see cref="InvalidOperationException"/>. TODO: Are we sure?
    /// </summary>
    public override View SuperView
    {
        get => null;
        set => throw new NotImplementedException ();
    }

    /// <summary>
    ///     Adornments only render to their <see cref="Parent"/>'s or Parent's SuperView's LineCanvas, so setting this
    ///     property throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    public override bool SuperViewRendersLineCanvas
    {
        get => false; // throw new NotImplementedException ();
        set => throw new NotImplementedException ();
    }

    /// <summary>Defines the rectangle that the <see cref="Adornment"/> will use to draw its content.</summary>
    public Thickness Thickness
    {
        get => _thickness;
        set
        {
            Thickness prev = _thickness;
            _thickness = value;

            if (prev != _thickness)
            {
                Parent?.LayoutAdornments ();
                OnThicknessChanged (prev);
            }
        }
    }

    /// <inheritdoc/>
    public override Rectangle FrameToScreen ()
    {
        if (Parent is null)
        {
            return Frame;
        }

        // Adornments are *Children* of a View, not SubViews. Thus View.FrameToScreen will not work.
        // To get the screen-relative coordinates of an Adornment, we need get the parent's Frame
        // in screen coords, and add our Frame location to it.
        Rectangle parent = Parent.FrameToScreen ();

        return new (new (parent.X + Frame.X, parent.Y + Frame.Y), Frame.Size);
    }

    /// <summary>
    ///     Gets the rectangle that describes the inner area of the Adornment. The Location is always (0,0).
    /// </summary>
    public override Rectangle GetVisibleContentArea ()
    {
        return Thickness?.GetInside (new Rectangle (Point.Empty, Frame.Size)) ?? new Rectangle (Point.Empty, Frame.Size);
    }

    /// <summary>Does nothing for Adornment</summary>
    /// <returns></returns>
    public override bool OnDrawAdornments () { return false; }

    /// <summary>Redraws the Adornments that comprise the <see cref="Adornment"/>.</summary>
    public override void OnDrawContent (Rectangle contentArea)
    {
        if (Thickness == Thickness.Empty)
        {
            return;
        }

        Rectangle screenBounds = BoundsToScreen (contentArea);
        Attribute normalAttr = GetNormalColor ();
        Driver.SetAttribute (normalAttr);

        // This just draws/clears the thickness, not the insides.
        Thickness.Draw (screenBounds, ToString ());

        if (!string.IsNullOrEmpty (TextFormatter.Text))
        {
            if (TextFormatter is { })
            {
                TextFormatter.Size = Frame.Size;
                TextFormatter.NeedsFormat = true;
            }
        }

        TextFormatter?.Draw (screenBounds, normalAttr, normalAttr, Rectangle.Empty);

        LayoutSubviews ();

        base.OnDrawContent (contentArea);

        ClearLayoutNeeded ();
        ClearNeedsDisplay ();
    }

    /// <summary>Does nothing for Adornment</summary>
    /// <returns></returns>
    public override bool OnRenderLineCanvas () { return false; }

    /// <summary>Called whenever the <see cref="Thickness"/> property changes.</summary>
    public virtual void OnThicknessChanged (Thickness previousThickness)
    {
        ThicknessChanged?.Invoke (
                                  this,
                                  new() { Thickness = Thickness, PreviousThickness = previousThickness }
                                 );
    }

    /// <summary>Fired whenever the <see cref="Thickness"/> property changes.</summary>
    public event EventHandler<ThicknessEventArgs> ThicknessChanged;

    internal override Adornment CreateAdornment (Type adornmentType)
    {
        /* Do nothing - Adornments do not have Adornments */
        return null;
    }

    internal override void LayoutAdornments ()
    {
        /* Do nothing - Adornments do not have Adornments */
    }
}
