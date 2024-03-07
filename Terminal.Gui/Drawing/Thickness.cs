using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Describes the thickness of a frame around a rectangle. Four <see cref="int"/> values describe the
///     <see cref="Left"/>, <see cref="Top"/>, <see cref="Right"/>, and <see cref="Bottom"/> sides of the rectangle,
///     respectively.
/// </summary>
/// <remarks>
///     <para>
///         Use the helper API (<see cref="GetInside(Rectangle)"/> to get the rectangle describing the insides of the frame,
///         with the thickness widths subtracted.
///     </para>
///     <para>Use the helper API (<see cref="Draw(Rectangle, string)"/> to draw the frame with the specified thickness.</para>
/// </remarks>
public class Thickness : IEquatable<Thickness>
{
    /// <summary>Gets or sets the width of the lower side of the rectangle.</summary>
    [JsonInclude]
    public int Bottom;

    /// <summary>Gets or sets the width of the left side of the rectangle.</summary>
    [JsonInclude]
    public int Left;

    /// <summary>Gets or sets the width of the right side of the rectangle.</summary>
    [JsonInclude]
    public int Right;

    /// <summary>Gets or sets the width of the upper side of the rectangle.</summary>
    [JsonInclude]
    public int Top;

    /// <summary>Initializes a new instance of the <see cref="Thickness"/> class with all widths set to 0.</summary>
    public Thickness () { }

    /// <summary>Initializes a new instance of the <see cref="Thickness"/> class with a uniform width to each side.</summary>
    /// <param name="width"></param>
    public Thickness (int width) : this (width, width, width, width) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Thickness"/> class that has specific widths applied to each side
    ///     of the rectangle.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    /// <param name="bottom"></param>
    public Thickness (int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    // TODO: add operator overloads
    /// <summary>Gets an empty thickness.</summary>
    public static Thickness Empty => new (0);

    /// <summary>
    ///     Gets the total width of the left and right sides of the rectangle. Sets the width of the left and rigth sides
    ///     of the rectangle to half the specified value.
    /// </summary>
    public int Horizontal
    {
        get => Left + Right;
        set => Left = Right = value / 2;
    }

    /// <summary>
    ///     Gets the total height of the top and bottom sides of the rectangle. Sets the height of the top and bottom
    ///     sides of the rectangle to half the specified value.
    /// </summary>
    public int Vertical
    {
        get => Top + Bottom;
        set => Top = Bottom = value / 2;
    }

    // IEquitable
    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other"></param>
    /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
    public bool Equals (Thickness other) { return other is { } && Left == other.Left && Right == other.Right && Top == other.Top && Bottom == other.Bottom; }

    /// <summary>
    ///     Gets whether the specified coordinates lie within the thickness (inside the bounding rectangle but outside of
    ///     the rectangle described by <see cref="GetInside(Rectangle)"/>.
    /// </summary>
    /// <param name="outside">Describes the location and size of the rectangle that contains the thickness.</param>
    /// <param name="x">The x coord to check.</param>
    /// <param name="y">The y coord to check.</param>
    /// <returns><see langword="true"/> if the specified coordinate is within the thickness; <see langword="false"/> otherwise.</returns>
    public bool Contains (Rectangle outside, int x, int y)
    {
        Rectangle inside = GetInside (outside);

        return outside.Contains (x, y) && !inside.Contains (x, y);
    }

    /// <summary>
    /// Adds the thickness widths of another <see cref="Thickness"/> to the current <see cref="Thickness"/>, returning a new <see cref="Thickness"/>.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Thickness Add (Thickness other)
    {
        return new Thickness (Left + other.Left, Top + other.Top, Right + other.Right, Bottom + other.Bottom);
    }

    /// <summary>
    /// Adds the thickness widths of another <see cref="Thickness"/> to another <see cref="Thickness"/>.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Thickness operator + (Thickness a, Thickness b) { return a.Add (b); }

    /// <summary>Draws the <see cref="Thickness"/> rectangle with an optional diagnostics label.</summary>
    /// <remarks>
    ///     If <see cref="ConsoleDriver.DiagnosticFlags"/> is set to
    ///     <see cref="ConsoleDriver.DiagnosticFlags.FramePadding"/> then 'T', 'L', 'R', and 'B' glyphs will be used instead of
    ///     space. If <see cref="ConsoleDriver.DiagnosticFlags"/> is set to
    ///     <see cref="ConsoleDriver.DiagnosticFlags.FrameRuler"/> then a ruler will be drawn on the outer edge of the
    ///     Thickness.
    /// </remarks>
    /// <param name="rect">The location and size of the rectangle that bounds the thickness rectangle, in screen coordinates.</param>
    /// <param name="label">The diagnostics label to draw on the bottom of the <see cref="Bottom"/>.</param>
    /// <returns>The inner rectangle remaining to be drawn.</returns>
    public Rectangle Draw (Rectangle rect, string label = null)
    {
        if (rect.Size.Width < 1 || rect.Size.Height < 1)
        {
            return Rectangle.Empty;
        }

        var clearChar = (Rune)' ';
        Rune leftChar = clearChar;
        Rune rightChar = clearChar;
        Rune topChar = clearChar;
        Rune bottomChar = clearChar;

        if ((ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FramePadding)
            == ConsoleDriver.DiagnosticFlags.FramePadding)
        {
            leftChar = (Rune)'L';
            rightChar = (Rune)'R';
            topChar = (Rune)'T';
            bottomChar = (Rune)'B';

            if (!string.IsNullOrEmpty (label))
            {
                leftChar = rightChar = bottomChar = topChar = (Rune)label [0];
            }
        }

        // Draw the Top side
        if (Top > 0)
        {
            Application.Driver.FillRect (rect with { Height = Math.Min (rect.Height, Top) }, topChar);
        }

        // Draw the Left side
        if (Left > 0)
        {
            Application.Driver.FillRect (rect with { Width = Math.Min (rect.Width, Left) }, leftChar);
        }

        // Draw the Right side
        if (Right > 0)
        {
            Application.Driver.FillRect (
                                         rect with
                                         {
                                             X = Math.Max (0, rect.X + rect.Width - Right),
                                             Width = Math.Min (rect.Width, Right)
                                         },
                                         rightChar
                                        );
        }

        // Draw the Bottom side
        if (Bottom > 0)
        {
            Application.Driver.FillRect (
                                         rect with
                                         {
                                             Y = rect.Y + Math.Max (0, rect.Height - Bottom),
                                             Height = Bottom
                                         },
                                         bottomChar
                                        );
        }

        // TODO: This should be moved to LineCanvas as a new LineStyle.Ruler
        if ((ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FrameRuler)
            == ConsoleDriver.DiagnosticFlags.FrameRuler)
        {
            // PERF: This can almost certainly be simplified down to a single point offset and fewer calls to Draw
            // Top
            var hruler = new Ruler { Length = rect.Width, Orientation = Orientation.Horizontal };

            if (Top > 0)
            {
                hruler.Draw (rect.Location);
            }

            //Left
            var vruler = new Ruler { Length = rect.Height - 2, Orientation = Orientation.Vertical };

            if (Left > 0)
            {
                vruler.Draw (rect.Location with { Y = rect.Y + 1 }, 1);
            }

            // Bottom
            if (Bottom > 0)
            {
                hruler.Draw (rect.Location with { Y = rect.Y + rect.Height - 1 });
            }

            // Right
            if (Right > 0)
            {
                vruler.Draw (new (rect.X + rect.Width - 1, rect.Y + 1), 1);
            }
        }

        if ((ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FramePadding)
            == ConsoleDriver.DiagnosticFlags.FramePadding)
        {
            // Draw the diagnostics label on the bottom
            var tf = new TextFormatter
            {
                Text = label is null ? string.Empty : $"{label} {this}",
                Alignment = TextAlignment.Centered,
                VerticalAlignment = VerticalTextAlignment.Bottom
            };
            tf.Draw (rect, Application.Driver.CurrentAttribute, Application.Driver.CurrentAttribute, rect);
        }

        return GetInside (rect);
    }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
    public override bool Equals (object obj)
    {
        //Check for null and compare run-time types.
        if (obj is null || !GetType ().Equals (obj.GetType ()))
        {
            return false;
        }

        return Equals ((Thickness)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode ()
    {
        var hashCode = 1380952125;
        hashCode = hashCode * -1521134295 + Left.GetHashCode ();
        hashCode = hashCode * -1521134295 + Right.GetHashCode ();
        hashCode = hashCode * -1521134295 + Top.GetHashCode ();
        hashCode = hashCode * -1521134295 + Bottom.GetHashCode ();

        return hashCode;
    }

    /// <summary>
    ///     Returns a rectangle describing the location and size of the inside area of <paramref name="rect"/> with the
    ///     thickness widths subtracted. The height and width of the returned rectangle will never be less than 0.
    /// </summary>
    /// <remarks>
    ///     If a thickness width is negative, the inside rectangle will be larger than <paramref name="rect"/>. e.g. a
    ///     <c>
    ///         Thickness (-1, -1, -1, -1) will result in a rectangle skewed -1 in the X and Y directions and with a Size
    ///         increased by 1.
    ///     </c>
    /// </remarks>
    /// <param name="rect">The source rectangle</param>
    /// <returns></returns>
    public Rectangle GetInside (Rectangle rect)
    {
        int x = rect.X + Left;
        int y = rect.Y + Top;
        int width = Math.Max (0, rect.Size.Width - Horizontal);
        int height = Math.Max (0, rect.Size.Height - Vertical);

        return new (x, y, width, height);
    }

    /// <inheritdoc/>
    public static bool operator == (Thickness left, Thickness right) { return EqualityComparer<Thickness>.Default.Equals (left, right); }

    /// <inheritdoc/>
    public static bool operator != (Thickness left, Thickness right) { return !(left == right); }

    /// <summary>Returns the thickness widths of the Thickness formatted as a string.</summary>
    /// <returns>The thickness widths as a string.</returns>
    public override string ToString () { return $"(Left={Left},Top={Top},Right={Right},Bottom={Bottom})"; }

    private int validate (int width)
    {
        if (width < 0)
        {
            throw new ArgumentException ("Thickness widths cannot be negative.");
        }

        return width;
    }
}
