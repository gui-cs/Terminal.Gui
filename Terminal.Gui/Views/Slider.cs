using System.Transactions;

namespace Terminal.Gui;

/// <summary>Slider control.</summary>
public class Slider : Slider<object>
{
    /// <summary>Initializes a new instance of the <see cref="Slider"/> class.</summary>
    public Slider () { }

    /// <summary>Initializes a new instance of the <see cref="Slider"/> class.</summary>
    /// <param name="options">Initial slider options.</param>
    /// <param name="orientation">Initial slider options.</param>
    public Slider (List<object> options, Orientation orientation = Orientation.Horizontal) :
        base (options, orientation)
    { }
}

/// <summary>
///     Provides a slider control letting the user navigate from a set of typed options in a linear manner using the
///     keyboard or mouse.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Slider<T> : View, IOrientation
{
    private readonly SliderConfiguration _config = new ();

    // List of the current set options.
    private readonly List<int> _setOptions = new ();

    // Options
    private List<SliderOption<T>> _options;

    private OrientationHelper _orientationHelper;

    #region Initialize

    private void SetInitialProperties (
        List<SliderOption<T>> options,
        Orientation orientation = Orientation.Horizontal
    )
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;

        _options = options ?? new List<SliderOption<T>> ();

        _orientationHelper = new (this);
        _orientationHelper.Orientation = _config._sliderOrientation = orientation;
        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);

        SetDefaultStyle ();
        SetCommands ();
        SetContentSize ();

        // BUGBUG: This should not be needed - Need to ensure SetRelativeLayout gets called during EndInit
        Initialized += (s, e) => { SetContentSize (); };

        LayoutStarted += (s, e) => { SetContentSize (); };
    }

    // TODO: Make configurable via ConfigurationManager
    private void SetDefaultStyle ()
    {
        _config._showLegends = true;

        switch (_config._sliderOrientation)
        {
            case Orientation.Horizontal:
                Style.SpaceChar = new () { Rune = Glyphs.HLine }; // '─'
                Style.OptionChar = new () { Rune = Glyphs.BlackCircle }; // '┼●🗹□⏹'

                break;
            case Orientation.Vertical:
                Style.SpaceChar = new () { Rune = Glyphs.VLine };
                Style.OptionChar = new () { Rune = Glyphs.BlackCircle };

                break;
        }

        // TODO(jmperricone) Wide Vertical ???
        /*
         │
         │
         ┼─ 40
         │
         │
        ███ 30
        ▒▒▒
        ▒▒▒
        ▒▒▒ 20
        ▒▒▒
        ▒▒▒
        ███ 10
         │
         │
        ─●─ 0
        */

        _config._legendsOrientation = _config._sliderOrientation;
        Style.EmptyChar = new () { Rune = new (' ') };
        Style.SetChar = new () { Rune = Glyphs.ContinuousMeterSegment }; // ■
        Style.RangeChar = new () { Rune = Glyphs.Stipple }; // ░ ▒ ▓   // Medium shade not blinking on curses.
        Style.StartRangeChar = new () { Rune = Glyphs.ContinuousMeterSegment };
        Style.EndRangeChar = new () { Rune = Glyphs.ContinuousMeterSegment };
        Style.DragChar = new () { Rune = Glyphs.Diamond };

        // TODO: Support left & right (top/bottom)
        // First = '├',
        // Last = '┤',
    }

    #endregion

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="Slider"/> class.</summary>
    public Slider () : this (new ()) { }

    /// <summary>Initializes a new instance of the <see cref="Slider"/> class.</summary>
    /// <param name="options">Initial slider options.</param>
    /// <param name="orientation">Initial slider orientation.</param>
    public Slider (List<T> options, Orientation orientation = Orientation.Horizontal)
    {
        if (options is null)
        {
            SetInitialProperties (null, orientation);
        }
        else
        {
            SetInitialProperties (
                                  options.Select (
                                                  e =>
                                                  {
                                                      var legend = e.ToString ();

                                                      return new SliderOption<T>
                                                      {
                                                          Data = e,
                                                          Legend = legend,
                                                          LegendAbbr =
                                                              (Rune)(legend?.Length > 0 ? legend [0] : ' ')
                                                      };
                                                  }
                                                 )
                                         .ToList (),
                                  orientation
                                 );
        }
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Setting the Text of a slider is a shortcut to setting options. The text is a CSV string of the options.
    /// </summary>
    public override string Text
    {
        get
        {
            if (_options.Count == 0)
            {
                return string.Empty;
            }

            // Return labels as a CSV string
            return string.Join (",", _options);
        }
        set
        {
            if (string.IsNullOrEmpty (value))
            {
                Options = [];
            }
            else
            {
                IEnumerable<string> list = value.Split (',').Select (x => x.Trim ());
                Options = list.Select (x => new SliderOption<T> { Legend = x }).ToList ();
            }
        }
    }

    /// <summary>Allow no selection.</summary>
    public bool AllowEmpty
    {
        get => _config._allowEmpty;
        set
        {
            _config._allowEmpty = value;

            if (!value && _options.Count > 0 && _setOptions.Count == 0)
            {
                SetOption (0);
            }
        }
    }

    /// <summary>Gets or sets the minimum number of rows/columns between <see cref="Options"/>. The default is 1.</summary>
    public int MinimumInnerSpacing
    {
        get => _config._minInnerSpacing;
        set
        {
            _config._minInnerSpacing = value;

            SetContentSize ();
        }
    }

    /// <summary>Slider Type. <see cref="SliderType"></see></summary>
    public SliderType Type
    {
        get => _config._type;
        set
        {
            _config._type = value;

            // Todo: Custom logic to preserve options.
            _setOptions.Clear ();
            SetNeedsDisplay ();
        }
    }


    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/>. The default is <see cref="Orientation.Horizontal"/>.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    #region IOrientation members

    /// <inheritdoc />
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanging;

    /// <inheritdoc />
    public event EventHandler<EventArgs<Orientation>> OrientationChanged;

    /// <inheritdoc />
    public void OnOrientationChanged (Orientation newOrientation)
    {
        _config._sliderOrientation = newOrientation;

        switch (_config._sliderOrientation)
        {
            case Orientation.Horizontal:
                Style.SpaceChar = new () { Rune = Glyphs.HLine }; // '─'

                break;
            case Orientation.Vertical:
                Style.SpaceChar = new () { Rune = Glyphs.VLine };

                break;
        }

        SetKeyBindings ();
        SetContentSize ();
    }
    #endregion

    /// <summary>Legends Orientation. <see cref="Gui.Orientation"></see></summary>
    public Orientation LegendsOrientation
    {
        get => _config._legendsOrientation;
        set
        {
            _config._legendsOrientation = value;

            SetContentSize ();
        }
    }

    /// <summary>Slider styles. <see cref="SliderStyle"></see></summary>
    public SliderStyle Style { get; set; } = new ();

    /// <summary>Set the slider options.</summary>
    public List<SliderOption<T>> Options
    {
        get =>
            _options;
        set
        {
            // _options should never be null
            _options = value ?? throw new ArgumentNullException (nameof (value));

            if (_options.Count == 0)
            {
                return;
            }

            SetContentSize ();
        }
    }

    /// <summary>Allow range start and end be in the same option, as a single option.</summary>
    public bool RangeAllowSingle
    {
        get => _config._rangeAllowSingle;
        set => _config._rangeAllowSingle = value;
    }

    /// <summary>Show/Hide spacing before and after the first and last option.</summary>
    public bool ShowEndSpacing
    {
        get => _config._showEndSpacing;
        set
        {
            _config._showEndSpacing = value;
            SetContentSize ();
        }
    }

    /// <summary>Show/Hide the options legends.</summary>
    public bool ShowLegends
    {
        get => _config._showLegends;
        set
        {
            _config._showLegends = value;
            SetContentSize ();
        }
    }

    /// <summary>
    ///     Gets or sets whether the minimum or ideal size will be used when calculating the size of the slider.
    /// </summary>
    public bool UseMinimumSize
    {
        get => _config._useMinimumSize;
        set
        {
            _config._useMinimumSize = value;
            SetContentSize ();
        }
    }

    #endregion

    #region Events

    /// <summary>Event raised when the slider option/s changed. The dictionary contains: key = option index, value = T</summary>
    public event EventHandler<SliderEventArgs<T>> OptionsChanged;

    /// <summary>Overridable method called when the slider options have changed. Raises the <see cref="OptionsChanged"/> event.</summary>
    public virtual void OnOptionsChanged ()
    {
        OptionsChanged?.Invoke (this, new (GetSetOptionDictionary ()));
        SetNeedsDisplay ();
    }

    /// <summary>Event raised When the option is hovered with the keys or the mouse.</summary>
    public event EventHandler<SliderEventArgs<T>> OptionFocused;

    private int
        _lastFocusedOption; // for Range type; the most recently focused option. Used to determine shrink direction

    /// <summary>Overridable function that fires the <see cref="OptionFocused"/> event.</summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> if the focus change was cancelled.</returns>
    /// <param name="newFocusedOption"></param>
    public virtual bool OnOptionFocused (int newFocusedOption, SliderEventArgs<T> args)
    {
        if (newFocusedOption > _options.Count - 1 || newFocusedOption < 0)
        {
            return true;
        }

        OptionFocused?.Invoke (this, args);

        if (!args.Cancel)
        {
            _lastFocusedOption = FocusedOption;
            FocusedOption = newFocusedOption;

            //PositionCursor ();
        }

        return args.Cancel;
    }

    #endregion Events

    #region Public Methods

    /// <summary>The focused option (has the cursor).</summary>
    public int FocusedOption { get; set; }

    /// <summary>Causes the specified option to be set and be focused.</summary>
    public bool SetOption (int optionIndex)
    {
        // TODO: Handle range type.
        // Note: Maybe return false only when optionIndex doesn't exist, otherwise true.

        if (!_setOptions.Contains (optionIndex) && optionIndex >= 0 && optionIndex < _options.Count)
        {
            FocusedOption = optionIndex;
            SetFocusedOption ();

            return true;
        }

        return false;
    }

    /// <summary>Causes the specified option to be un-set and be focused.</summary>
    public bool UnSetOption (int optionIndex)
    {
        if (!AllowEmpty && _setOptions.Count > 2 && _setOptions.Contains (optionIndex))
        {
            FocusedOption = optionIndex;
            SetFocusedOption ();

            return true;
        }

        return false;
    }

    /// <summary>Get the indexes of the set options.</summary>
    public List<int> GetSetOptions ()
    {
        // Copy
        return _setOptions.OrderBy (e => e).ToList ();
    }

    #endregion Public Methods

    #region Helpers

    private void MoveAndAdd (int x, int y, Rune rune)
    {
        Move (x, y);
        Driver?.AddRune (rune);
    }

    private void MoveAndAdd (int x, int y, string str)
    {
        Move (x, y);
        Driver?.AddStr (str);
    }

    /// <summary>Sets the dimensions of the Slider to the ideal values.</summary>
    private void SetContentSize ()
    {
        if (_options.Count == 0)
        {
            return;
        }

        bool horizontal = _config._sliderOrientation == Orientation.Horizontal;

        if (UseMinimumSize)
        {
            CalcSpacingConfig (CalcMinLength ());
        }
        else
        {
            CalcSpacingConfig (horizontal ? Viewport.Width : Viewport.Height);
        }

        SetContentSize (new (GetIdealWidth (), GetIdealHeight ()));

        return;

        void CalcSpacingConfig (int size)
        {
            _config._cachedInnerSpacing = 0;
            _config._startSpacing = 0;
            _config._endSpacing = 0;

            int maxLegend; // Because the legends are centered, the longest one determines inner spacing

            if (_config._sliderOrientation == _config._legendsOrientation)
            {
                maxLegend = int.Max (_options.Max (s => s.Legend?.GetColumns () ?? 1), 1);
            }
            else
            {
                maxLegend = 1;
            }

            int minSizeThatFitsLegends = _options.Count == 1 ? maxLegend : _options.Sum (o => o.Legend.GetColumns ());

            string first;
            string last;

            if (minSizeThatFitsLegends > size)
            {
                _config._showLegendsAbbr = false;

                if (_config._sliderOrientation == _config._legendsOrientation)
                {
                    _config._showLegendsAbbr = true;

                    foreach (SliderOption<T> o in _options.Where (op => op.LegendAbbr == default (Rune)))
                    {
                        o.LegendAbbr = (Rune)(o.Legend?.GetColumns () > 0 ? o.Legend [0] : ' ');
                    }
                }

                first = "x";
                last = "x";
            }
            else
            {
                _config._showLegendsAbbr = false;
                first = _options.First ().Legend;
                last = _options.Last ().Legend;
            }

            // --o--
            // Hello
            // Left = He
            // Right = lo
            int firstLeft = (first.Length - 1) / 2; // Chars count of the first option to the left.
            int lastRight = last.Length / 2; // Chars count of the last option to the right.

            if (_config._sliderOrientation != _config._legendsOrientation)
            {
                firstLeft = 0;
                lastRight = 0;
            }

            // -1 because it's better to have an extra space at right than to clip
            int width = size - firstLeft - lastRight - 1;

            _config._startSpacing = firstLeft;

            if (_options.Count == 1)
            {
                _config._cachedInnerSpacing = maxLegend;
            }
            else
            {
                _config._cachedInnerSpacing = Math.Max (0, (int)Math.Floor ((double)width / (_options.Count - 1)) - 1);
            }

            _config._cachedInnerSpacing = Math.Max (_config._minInnerSpacing, _config._cachedInnerSpacing);

            _config._endSpacing = lastRight;
        }
    }

    /// <summary>Calculates the min dimension required for all options and inner spacing with abbreviated legends</summary>
    /// <returns></returns>
    private int CalcMinLength ()
    {
        if (_options.Count == 0)
        {
            return 0;
        }

        var length = 0;
        length += _config._startSpacing + _config._endSpacing;
        length += _options.Count;
        length += (_options.Count - 1) * _config._minInnerSpacing;

        return length;
    }

    /// <summary>
    ///     Gets the ideal width of the slider. The ideal width is the minimum width required to display all options and inner
    ///     spacing.
    /// </summary>
    /// <returns></returns>
    public int GetIdealWidth ()
    {
        if (UseMinimumSize)
        {
            return Orientation == Orientation.Horizontal ? CalcMinLength () : CalcIdealThickness ();
        }

        return Orientation == Orientation.Horizontal ? CalcIdealLength () : CalcIdealThickness ();
    }

    /// <summary>
    ///     Gets the ideal height of the slider. The ideal height is the minimum height required to display all options and
    ///     inner spacing.
    /// </summary>
    /// <returns></returns>
    public int GetIdealHeight ()
    {
        if (UseMinimumSize)
        {
            return Orientation == Orientation.Horizontal ? CalcIdealThickness () : CalcMinLength ();
        }

        return Orientation == Orientation.Horizontal ? CalcIdealThickness () : CalcIdealLength ();
    }

    /// <summary>
    ///     Calculates the ideal dimension required for all options, inner spacing, and legends (non-abbreviated, with one
    ///     space between).
    /// </summary>
    /// <returns></returns>
    private int CalcIdealLength ()
    {
        if (_options.Count == 0)
        {
            return 0;
        }

        bool isVertical = Orientation == Orientation.Vertical;
        var length = 0;

        if (_config._showLegends)
        {
            if (_config._legendsOrientation == _config._sliderOrientation && _options.Count > 0)
            {
                // Each legend should be centered in a space the width of the longest legend, with one space between.
                // Calculate the total length required for all legends.
                //if (!isVertical)
                {
                    int maxLegend = int.Max (_options.Max (s => s.Legend?.GetColumns () ?? 1), 1);
                    length = maxLegend * _options.Count + (_options.Count - 1);
                }

                //
                //{
                //    length = CalcMinLength ();
                //}
            }
            else
            {
                length = CalcMinLength ();
            }
        }

        return Math.Max (length, CalcMinLength ());
    }

    /// <summary>
    ///     Calculates the minimum dimension required for the slider and legends.
    /// </summary>
    /// <returns></returns>
    private int CalcIdealThickness ()
    {
        var thickness = 1; // Always show the slider.

        if (_config._showLegends)
        {
            if (_config._legendsOrientation != _config._sliderOrientation && _options.Count > 0)
            {
                thickness += _options.Max (s => s.Legend?.GetColumns () ?? 0);
            }
            else
            {
                thickness += 1;
            }
        }

        return thickness;
    }

    #endregion Helpers

    #region Cursor and Position

    internal bool TryGetPositionByOption (int option, out (int x, int y) position)
    {
        position = (-1, -1);

        if (option < 0 || option >= _options.Count ())
        {
            return false;
        }

        var offset = 0;
        offset += _config._startSpacing;
        offset += option * (_config._cachedInnerSpacing + 1);

        if (_config._sliderOrientation == Orientation.Vertical)
        {
            position = (0, offset);
        }
        else
        {
            position = (offset, 0);
        }

        return true;
    }

    /// <summary>Tries to get the option index by the position.</summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="threshold"></param>
    /// <param name="optionIdx"></param>
    /// <returns></returns>
    internal bool TryGetOptionByPosition (int x, int y, int threshold, out int optionIdx)
    {
        // Fix(jmperricone): Not working.
        optionIdx = -1;

        if (Orientation == Orientation.Horizontal)
        {
            if (y != 0)
            {
                return false;
            }

            for (int xx = x - threshold; xx < x + threshold + 1; xx++)
            {
                int cx = xx;
                cx -= _config._startSpacing;

                int option = cx / (_config._cachedInnerSpacing + 1);
                bool valid = cx % (_config._cachedInnerSpacing + 1) == 0;

                if (!valid || option < 0 || option > _options.Count - 1)
                {
                    continue;
                }

                optionIdx = option;

                return true;
            }
        }
        else
        {
            if (x != 0)
            {
                return false;
            }

            for (int yy = y - threshold; yy < y + threshold + 1; yy++)
            {
                int cy = yy;
                cy -= _config._startSpacing;

                int option = cy / (_config._cachedInnerSpacing + 1);
                bool valid = cy % (_config._cachedInnerSpacing + 1) == 0;

                if (!valid || option < 0 || option > _options.Count - 1)
                {
                    continue;
                }

                optionIdx = option;

                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        if (TryGetPositionByOption (FocusedOption, out (int x, int y) position))
        {
            if (IsInitialized && Viewport.Contains (position.x, position.y))
            {
                Move (position.x, position.y);

                return new (position.x, position.y);
            }
        }

        return base.PositionCursor ();
    }

    #endregion Cursor and Position

    #region Drawing

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        // TODO: make this more surgical to reduce repaint

        if (_options is null || _options.Count == 0)
        {
            return;
        }

        // Draw Slider
        DrawSlider ();

        // Draw Legends.
        if (_config._showLegends)
        {
            DrawLegends ();
        }

        if (_dragPosition.HasValue && _moveRenderPosition.HasValue)
        {
            AddRune (_moveRenderPosition.Value.X, _moveRenderPosition.Value.Y, Style.DragChar.Rune);
        }
    }

    private string AlignText (string text, int width, Alignment alignment)
    {
        if (text is null)
        {
            return "";
        }

        if (text.Length > width)
        {
            text = text [..width];
        }

        int w = width - text.Length;
        string s1 = new (' ', w / 2);
        string s2 = new (' ', w % 2);

        // Note: The formatter doesn't handle all of this ???
        switch (alignment)
        {
            case Alignment.Fill:
                return TextFormatter.Justify (text, width);
            case Alignment.Start:
                return text + s1 + s1 + s2;
            case Alignment.Center:
                if (text.Length % 2 != 0)
                {
                    return s1 + text + s1 + s2;
                }

                return s1 + s2 + text + s1;
            case Alignment.End:
                return s1 + s1 + s2 + text;
            default:
                return text;
        }
    }

    private void DrawSlider ()
    {
        // TODO: be more surgical on clear
        Clear ();

        // Attributes

        var normalAttr = new Attribute (Color.White, Color.Black);
        var setAttr = new Attribute (Color.Black, Color.White);

        if (IsInitialized)
        {
            normalAttr = ColorScheme?.Normal ?? Application.Top.ColorScheme.Normal;
            setAttr = Style.SetChar.Attribute ?? ColorScheme!.HotNormal;
        }

        bool isVertical = _config._sliderOrientation == Orientation.Vertical;
        bool isLegendsVertical = _config._legendsOrientation == Orientation.Vertical;
        bool isReverse = _config._sliderOrientation != _config._legendsOrientation;

        var x = 0;
        var y = 0;

        bool isSet = _setOptions.Count > 0;

        // Left Spacing
        if (_config._showEndSpacing && _config._startSpacing > 0)
        {
            Driver?.SetAttribute (
                                  isSet && _config._type == SliderType.LeftRange
                                      ? Style.RangeChar.Attribute ?? normalAttr
                                      : Style.SpaceChar.Attribute ?? normalAttr
                                 );
            Rune rune = isSet && _config._type == SliderType.LeftRange ? Style.RangeChar.Rune : Style.SpaceChar.Rune;

            for (var i = 0; i < _config._startSpacing; i++)
            {
                MoveAndAdd (x, y, rune);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }
            }
        }
        else
        {
            Driver?.SetAttribute (Style.EmptyChar.Attribute ?? normalAttr);

            for (var i = 0; i < _config._startSpacing; i++)
            {
                MoveAndAdd (x, y, Style.EmptyChar.Rune);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }
            }
        }

        // Slider
        if (_options.Count > 0)
        {
            for (var i = 0; i < _options.Count; i++)
            {
                var drawRange = false;

                if (isSet)
                {
                    switch (_config._type)
                    {
                        case SliderType.LeftRange when i <= _setOptions [0]:
                            drawRange = i < _setOptions [0];

                            break;
                        case SliderType.RightRange when i >= _setOptions [0]:
                            drawRange = i >= _setOptions [0];

                            break;
                        case SliderType.Range when _setOptions.Count == 1:
                            drawRange = false;

                            break;
                        case SliderType.Range when _setOptions.Count == 2:
                            if ((i >= _setOptions [0] && i <= _setOptions [1])
                                || (i >= _setOptions [1] && i <= _setOptions [0]))
                            {
                                drawRange = (i >= _setOptions [0] && i < _setOptions [1])
                                            || (i >= _setOptions [1] && i < _setOptions [0]);
                            }

                            break;
                    }
                }

                // Draw Option
                Driver?.SetAttribute (
                                      isSet && _setOptions.Contains (i) ? Style.SetChar.Attribute ?? setAttr :
                                      drawRange ? Style.RangeChar.Attribute ?? setAttr : Style.OptionChar.Attribute ?? normalAttr
                                     );

                Rune rune = drawRange ? Style.RangeChar.Rune : Style.OptionChar.Rune;

                if (isSet)
                {
                    if (_setOptions [0] == i)
                    {
                        rune = Style.StartRangeChar.Rune;
                    }
                    else if (_setOptions.Count > 1 && _setOptions [1] == i)
                    {
                        rune = Style.EndRangeChar.Rune;
                    }
                    else if (_setOptions.Contains (i))
                    {
                        rune = Style.SetChar.Rune;
                    }
                }

                MoveAndAdd (x, y, rune);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }

                // Draw Spacing
                if (_config._showEndSpacing || i < _options.Count - 1)
                {
                    // Skip if is the Last Spacing.
                    Driver?.SetAttribute (
                                          drawRange && isSet
                                              ? Style.RangeChar.Attribute ?? setAttr
                                              : Style.SpaceChar.Attribute ?? normalAttr
                                         );

                    for (var s = 0; s < _config._cachedInnerSpacing; s++)
                    {
                        MoveAndAdd (x, y, drawRange && isSet ? Style.RangeChar.Rune : Style.SpaceChar.Rune);

                        if (isVertical)
                        {
                            y++;
                        }
                        else
                        {
                            x++;
                        }
                    }
                }
            }
        }

        int remaining = isVertical ? Viewport.Height - y : Viewport.Width - x;

        // Right Spacing
        if (_config._showEndSpacing)
        {
            Driver?.SetAttribute (
                                  isSet && _config._type == SliderType.RightRange
                                      ? Style.RangeChar.Attribute ?? normalAttr
                                      : Style.SpaceChar.Attribute ?? normalAttr
                                 );
            Rune rune = isSet && _config._type == SliderType.RightRange ? Style.RangeChar.Rune : Style.SpaceChar.Rune;

            for (var i = 0; i < remaining; i++)
            {
                MoveAndAdd (x, y, rune);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }
            }
        }
        else
        {
            Driver?.SetAttribute (Style.EmptyChar.Attribute ?? normalAttr);

            for (var i = 0; i < remaining; i++)
            {
                MoveAndAdd (x, y, Style.EmptyChar.Rune);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }
            }
        }
    }

    private void DrawLegends ()
    {
        // Attributes
        var normalAttr = new Attribute (Color.White, Color.Black);
        var setAttr = new Attribute (Color.Black, Color.White);
        Attribute spaceAttr = normalAttr;

        if (IsInitialized)
        {
            normalAttr = Style.LegendAttributes.NormalAttribute ?? ColorScheme?.Normal ?? ColorScheme.Disabled;
            setAttr = Style.LegendAttributes.SetAttribute ?? ColorScheme?.HotNormal ?? ColorScheme.Normal;
            spaceAttr = Style.LegendAttributes.EmptyAttribute ?? normalAttr;
        }

        bool isTextVertical = _config._legendsOrientation == Orientation.Vertical;
        bool isSet = _setOptions.Count > 0;

        var x = 0;
        var y = 0;

        Move (x, y);

        if (_config._sliderOrientation == Orientation.Horizontal
            && _config._legendsOrientation == Orientation.Vertical)
        {
            x += _config._startSpacing;
        }

        if (_config._sliderOrientation == Orientation.Vertical
            && _config._legendsOrientation == Orientation.Horizontal)
        {
            y += _config._startSpacing;
        }

        if (_config._sliderOrientation == Orientation.Horizontal)
        {
            y += 1;
        }
        else
        {
            // Vertical
            x += 1;
        }

        for (var i = 0; i < _options.Count; i++)
        {
            var isOptionSet = false;

            // Check if the Option is Set.
            switch (_config._type)
            {
                case SliderType.Single:
                case SliderType.Multiple:
                    if (isSet && _setOptions.Contains (i))
                    {
                        isOptionSet = true;
                    }

                    break;
                case SliderType.LeftRange:
                    if (isSet && i <= _setOptions [0])
                    {
                        isOptionSet = true;
                    }

                    break;
                case SliderType.RightRange:
                    if (isSet && i >= _setOptions [0])
                    {
                        isOptionSet = true;
                    }

                    break;
                case SliderType.Range when _setOptions.Count == 1:
                    if (isSet && i == _setOptions [0])
                    {
                        isOptionSet = true;
                    }

                    break;
                case SliderType.Range:
                    if (isSet
                        && ((i >= _setOptions [0] && i <= _setOptions [1])
                            || (i >= _setOptions [1] && i <= _setOptions [0])))
                    {
                        isOptionSet = true;
                    }

                    break;
            }

            // Text || Abbreviation
            var text = string.Empty;

            if (_config._showLegendsAbbr)
            {
                text = _options [i].LegendAbbr.ToString () ?? new Rune (_options [i].Legend.First ()).ToString ();
            }
            else
            {
                text = _options [i].Legend;
            }

            switch (_config._sliderOrientation)
            {
                case Orientation.Horizontal:
                    switch (_config._legendsOrientation)
                    {
                        case Orientation.Horizontal:
                            text = AlignText (text, _config._cachedInnerSpacing + 1, Alignment.Center);

                            break;
                        case Orientation.Vertical:
                            y = 1;

                            break;
                    }

                    break;
                case Orientation.Vertical:
                    switch (_config._legendsOrientation)
                    {
                        case Orientation.Horizontal:
                            x = 1;

                            break;
                        case Orientation.Vertical:
                            text = AlignText (text, _config._cachedInnerSpacing + 1, Alignment.Center);

                            break;
                    }

                    break;
            }

            // Text
            int legendLeftSpacesCount = text.TakeWhile (e => e == ' ').Count ();
            int legendRightSpacesCount = text.Reverse ().TakeWhile (e => e == ' ').Count ();
            text = text.Trim ();

            // TODO(jmperricone): Improve the Orientation check.

            // Calculate Start Spacing
            if (_config._sliderOrientation == _config._legendsOrientation)
            {
                if (i == 0)
                {
                    // The spacing for the slider use the StartSpacing but...
                    // The spacing for the legends is the StartSpacing MINUS the total chars to the left of the first options.
                    //    ●────●────●
                    //  Hello Bye World
                    //
                    // chars_left is 2 for Hello => (5 - 1) / 2
                    //
                    // then the spacing is 2 for the slider but 0 for the legends.

                    int charsLeft = (text.Length - 1) / 2;
                    legendLeftSpacesCount = _config._startSpacing - charsLeft;
                }

                // Option Left Spacing
                if (isTextVertical)
                {
                    y += legendLeftSpacesCount;
                }
                else
                {
                    x += legendLeftSpacesCount;
                }

                //Move (x, y);
            }

            // Legend
            Driver?.SetAttribute (isOptionSet ? setAttr : normalAttr);

            foreach (Rune c in text.EnumerateRunes ())
            {
                MoveAndAdd (x, y, c);

                //Driver.AddRune (c);
                if (isTextVertical)
                {
                    y += 1;
                }
                else
                {
                    x += 1;
                }
            }

            // Calculate End Spacing
            if (i == _options.Count () - 1)
            {
                // See Start Spacing explanation.
                int charsRight = text.Length / 2;
                legendRightSpacesCount = _config._endSpacing - charsRight;
            }

            // Option Right Spacing of Option
            Driver?.SetAttribute (spaceAttr);

            if (isTextVertical)
            {
                y += legendRightSpacesCount;
            }
            else
            {
                x += legendRightSpacesCount;
            }

            if (_config._sliderOrientation == Orientation.Horizontal
                && _config._legendsOrientation == Orientation.Vertical)
            {
                x += _config._cachedInnerSpacing + 1;
            }
            else if (_config._sliderOrientation == Orientation.Vertical
                     && _config._legendsOrientation == Orientation.Horizontal)
            {
                y += _config._cachedInnerSpacing + 1;
            }
        }
    }

    #endregion Drawing

    #region Keys and Mouse

    // Mouse coordinates of current drag
    private Point? _dragPosition;

    // Coordinates of where the "move cursor" is drawn (in OnDrawContent)
    private Point? _moveRenderPosition;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        // Note(jmperricone): Maybe we click to focus the cursor, and on next click we set the option.
        //                    That will make OptionFocused Event more relevant.
        // (tig: I don't think so. Maybe an option if someone really wants it, but for now that
        //       adds too much friction to UI.
        // TODO(jmperricone): Make Range Type work with mouse.

        if (!(mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
              || mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)
              || mouseEvent.Flags.HasFlag (MouseFlags.ReportMousePosition)
              || mouseEvent.Flags.HasFlag (MouseFlags.Button1Released)))
        {
            return false;
        }

        SetFocus ();

        if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.ReportMousePosition))
            {
                _dragPosition = mouseEvent.Position;
                _moveRenderPosition = ClampMovePosition ((Point)_dragPosition);
                Application.GrabMouse (this);
            }

            SetNeedsDisplay ();

            return true;
        }

        if (_dragPosition.HasValue
            && mouseEvent.Flags.HasFlag (MouseFlags.ReportMousePosition)
            && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            // Continue Drag
            _dragPosition = mouseEvent.Position;
            _moveRenderPosition = ClampMovePosition ((Point)_dragPosition);

            var success = false;
            var option = 0;

            // how far has user dragged from original location?
            if (Orientation == Orientation.Horizontal)
            {
                success = TryGetOptionByPosition (mouseEvent.Position.X, 0, Math.Max (0, _config._cachedInnerSpacing / 2), out option);
            }
            else
            {
                success = TryGetOptionByPosition (0, mouseEvent.Position.Y, Math.Max (0, _config._cachedInnerSpacing / 2), out option);
            }

            if (!_config._allowEmpty && success)
            {
                if (!OnOptionFocused (option, new (GetSetOptionDictionary (), FocusedOption)))
                {
                    SetFocusedOption ();
                }
            }

            SetNeedsDisplay ();

            return true;
        }

        if ((_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Released))
            || mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            // End Drag
            Application.UngrabMouse ();
            _dragPosition = null;
            _moveRenderPosition = null;

            // TODO: Add func to calc distance between options to use as the MouseClickXOptionThreshold
            var success = false;
            var option = 0;

            if (Orientation == Orientation.Horizontal)
            {
                success = TryGetOptionByPosition (mouseEvent.Position.X, 0, Math.Max (0, _config._cachedInnerSpacing / 2), out option);
            }
            else
            {
                success = TryGetOptionByPosition (0, mouseEvent.Position.Y, Math.Max (0, _config._cachedInnerSpacing / 2), out option);
            }

            if (success)
            {
                if (!OnOptionFocused (option, new (GetSetOptionDictionary (), FocusedOption)))
                {
                    SetFocusedOption ();
                }
            }

            SetNeedsDisplay ();

            mouseEvent.Handled = true;

        }

        return mouseEvent.Handled;

        Point ClampMovePosition (Point position)
        {
            int Clamp (int value, int min, int max) { return Math.Max (min, Math.Min (max, value)); }

            if (Orientation == Orientation.Horizontal)
            {
                int left = _config._startSpacing;
                int width = _options.Count + (_options.Count - 1) * _config._cachedInnerSpacing;
                int right = left + width - 1;
                int clampedX = Clamp (position.X, left, right);
                position = new (clampedX, 0);
            }
            else
            {
                int top = _config._startSpacing;
                int height = _options.Count + (_options.Count - 1) * _config._cachedInnerSpacing;
                int bottom = top + height - 1;
                int clampedY = Clamp (position.Y, top, bottom);
                position = new (0, clampedY);
            }

            return position;
        }
    }

    private void SetCommands ()
    {
        AddCommand (Command.Right, () => MovePlus ());
        AddCommand (Command.Down, () => MovePlus ());
        AddCommand (Command.Left, () => MoveMinus ());
        AddCommand (Command.Up, () => MoveMinus ());
        AddCommand (Command.LeftStart, () => MoveStart ());
        AddCommand (Command.RightEnd, () => MoveEnd ());
        AddCommand (Command.RightExtend, () => ExtendPlus ());
        AddCommand (Command.LeftExtend, () => ExtendMinus ());
        AddCommand (Command.Select, () => Select ());
        AddCommand (Command.Accept, (ctx) => Accept (ctx));

        SetKeyBindings ();
    }

    // This is called during initialization and anytime orientation changes
    private void SetKeyBindings ()
    {
        if (_config._sliderOrientation == Orientation.Horizontal)
        {
            KeyBindings.Add (Key.CursorRight, Command.Right);
            KeyBindings.Remove (Key.CursorDown);
            KeyBindings.Add (Key.CursorLeft, Command.Left);
            KeyBindings.Remove (Key.CursorUp);

            KeyBindings.Add (Key.CursorRight.WithCtrl, Command.RightExtend);
            KeyBindings.Remove (Key.CursorDown.WithCtrl);
            KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.LeftExtend);
            KeyBindings.Remove (Key.CursorUp.WithCtrl);
        }
        else
        {
            KeyBindings.Remove (Key.CursorRight);
            KeyBindings.Add (Key.CursorDown, Command.Down);
            KeyBindings.Remove (Key.CursorLeft);
            KeyBindings.Add (Key.CursorUp, Command.Up);

            KeyBindings.Remove (Key.CursorRight.WithCtrl);
            KeyBindings.Add (Key.CursorDown.WithCtrl, Command.RightExtend);
            KeyBindings.Remove (Key.CursorLeft.WithCtrl);
            KeyBindings.Add (Key.CursorUp.WithCtrl, Command.LeftExtend);
        }

        KeyBindings.Remove (Key.Home);
        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Remove (Key.End);
        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Remove (Key.Enter);
        KeyBindings.Add (Key.Enter, Command.Accept);
        KeyBindings.Remove (Key.Space);
        KeyBindings.Add (Key.Space, Command.Select);
    }

    private Dictionary<int, SliderOption<T>> GetSetOptionDictionary () { return _setOptions.ToDictionary (e => e, e => _options [e]); }

    /// <summary>
    /// Sets or unsets <paramref name="optionIndex"/> based on <paramref name="set"/>.
    /// </summary>
    /// <param name="optionIndex">The option to change.</param>
    /// <param name="set">If <see langword="true"/>, sets the option. Unsets it otherwise.</param>
    public void ChangeOption (int optionIndex, bool set)
    {
        if (set)
        {
            if (!_setOptions.Contains (optionIndex))
            {
                _setOptions.Add (optionIndex);
                _options [optionIndex].OnSet ();
            }
        }
        else
        {
            if (_setOptions.Contains (optionIndex))
            {
                _setOptions.Remove (optionIndex);
                _options [optionIndex].OnUnSet ();
            }
        }

        // Raise slider changed event.
        OnOptionsChanged ();
    }

    private bool SetFocusedOption ()
    {
        if (_options.Count == 0)
        {
            return false;
        }
        bool changed = false;
        switch (_config._type)
        {
            case SliderType.Single:
            case SliderType.LeftRange:
            case SliderType.RightRange:

                if (_setOptions.Count == 1)
                {
                    int prev = _setOptions [0];

                    if (!_config._allowEmpty && prev == FocusedOption)
                    {
                        break;
                    }

                    _setOptions.Clear ();
                    _options [FocusedOption].OnUnSet ();

                    if (FocusedOption != prev)
                    {
                        _setOptions.Add (FocusedOption);
                        _options [FocusedOption].OnSet ();
                    }
                }
                else
                {
                    _setOptions.Add (FocusedOption);
                    _options [FocusedOption].OnSet ();
                }

                // Raise slider changed event.
                OnOptionsChanged ();
                changed = true;

                break;
            case SliderType.Multiple:
                if (_setOptions.Contains (FocusedOption))
                {
                    if (!_config._allowEmpty && _setOptions.Count () == 1)
                    {
                        break;
                    }

                    _setOptions.Remove (FocusedOption);
                    _options [FocusedOption].OnUnSet ();
                }
                else
                {
                    _setOptions.Add (FocusedOption);
                    _options [FocusedOption].OnSet ();
                }

                OnOptionsChanged ();
                changed = true;

                break;

            case SliderType.Range:
                if (_config._rangeAllowSingle)
                {
                    if (_setOptions.Count == 1)
                    {
                        int prev = _setOptions [0];

                        if (!_config._allowEmpty && prev == FocusedOption)
                        {
                            break;
                        }

                        if (FocusedOption == prev)
                        {
                            // un-set
                            _setOptions.Clear ();
                            _options [FocusedOption].OnUnSet ();
                        }
                        else
                        {
                            _setOptions [0] = FocusedOption;
                            _setOptions.Add (prev);
                            _setOptions.Sort ();
                            _options [FocusedOption].OnSet ();
                        }
                    }
                    else if (_setOptions.Count == 0)
                    {
                        _setOptions.Add (FocusedOption);
                        _options [FocusedOption].OnSet ();
                    }
                    else
                    {
                        // Extend/Shrink
                        if (FocusedOption < _setOptions [0])
                        {
                            // extend left
                            _options [_setOptions [0]].OnUnSet ();
                            _setOptions [0] = FocusedOption;
                        }
                        else if (FocusedOption > _setOptions [1])
                        {
                            // extend right
                            _options [_setOptions [1]].OnUnSet ();
                            _setOptions [1] = FocusedOption;
                        }
                        else if (FocusedOption >= _setOptions [0] && FocusedOption <= _setOptions [1])
                        {
                            if (FocusedOption < _lastFocusedOption)
                            {
                                // shrink to the left
                                _options [_setOptions [1]].OnUnSet ();
                                _setOptions [1] = FocusedOption;
                            }
                            else if (FocusedOption > _lastFocusedOption)
                            {
                                // shrink to the right
                                _options [_setOptions [0]].OnUnSet ();
                                _setOptions [0] = FocusedOption;
                            }

                            if (_setOptions.Count > 1 && _setOptions [0] == _setOptions [1])
                            {
                                _setOptions.Clear ();
                                _setOptions.Add (FocusedOption);
                            }
                        }
                    }
                }
                else
                {
                    if (_setOptions.Count == 1)
                    {
                        int prev = _setOptions [0];

                        if (!_config._allowEmpty && prev == FocusedOption)
                        {
                            break;
                        }

                        _setOptions [0] = FocusedOption;
                        _setOptions.Add (prev);
                        _setOptions.Sort ();
                        _options [FocusedOption].OnSet ();
                    }
                    else if (_setOptions.Count == 0)
                    {
                        _setOptions.Add (FocusedOption);
                        _options [FocusedOption].OnSet ();
                        int next = FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption - 1;
                        _setOptions.Add (next);
                        _options [next].OnSet ();
                    }
                    else
                    {
                        // Extend/Shrink
                        if (FocusedOption < _setOptions [0])
                        {
                            // extend left
                            _options [_setOptions [0]].OnUnSet ();
                            _setOptions [0] = FocusedOption;
                        }
                        else if (FocusedOption > _setOptions [1])
                        {
                            // extend right
                            _options [_setOptions [1]].OnUnSet ();
                            _setOptions [1] = FocusedOption;
                        }
                        else if (FocusedOption >= _setOptions [0]
                                 && FocusedOption <= _setOptions [1]
                                 && _setOptions [1] - _setOptions [0] > 1)
                        {
                            if (FocusedOption < _lastFocusedOption)
                            {
                                // shrink to the left
                                _options [_setOptions [1]].OnUnSet ();
                                _setOptions [1] = FocusedOption;
                            }
                            else if (FocusedOption > _lastFocusedOption)
                            {
                                // shrink to the right
                                _options [_setOptions [0]].OnUnSet ();
                                _setOptions [0] = FocusedOption;
                            }
                        }
                    }
                }

                // Raise Slider Option Changed Event.
                OnOptionsChanged ();
                changed = true;

                break;
            default:
                throw new ArgumentOutOfRangeException (_config._type.ToString ());
        }

        return changed;
    }

    internal bool ExtendPlus ()
    {
        int next = FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption;

        if (next != FocusedOption
            && !OnOptionFocused (
                                 next,
                                 new (
                                      GetSetOptionDictionary (),
                                      FocusedOption
                                     )
                                ))
        {
            SetFocusedOption ();
        }

        return true;

        //// TODO: Support RangeMultiple
        //if (_setOptions.Contains (FocusedOption)) {
        //	var next = FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption;
        //	if (!_setOptions.Contains (next)) {
        //		if (_config._type == SliderType.Range) {
        //			if (_setOptions.Count == 1) {
        //				if (!OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
        //					_setOptions.Add (FocusedOption);
        //					_setOptions.Sort (); // Range Type
        //					OnOptionsChanged ();
        //				}
        //			} else if (_setOptions.Count == 2) {
        //				if (!OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
        //					_setOptions [1] = FocusedOption;
        //					_setOptions.Sort (); // Range Type
        //					OnOptionsChanged ();
        //				}
        //			}
        //		} else {
        //			_setOptions.Remove (FocusedOption);
        //			// Note(jmperricone): We are setting the option here, do we send the OptionFocused Event too ?

        //			if (!OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
        //				_setOptions.Add (FocusedOption);
        //				_setOptions.Sort (); // Range Type
        //				OnOptionsChanged ();
        //			}
        //		}
        //	} else {
        //		if (_config._type == SliderType.Range) {
        //			if (!OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
        //				_setOptions.Clear();
        //				_setOptions.Add (FocusedOption);
        //				OnOptionsChanged ();
        //			}
        //		} else if (/*_settingRange == true ||*/ !AllowEmpty) {
        //			SetFocusedOption ();
        //		}
        //	}
        //}
        //return true;
    }

    internal bool ExtendMinus ()
    {
        int prev = FocusedOption > 0 ? FocusedOption - 1 : FocusedOption;

        if (prev != FocusedOption
            && !OnOptionFocused (
                                 prev,
                                 new (
                                      GetSetOptionDictionary (),
                                      FocusedOption
                                     )
                                ))
        {
            SetFocusedOption ();
        }

        return true;
    }

    internal bool Select ()
    {
        return SetFocusedOption ();
    }

    internal bool Accept (CommandContext ctx)
    {
        SetFocusedOption ();

        return RaiseAccepting (ctx) == true;
    }

    internal bool MovePlus ()
    {
        bool cancelled = OnOptionFocused (
                                          FocusedOption + 1,
                                          new (GetSetOptionDictionary (), FocusedOption)
                                         );

        if (cancelled)
        {
            return false;
        }

        if (!AllowEmpty)
        {
            SetFocusedOption ();
        }

        return true;
    }

    internal bool MoveMinus ()
    {
        bool cancelled = OnOptionFocused (
                                          FocusedOption - 1,
                                          new (GetSetOptionDictionary (), FocusedOption)
                                         );

        if (cancelled)
        {
            return false;
        }

        if (!AllowEmpty)
        {
            SetFocusedOption ();
        }

        return true;
    }

    internal bool MoveStart ()
    {
        if (OnOptionFocused (0, new (GetSetOptionDictionary (), FocusedOption)))
        {
            return false;
        }

        if (!AllowEmpty)
        {
            SetFocusedOption ();
        }

        return true;
    }

    internal bool MoveEnd ()
    {
        if (OnOptionFocused (_options.Count - 1, new (GetSetOptionDictionary (), FocusedOption)))
        {
            return false;
        }

        if (!AllowEmpty)
        {
            SetFocusedOption ();
        }

        return true;
    }

    #endregion
}
