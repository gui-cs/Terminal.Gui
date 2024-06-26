﻿namespace Terminal.Gui;

/// <summary>Specifies the style that a <see cref="ProgressBar"/> uses to indicate the progress of an operation.</summary>
public enum ProgressBarStyle
{
    /// <summary>Indicates progress by increasing the number of segmented blocks in a <see cref="ProgressBar"/>.</summary>
    Blocks,

    /// <summary>Indicates progress by increasing the size of a smooth, continuous bar in a <see cref="ProgressBar"/>.</summary>
    Continuous,

    /// <summary>Indicates progress by continuously scrolling a block across a <see cref="ProgressBar"/> in a marquee fashion.</summary>
    MarqueeBlocks,

    /// <summary>Indicates progress by continuously scrolling a block across a <see cref="ProgressBar"/> in a marquee fashion.</summary>
    MarqueeContinuous
}

/// <summary>Specifies the format that a <see cref="ProgressBar"/> uses to indicate the visual presentation.</summary>
public enum ProgressBarFormat
{
    /// <summary>A simple visual presentation showing only the progress bar.</summary>
    Simple,

    /// <summary>A simple visual presentation showing the progress bar overlaid with the percentage.</summary>
    SimplePlusPercentage
}

/// <summary>A Progress Bar view that can indicate progress of an activity visually.</summary>
/// <remarks>
///     <para>
///         <see cref="ProgressBar"/> can operate in two modes, percentage mode, or activity mode. The progress bar
///         starts in percentage mode and setting the Fraction property will reflect on the UI the progress made so far.
///         Activity mode is used when the application has no way of knowing how much time is left, and is started when the
///         <see cref="Pulse"/> method is called. Call <see cref="Pulse"/> repeatedly as progress is made.
///     </para>
/// </remarks>
public class ProgressBar : View, IDisignable
{
    private int [] _activityPos;
    private bool _bidirectionalMarquee = true;
    private int _delta;
    private float _fraction;
    private bool _isActivity;
    private ProgressBarStyle _progressBarStyle = ProgressBarStyle.Blocks;
    private ProgressBarFormat _progressBarFormat = ProgressBarFormat.Simple;
    private Rune _segmentCharacter = Glyphs.BlocksMeterSegment;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProgressBar"/> class, starts in percentage mode and uses relative
    ///     layout.
    /// </summary>
    public ProgressBar () { SetInitialProperties (); }

    /// <summary>
    ///     Specifies if the <see cref="ProgressBarStyle.MarqueeBlocks"/> or the
    ///     <see cref="ProgressBarStyle.MarqueeContinuous"/> styles is unidirectional or bidirectional.
    /// </summary>
    public bool BidirectionalMarquee
    {
        get => _bidirectionalMarquee;
        set => _bidirectionalMarquee = value;
    }

    /// <summary>Gets or sets the <see cref="ProgressBar"/> fraction to display, must be a value between 0 and 1.</summary>
    /// <value>The fraction representing the progress.</value>
    public float Fraction
    {
        get => _fraction;
        set
        {
            _fraction = Math.Min (value, 1);
            _isActivity = false;
        }
    }

    /// <summary>Specifies the format that a <see cref="ProgressBar"/> uses to indicate the visual presentation.</summary>
    public ProgressBarFormat ProgressBarFormat
    {
        get => _progressBarFormat;
        set => _progressBarFormat = value;
    }

    /// <summary>Gets/Sets the progress bar style based on the <see cref="Terminal.Gui.ProgressBarStyle"/></summary>
    public ProgressBarStyle ProgressBarStyle
    {
        get => _progressBarStyle;
        set
        {
            _progressBarStyle = value;

            switch (value)
            {
                case ProgressBarStyle.Blocks:
                    SegmentCharacter = Glyphs.BlocksMeterSegment;

                    break;
                case ProgressBarStyle.Continuous:
                    SegmentCharacter = Glyphs.ContinuousMeterSegment;

                    break;
                case ProgressBarStyle.MarqueeBlocks:
                    SegmentCharacter = Glyphs.BlocksMeterSegment;

                    break;
                case ProgressBarStyle.MarqueeContinuous:
                    SegmentCharacter = Glyphs.ContinuousMeterSegment;

                    break;
            }
        }
    }

    /// <summary>Segment indicator for meter views.</summary>
    public Rune SegmentCharacter
    {
        get => _segmentCharacter;
        set => _segmentCharacter = value;
    }

    /// <summary>
    ///     Gets or sets the text displayed on the progress bar. If set to an empty string and
    ///     <see cref="ProgressBarFormat"/> is <see cref="ProgressBarFormat.SimplePlusPercentage"/> the percentage will be
    ///     displayed. If <see cref="ProgressBarStyle"/> is a marquee style, the text will be displayed.
    /// </summary>
    public override string Text
    {
        get => string.IsNullOrEmpty (base.Text) ? $"{_fraction * 100:F0}%" : base.Text;
        set
        {
            if (ProgressBarStyle == ProgressBarStyle.MarqueeBlocks
                || ProgressBarStyle == ProgressBarStyle.MarqueeContinuous)
            {
                base.Text = value;
            }
        }
    }

    ///<inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        Driver.SetAttribute (GetHotNormalColor ());

        Move (0, 0);

        if (_isActivity)
        {
            for (var i = 0; i < Viewport.Width; i++)
            {
                if (Array.IndexOf (_activityPos, i) != -1)
                {
                    Driver.AddRune (SegmentCharacter);
                }
                else
                {
                    Driver.AddRune ((Rune)' ');
                }
            }
        }
        else
        {
            var mid = (int)(_fraction * Viewport.Width);
            int i;

            for (i = 0; (i < mid) & (i < Viewport.Width); i++)
            {
                Driver.AddRune (SegmentCharacter);
            }

            for (; i < Viewport.Width; i++)
            {
                Driver.AddRune ((Rune)' ');
            }
        }

        if (ProgressBarFormat != ProgressBarFormat.Simple && !_isActivity)
        {
            var tf = new TextFormatter { Alignment = Alignment.Center, Text = Text, AutoSize = true };
            var attr = new Attribute (ColorScheme.HotNormal.Foreground, ColorScheme.HotNormal.Background);

            if (_fraction > .5)
            {
                attr = new Attribute (ColorScheme.HotNormal.Background, ColorScheme.HotNormal.Foreground);
            }

            tf?.Draw (
                      ViewportToScreen (Viewport),
                      attr,
                      ColorScheme.Normal,
                      SuperView?.ViewportToScreen (SuperView.Viewport) ?? default (Rectangle)
                     );
        }
    }

    /// <summary>Notifies the <see cref="ProgressBar"/> that some progress has taken place.</summary>
    /// <remarks>
    ///     If the <see cref="ProgressBar"/> is percentage mode, it switches to activity mode. If is in activity mode, the
    ///     marker is moved.
    /// </remarks>
    public void Pulse ()
    {
        if (_activityPos is null || _activityPos.Length == 0)
        {
            PopulateActivityPos ();
        }

        if (_activityPos!.Length == 0)
        {
            return;
        }

        if (!_isActivity)
        {
            _isActivity = true;
            _delta = 1;
        }
        else
        {
            for (var i = 0; i < _activityPos.Length; i++)
            {
                _activityPos [i] += _delta;
            }

            if (_activityPos [^1] < 0)
            {
                for (var i = 0; i < _activityPos.Length; i++)
                {
                    _activityPos [i] = i - _activityPos.Length + 2;
                }

                _delta = 1;
            }
            else if (_activityPos [0] >= Viewport.Width)
            {
                if (_bidirectionalMarquee)
                {
                    for (var i = 0; i < _activityPos.Length; i++)
                    {
                        _activityPos [i] = Viewport.Width + i - 2;
                    }

                    _delta = -1;
                }
                else
                {
                    PopulateActivityPos ();
                }
            }
        }

        SetNeedsDisplay ();
    }

    private void PopulateActivityPos ()
    {
        _activityPos = new int [Math.Min (Frame.Width / 3, 5)];

        for (var i = 0; i < _activityPos.Length; i++)
        {
            _activityPos [i] = i - _activityPos.Length + 1;
        }
    }

    private void ProgressBar_Initialized (object sender, EventArgs e)
    {
        ColorScheme = new ColorScheme (ColorScheme ?? SuperView?.ColorScheme ?? Colors.ColorSchemes ["Base"])
        {
            HotNormal = new Attribute (Color.BrightGreen, Color.Gray)
        };
    }

    private void SetInitialProperties ()
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: 1);
        CanFocus = false;
        _fraction = 0;
        Initialized += ProgressBar_Initialized;
    }

    /// <inheritdoc />
    public bool LoadDemoData (object ctx = null)
    {
        Width = Dim.Fill ();
        Height = Dim.Auto (DimAutoStyle.Text, minimumContentDim: 1);
        Fraction = 0.75f;
        return true;
    }
}
