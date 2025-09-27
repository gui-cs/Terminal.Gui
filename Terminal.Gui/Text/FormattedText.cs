#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Terminal.Gui.Text;

/// <summary>
///     Represents the result of text formatting, containing formatted lines, size requirements, and metadata.
/// </summary>
public sealed class FormattedText
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FormattedText"/> class.
    /// </summary>
    /// <param name="lines">The formatted text lines.</param>
    /// <param name="requiredSize">The size required to display the text.</param>
    /// <param name="hotKey">The HotKey found in the text, if any.</param>
    /// <param name="hotKeyPosition">The position of the HotKey in the original text.</param>
    public FormattedText(
        IReadOnlyList<FormattedLine> lines,
        Size requiredSize,
        Key hotKey = default,
        int hotKeyPosition = -1)
    {
        Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        RequiredSize = requiredSize;
        HotKey = hotKey;
        HotKeyPosition = hotKeyPosition;
    }

    /// <summary>Gets the formatted text lines.</summary>
    public IReadOnlyList<FormattedLine> Lines { get; }

    /// <summary>Gets the size required to display the formatted text.</summary>
    public Size RequiredSize { get; }

    /// <summary>Gets the HotKey found in the text, if any.</summary>
    public Key HotKey { get; }

    /// <summary>Gets the position of the HotKey in the original text (-1 if no HotKey).</summary>
    public int HotKeyPosition { get; }

    /// <summary>Gets a value indicating whether the text contains a HotKey.</summary>
    public bool HasHotKey => HotKeyPosition >= 0;
}

/// <summary>
///     Represents a single formatted line of text.
/// </summary>
public sealed class FormattedLine
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FormattedLine"/> class.
    /// </summary>
    /// <param name="runs">The text runs that make up this line.</param>
    /// <param name="width">The display width of this line.</param>
    public FormattedLine(IReadOnlyList<FormattedRun> runs, int width)
    {
        Runs = runs;
        Width = width;
    }

    /// <summary>Gets the text runs that make up this line.</summary>
    public IReadOnlyList<FormattedRun> Runs { get; }

    /// <summary>Gets the display width of this line.</summary>
    public int Width { get; }
}

/// <summary>
///     Represents a run of text with consistent formatting.
/// </summary>
public sealed class FormattedRun
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FormattedRun"/> class.
    /// </summary>
    /// <param name="text">The text content of this run.</param>
    /// <param name="isHotKey">Whether this run represents a HotKey.</param>
    public FormattedRun(string text, bool isHotKey = false)
    {
        Text = text;
        IsHotKey = isHotKey;
    }

    /// <summary>Gets the text content of this run.</summary>
    public string Text { get; }

    /// <summary>Gets a value indicating whether this run represents a HotKey.</summary>
    public bool IsHotKey { get; }
}