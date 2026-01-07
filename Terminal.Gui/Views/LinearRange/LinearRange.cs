namespace Terminal.Gui.Views;

/// <summary>
///     Provides a linear range control letting the user navigate from a set of typed options in a linear manner using the
///     keyboard or mouse.
/// </summary>
public class LinearRange : LinearRange<object>
{
    /// <summary>Initializes a new instance of the <see cref="LinearRange"/> class.</summary>
    public LinearRange () { }

    /// <summary>Initializes a new instance of the <see cref="LinearRange"/> class.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearRange (List<object> options, Orientation orientation = Orientation.Horizontal) :
        base (options, orientation)
    { }
}

/// <summary>
///     Provides a type-safe linear range control letting the user navigate from a set of typed options in a linear manner
///     using the
///     keyboard or mouse.
/// </summary>
/// <typeparam name="T"></typeparam>
public class LinearRange<T> : View, IOrientation
{
    private readonly LinearRangeConfiguration _config = new ();

    // List of the current set options.
    private readonly List<int> _setOptions = [];

    // Options
    private List<LinearRangeOption<T>>? _options;

    private OrientationHelper? _orientationHelper;

    #region Initialize

    private void SetInitialProperties (
        List<LinearRangeOption<T>> options,
        Orientation orientation = Orientation.Horizontal
    )
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        CanFocus = true;

        _options = options;

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new (this); // Do not use object initializer!
        _orientationHelper.Orientation = _config._linearRangeOrientation = orientation;
        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);

        SetDefaultStyle ();
        SetCommands ();
        SetContentSize ();

        SubViewLayout += (s, e) => { SetContentSize (); };
    }

    // TODO: Make configurable via ConfigurationManager
    private void SetDefaultStyle ()
    {
        _config._showLegends = true;

        switch (_config._linearRangeOrientation)
        {
            case Orientation.Horizontal:
                Style.SpaceChar = new () { Grapheme = Glyphs.HLine.ToString () }; // '─'
                Style.OptionChar = new () { Grapheme = Glyphs.BlackCircle.ToString () }; // '┼●🗹□⏹'

                break;
            case Orientation.Vertical:
                Style.SpaceChar = new () { Grapheme = Glyphs.VLine.ToString () };
                Style.OptionChar = new () { Grapheme = Glyphs.BlackCircle.ToString () };

                break;
        }

        _config._legendsOrientation = _config._linearRangeOrientation;
        Style.EmptyChar = new () { Grapheme = " " };
        Style.SetChar = new () { Grapheme = Glyphs.ContinuousMeterSegment.ToString () }; // ■
        Style.RangeChar = new () { Grapheme = Glyphs.Stipple.ToString () }; // ░ ▒ ▓   // Medium shade not blinking on curses.
        Style.StartRangeChar = new () { Grapheme = Glyphs.ContinuousMeterSegment.ToString () };
        Style.EndRangeChar = new () { Grapheme = Glyphs.ContinuousMeterSegment.ToString () };
        Style.DragChar = new () { Grapheme = Glyphs.Diamond.ToString () };
    }

    #endregion

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="LinearRange{T}"/> class.</summary>
    public LinearRange () : this (new ()) { }

    /// <summary>Initializes a new instance of the <see cref="LinearRange{T}"/> class.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearRange (List<T>? options, Orientation orientation = Orientation.Horizontal)
    {
        if (options is null)
        {
            return;
        }

        if (options is { Count: 0 })
        {
            SetInitialProperties ([], orientation);
        }
        else
        {
            SetInitialProperties (
                                  options.Select (e =>
                                                  {
                                                      var legend = e?.ToString ();

                                                      return new LinearRangeOption<T>
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
    ///     Setting the Text of a linear range is a shortcut to setting options. The text is a CSV string of the options.
    /// </summary>
    public override string Text
    {
        // Return labels as a CSV string
        get => _options is null or { Count: 0 } ? string.Empty : string.Join (",", _options);
        set
        {
            if (string.IsNullOrEmpty (value))
            {
                Options = [];
            }
            else
            {
                IEnumerable<string> list = value.Split (',').Select (x => x.Trim ());
                Options = list.Select (x => new LinearRangeOption<T> { Legend = x }).ToList ();
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

            if (!value && _options!.Count > 0 && _setOptions.Count == 0)
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
            int current = _config._minInnerSpacing;

            CWPPropertyHelper.ChangeProperty (
                                              this,
                                              ref current,
                                              value,
                                              OnMinimumInnerSpacingChanging,
                                              MinimumInnerSpacingChanging,
                                              newValue =>
                                              {
                                                  _config._minInnerSpacing = newValue;
                                                  SetContentSize ();
                                              },
                                              OnMinimumInnerSpacingChanged,
                                              MinimumInnerSpacingChanged,
                                              out int _
                                             );
        }
    }

    /// <summary>Event raised before the <see cref="MinimumInnerSpacing"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<int>>? MinimumInnerSpacingChanging;

    /// <summary>Event raised after the <see cref="MinimumInnerSpacing"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<int>>? MinimumInnerSpacingChanged;

    /// <summary>Called before <see cref="MinimumInnerSpacing"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnMinimumInnerSpacingChanging (ValueChangingEventArgs<int> args) => false;

    /// <summary>Called after <see cref="MinimumInnerSpacing"/> has changed.</summary>
    protected virtual void OnMinimumInnerSpacingChanged (ValueChangedEventArgs<int> args) { }

    /// <summary>LinearRange Type. <see cref="LinearRangeType"></see></summary>
    public LinearRangeType Type
    {
        get => _config._type;
        set
        {
            LinearRangeType current = _config._type;

            CWPPropertyHelper.ChangeProperty (
                                              this,
                                              ref current,
                                              value,
                                              OnTypeChanging,
                                              TypeChanging,
                                              newValue =>
                                              {
                                                  _config._type = newValue;

                                                  // Todo: Custom logic to preserve options.
                                                  _setOptions.Clear ();
                                                  SetNeedsDraw ();
                                              },
                                              OnTypeChanged,
                                              TypeChanged,
                                              out LinearRangeType _
                                             );
        }
    }

    /// <summary>Event raised before the <see cref="Type"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<LinearRangeType>>? TypeChanging;

    /// <summary>Event raised after the <see cref="Type"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<LinearRangeType>>? TypeChanged;

    /// <summary>Called before <see cref="Type"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnTypeChanging (ValueChangingEventArgs<LinearRangeType> args) => false;

    /// <summary>Called after <see cref="Type"/> has changed.</summary>
    protected virtual void OnTypeChanged (ValueChangedEventArgs<LinearRangeType> args) { }

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/>. The default is <see cref="Orientation.Horizontal"/>.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientationHelper!.Orientation;
        set => _orientationHelper!.Orientation = value;
    }

    #region IOrientation members

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        _config._linearRangeOrientation = newOrientation;

        switch (_config._linearRangeOrientation)
        {
            case Orientation.Horizontal:
                Style.SpaceChar = new () { Grapheme = Glyphs.HLine.ToString () }; // '─'

                break;
            case Orientation.Vertical:
                Style.SpaceChar = new () { Grapheme = Glyphs.VLine.ToString () };

                break;
        }

        SetKeyBindings ();
        SetContentSize ();
    }

    #endregion

    /// <summary>Legends Orientation. <see cref="ViewBase.Orientation"></see></summary>
    public Orientation LegendsOrientation
    {
        get => _config._legendsOrientation;
        set
        {
            Orientation current = _config._legendsOrientation;

            CWPPropertyHelper.ChangeProperty (
                                              this,
                                              ref current,
                                              value,
                                              OnLegendsOrientationChanging,
                                              LegendsOrientationChanging,
                                              newValue =>
                                              {
                                                  _config._legendsOrientation = newValue;
                                                  SetContentSize ();
                                              },
                                              OnLegendsOrientationChanged,
                                              LegendsOrientationChanged,
                                              out Orientation _
                                             );
        }
    }

    /// <summary>Event raised before the <see cref="LegendsOrientation"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<Orientation>>? LegendsOrientationChanging;

    /// <summary>Event raised after the <see cref="LegendsOrientation"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<Orientation>>? LegendsOrientationChanged;

    /// <summary>Called before <see cref="LegendsOrientation"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnLegendsOrientationChanging (ValueChangingEventArgs<Orientation> args) => false;

    /// <summary>Called after <see cref="LegendsOrientation"/> has changed.</summary>
    protected virtual void OnLegendsOrientationChanged (ValueChangedEventArgs<Orientation> args) { }

    /// <summary>LinearRange styles. <see cref="LinearRangeStyle"></see></summary>
    public LinearRangeStyle Style { get; set; } = new ();

    /// <summary>Set the linear range options.</summary>
    public List<LinearRangeOption<T>> Options
    {
        get => _options ?? [];
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
            bool current = _config._showEndSpacing;

            CWPPropertyHelper.ChangeProperty (
                                              this,
                                              ref current,
                                              value,
                                              OnShowEndSpacingChanging,
                                              ShowEndSpacingChanging,
                                              newValue =>
                                              {
                                                  _config._showEndSpacing = newValue;
                                                  SetContentSize ();
                                              },
                                              OnShowEndSpacingChanged,
                                              ShowEndSpacingChanged,
                                              out bool _
                                             );
        }
    }

    /// <summary>Event raised before the <see cref="ShowEndSpacing"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? ShowEndSpacingChanging;

    /// <summary>Event raised after the <see cref="ShowEndSpacing"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? ShowEndSpacingChanged;

    /// <summary>Called before <see cref="ShowEndSpacing"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnShowEndSpacingChanging (ValueChangingEventArgs<bool> args) => false;

    /// <summary>Called after <see cref="ShowEndSpacing"/> has changed.</summary>
    protected virtual void OnShowEndSpacingChanged (ValueChangedEventArgs<bool> args) { }

    /// <summary>Show/Hide the options legends.</summary>
    public bool ShowLegends
    {
        get => _config._showLegends;
        set
        {
            bool current = _config._showLegends;

            CWPPropertyHelper.ChangeProperty (
                                              this,
                                              ref current,
                                              value,
                                              OnShowLegendsChanging,
                                              ShowLegendsChanging,
                                              newValue =>
                                              {
                                                  _config._showLegends = newValue;
                                                  SetContentSize ();
                                              },
                                              OnShowLegendsChanged,
                                              ShowLegendsChanged,
                                              out bool _
                                             );
        }
    }

    /// <summary>Event raised before the <see cref="ShowLegends"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? ShowLegendsChanging;

    /// <summary>Event raised after the <see cref="ShowLegends"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? ShowLegendsChanged;

    /// <summary>Called before <see cref="ShowLegends"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnShowLegendsChanging (ValueChangingEventArgs<bool> args) => false;

    /// <summary>Called after <see cref="ShowLegends"/> has changed.</summary>
    protected virtual void OnShowLegendsChanged (ValueChangedEventArgs<bool> args) { }

    /// <summary>
    ///     Gets or sets whether the minimum or ideal size will be used when calculating the size of the linear range.
    /// </summary>
    public bool UseMinimumSize
    {
        get => _config._useMinimumSize;
        set
        {
            bool current = _config._useMinimumSize;

            CWPPropertyHelper.ChangeProperty (
                                              this,
                                              ref current,
                                              value,
                                              OnUseMinimumSizeChanging,
                                              UseMinimumSizeChanging,
                                              newValue =>
                                              {
                                                  _config._useMinimumSize = newValue;
                                                  SetContentSize ();
                                              },
                                              OnUseMinimumSizeChanged,
                                              UseMinimumSizeChanged,
                                              out bool _
                                             );
        }
    }

    /// <summary>Event raised before the <see cref="UseMinimumSize"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? UseMinimumSizeChanging;

    /// <summary>Event raised after the <see cref="UseMinimumSize"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? UseMinimumSizeChanged;

    /// <summary>Called before <see cref="UseMinimumSize"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnUseMinimumSizeChanging (ValueChangingEventArgs<bool> args) => false;

    /// <summary>Called after <see cref="UseMinimumSize"/> has changed.</summary>
    protected virtual void OnUseMinimumSizeChanged (ValueChangedEventArgs<bool> args) { }

    #endregion

    #region Events

    /// <summary>Event raised when the linear range option/s changed. The dictionary contains: key = option index, value = T</summary>
    public event EventHandler<LinearRangeEventArgs<T>>? OptionsChanged;

    /// <summary>
    ///     Overridable method called when the linear range options have changed. Raises the <see cref="OptionsChanged"/>
    ///     event.
    /// </summary>
    public virtual void OnOptionsChanged ()
    {
        OptionsChanged?.Invoke (this, new (GetSetOptionDictionary ()));
        SetNeedsDraw ();
    }

    /// <summary>Event raised When the option is hovered with the keys or the mouse.</summary>
    public event EventHandler<LinearRangeEventArgs<T>>? OptionFocused;

    private int
        _lastFocusedOption; // for Range type; the most recently focused option. Used to determine shrink direction

    /// <summary>Overridable function that fires the <see cref="OptionFocused"/> event.</summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> if the focus change was cancelled.</returns>
    /// <param name="newFocusedOption"></param>
    public virtual bool OnOptionFocused (int newFocusedOption, LinearRangeEventArgs<T> args)
    {
        if (newFocusedOption > _options!.Count - 1 || newFocusedOption < 0)
        {
            return true;
        }

        OptionFocused?.Invoke (this, args);

        if (!args.Cancel)
        {
            _lastFocusedOption = FocusedOption;
            FocusedOption = newFocusedOption;
        }

        return args.Cancel;
    }

    #endregion Events

    #region Public Methods

    private int _focusedOption;

    /// <summary>The focused option (has the cursor).</summary>
    public int FocusedOption
    {
        get => _focusedOption;
        set
        {
            if (_focusedOption != value)
            {
                _focusedOption = value;
                UpdateCursor ();
            }
        }
    }

    /// <summary>Causes the specified option to be set and be focused.</summary>
    public bool SetOption (int optionIndex)
    {
        // TODO: Handle range type.
        // Note: Maybe return false only when optionIndex doesn't exist, otherwise true.

        if (!_setOptions.Contains (optionIndex) && optionIndex >= 0 && optionIndex < _options!.Count)
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
        AddRune (rune);
    }

    private void MoveAndAdd (int x, int y, string str)
    {
        Move (x, y);
        AddStr (str);
    }

    /// <summary>Sets the dimensions of the LinearRange to the ideal values.</summary>
    private void SetContentSize ()
    {
        if (_options is { Count: 0 })
        {
            return;
        }

        bool horizontal = _config._linearRangeOrientation == Orientation.Horizontal;

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

            if (_config._linearRangeOrientation == _config._legendsOrientation)
            {
                maxLegend = int.Max (_options!.Max (s => s.Legend?.GetColumns () ?? 1), 1);
            }
            else
            {
                maxLegend = 1;
            }

            int minSizeThatFitsLegends = _options!.Count == 1 ? maxLegend : _options.Sum (o => o.Legend!.GetColumns ());

            string? first;
            string? last;

            _config._showLegendsAbbr = false;

            if (minSizeThatFitsLegends > size)
            {
                if (_config._linearRangeOrientation == _config._legendsOrientation)
                {
                    _config._showLegendsAbbr = true;

                    foreach (LinearRangeOption<T> o in _options.Where (op => op.LegendAbbr == default (Rune)))
                    {
                        o.LegendAbbr = (Rune)(o.Legend?.GetColumns () > 0 ? o.Legend [0] : ' ');
                    }
                }

                first = "x";
                last = "x";
            }
            else
            {
                first = _options.First ().Legend;
                last = _options.Last ().Legend;
            }

            // --o--
            // Hello
            // Left = He
            // Right = lo
            int firstLeft = (first!.Length - 1) / 2; // Chars count of the first option to the left.
            int lastRight = last!.Length / 2; // Chars count of the last option to the right.

            if (_config._linearRangeOrientation != _config._legendsOrientation)
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
        if (_options is { Count: 0 })
        {
            return 0;
        }

        var length = 0;
        length += _config._startSpacing + _config._endSpacing;
        length += _options!.Count;
        length += (_options.Count - 1) * _config._minInnerSpacing;

        return length;
    }

    /// <summary>
    ///     Gets the ideal width of the linear range. The ideal width is the minimum width required to display all options and
    ///     inner
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
    ///     Gets the ideal height of the linear range. The ideal height is the minimum height required to display all options
    ///     and
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
        if (_options is { Count: 0 })
        {
            return 0;
        }

        var length = 0;

        if (!_config._showLegends)
        {
            return Math.Max (length, CalcMinLength ());
        }

        if (_config._legendsOrientation == _config._linearRangeOrientation && _options!.Count > 0)
        {
            // Each legend should be centered in a space the width of the longest legend, with one space between.
            // Calculate the total length required for all legends.
            int maxLegend = int.Max (_options.Max (s => s.Legend?.GetColumns () ?? 1), 1);
            length = maxLegend * _options.Count + (_options.Count - 1);
        }
        else
        {
            length = CalcMinLength ();
        }

        return Math.Max (length, CalcMinLength ());
    }

    /// <summary>
    ///     Calculates the minimum dimension required for the linear range and legends.
    /// </summary>
    /// <returns></returns>
    private int CalcIdealThickness ()
    {
        var thickness = 1; // Always show the linear range.

        if (!_config._showLegends)
        {
            return thickness;
        }

        if (_config._legendsOrientation != _config._linearRangeOrientation && _options!.Count > 0)
        {
            thickness += _options.Max (s => s.Legend?.GetColumns () ?? 0);
        }
        else
        {
            thickness += 1;
        }

        return thickness;
    }

    #endregion Helpers

    #region Cursor and Position

    internal bool TryGetPositionByOption (int option, out (int x, int y) position)
    {
        position = (-1, -1);

        if (option < 0 || option >= _options!.Count ())
        {
            return false;
        }

        var offset = 0;
        offset += _config._startSpacing;
        offset += option * (_config._cachedInnerSpacing + 1);

        if (_config._linearRangeOrientation == Orientation.Vertical)
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

                if (!valid || option < 0 || option > _options!.Count - 1)
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

                if (!valid || option < 0 || option > _options!.Count - 1)
                {
                    continue;
                }

                optionIdx = option;

                return true;
            }
        }

        return false;
    }

    /// <summary>Updates the cursor position based on the focused option.</summary>
    /// <remarks>
    ///     This method calculates the cursor position and calls <see cref="View.SetCursor"/>.
    ///     The framework automatically handles hiding the cursor when the view loses focus.
    /// </remarks>
    private void UpdateCursor ()
    {
        if (!TryGetPositionByOption (FocusedOption, out (int x, int y) position)
            || !IsInitialized
            || !Viewport.Contains (position.x, position.y))
        {
            SetCursor (new () { Position = null, Shape = Cursor.Shape }); // Hide cursor

            return;
        }

        SetCursor (
                   Cursor with
                   {
                       Position = ViewportToScreen (new Point (position.x, position.y)),
                       Shape = CursorShape.Default
                   });
    }

    #endregion Cursor and Position

    #region Drawing

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // TODO: make this more surgical to reduce repaint

        if (_options is null || _options.Count == 0)
        {
            return true;
        }

        // Draw LinearRange
        DrawLinearRange ();

        // Draw Legends.
        if (_config._showLegends)
        {
            DrawLegends ();
        }

        if (_dragPosition.HasValue && _moveRenderPosition.HasValue)
        {
            AddStr (_moveRenderPosition.Value.X, _moveRenderPosition.Value.Y, Style.DragChar.Grapheme);
        }

        return true;
    }

    private static string AlignText (string? text, int width, Alignment alignment)
    {
        if (string.IsNullOrEmpty (text))
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

    private void DrawLinearRange ()
    {
        // TODO: be more surgical on clear
        ClearViewport ();

        // Attributes
        var normalAttr = new Attribute (Color.White, Color.Black);
        var setAttr = new Attribute (Color.Black, Color.White);

        if (IsInitialized)
        {
            normalAttr = GetAttributeForRole (VisualRole.Normal);
            setAttr = Style.SetChar.Attribute ?? GetAttributeForRole (VisualRole.HotNormal);
        }

        bool isVertical = _config._linearRangeOrientation == Orientation.Vertical;

        var x = 0;
        var y = 0;

        bool isSet = _setOptions.Count > 0;

        // Left Spacing
        if (_config is { _showEndSpacing: true, _startSpacing: > 0 })
        {
            SetAttribute (
                          isSet && _config._type == LinearRangeType.LeftRange
                              ? Style.RangeChar.Attribute ?? normalAttr
                              : Style.SpaceChar.Attribute ?? normalAttr
                         );
            string text = isSet && _config._type == LinearRangeType.LeftRange ? Style.RangeChar.Grapheme : Style.SpaceChar.Grapheme;

            for (var i = 0; i < _config._startSpacing; i++)
            {
                MoveAndAdd (x, y, text);

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
            SetAttribute (Style.EmptyChar.Attribute ?? normalAttr);

            for (var i = 0; i < _config._startSpacing; i++)
            {
                MoveAndAdd (x, y, Style.EmptyChar.Grapheme);

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

        // LinearRange
        if (_options!.Count > 0)
        {
            for (var i = 0; i < _options.Count; i++)
            {
                var drawRange = false;

                if (isSet)
                {
                    switch (_config._type)
                    {
                        case LinearRangeType.LeftRange when i <= _setOptions [0]:
                            drawRange = i < _setOptions [0];

                            break;
                        case LinearRangeType.RightRange when i >= _setOptions [0]:
                            drawRange = i >= _setOptions [0];

                            break;
                        case LinearRangeType.Range when _setOptions.Count == 1:
                            drawRange = false;

                            break;
                        case LinearRangeType.Range when _setOptions.Count == 2:
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
                SetAttribute (
                              isSet && _setOptions.Contains (i) ? Style.SetChar.Attribute ?? setAttr :
                              drawRange ? Style.RangeChar.Attribute ?? setAttr : Style.OptionChar.Attribute ?? normalAttr
                             );

                string text = drawRange ? Style.RangeChar.Grapheme : Style.OptionChar.Grapheme;

                if (isSet)
                {
                    if (_setOptions [0] == i)
                    {
                        text = Style.StartRangeChar.Grapheme;
                    }
                    else if (_setOptions.Count > 1 && _setOptions [1] == i)
                    {
                        text = Style.EndRangeChar.Grapheme;
                    }
                    else if (_setOptions.Contains (i))
                    {
                        text = Style.SetChar.Grapheme;
                    }
                }

                MoveAndAdd (x, y, text);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }

                // Draw Spacing
                if (!_config._showEndSpacing && i >= _options.Count - 1)
                {
                    continue;
                }

                // Skip if is the Last Spacing.
                SetAttribute (
                              drawRange && isSet
                                  ? Style.RangeChar.Attribute ?? setAttr
                                  : Style.SpaceChar.Attribute ?? normalAttr
                             );

                for (var s = 0; s < _config._cachedInnerSpacing; s++)
                {
                    MoveAndAdd (x, y, drawRange && isSet ? Style.RangeChar.Grapheme : Style.SpaceChar.Grapheme);

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

        int remaining = isVertical ? Viewport.Height - y : Viewport.Width - x;

        // Right Spacing
        if (_config._showEndSpacing)
        {
            SetAttribute (
                          isSet && _config._type == LinearRangeType.RightRange
                              ? Style.RangeChar.Attribute ?? normalAttr
                              : Style.SpaceChar.Attribute ?? normalAttr
                         );
            string text = isSet && _config._type == LinearRangeType.RightRange ? Style.RangeChar.Grapheme : Style.SpaceChar.Grapheme;

            for (var i = 0; i < remaining; i++)
            {
                MoveAndAdd (x, y, text);

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
            SetAttribute (Style.EmptyChar.Attribute ?? normalAttr);

            for (var i = 0; i < remaining; i++)
            {
                MoveAndAdd (x, y, Style.EmptyChar.Grapheme);

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
            normalAttr = Style.LegendAttributes.NormalAttribute ?? GetAttributeForRole (VisualRole.Normal);
            setAttr = Style.LegendAttributes.SetAttribute ?? GetAttributeForRole (VisualRole.HotNormal);
            spaceAttr = Style.LegendAttributes.EmptyAttribute ?? normalAttr;
        }

        bool isTextVertical = _config._legendsOrientation == Orientation.Vertical;
        bool isSet = _setOptions.Count > 0;

        var x = 0;
        var y = 0;

        Move (x, y);

        switch (_config._linearRangeOrientation)
        {
            case Orientation.Horizontal
                when _config._legendsOrientation == Orientation.Vertical:
                x += _config._startSpacing;

                break;
            case Orientation.Vertical
                when _config._legendsOrientation == Orientation.Horizontal:
                y += _config._startSpacing;

                break;
        }

        if (_config._linearRangeOrientation == Orientation.Horizontal)
        {
            y += 1;
        }
        else
        {
            // Vertical
            x += 1;
        }

        for (var i = 0; i < _options!.Count; i++)
        {
            var isOptionSet = false;

            // Check if the Option is Set.
            switch (_config._type)
            {
                case LinearRangeType.Single:
                case LinearRangeType.Multiple:
                    if (isSet && _setOptions.Contains (i))
                    {
                        isOptionSet = true;
                    }

                    break;
                case LinearRangeType.LeftRange:
                    if (isSet && i <= _setOptions [0])
                    {
                        isOptionSet = true;
                    }

                    break;
                case LinearRangeType.RightRange:
                    if (isSet && i >= _setOptions [0])
                    {
                        isOptionSet = true;
                    }

                    break;
                case LinearRangeType.Range when _setOptions.Count == 1:
                    if (isSet && i == _setOptions [0])
                    {
                        isOptionSet = true;
                    }

                    break;
                case LinearRangeType.Range:
                    if (isSet
                        && ((i >= _setOptions [0] && i <= _setOptions [1])
                            || (i >= _setOptions [1] && i <= _setOptions [0])))
                    {
                        isOptionSet = true;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException ();
            }

            // Text || Abbreviation

            string text = (_config._showLegendsAbbr ? _options [i].LegendAbbr.ToString () : _options [i].Legend)!;

            switch (_config._linearRangeOrientation)
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

            // Calculate Start Spacing
            if (_config._linearRangeOrientation == _config._legendsOrientation)
            {
                if (i == 0)
                {
                    // The spacing for the linear range use the StartSpacing but...
                    // The spacing for the legends is the StartSpacing MINUS the total chars to the left of the first options.
                    //    ●────●────●
                    //  Hello Bye World
                    //
                    // chars_left is 2 for Hello => (5 - 1) / 2
                    //
                    // then the spacing is 2 for the linear range but 0 for the legends.

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
            }

            // Legend
            SetAttribute (isOptionSet ? setAttr : normalAttr);

            foreach (Rune c in text.EnumerateRunes ())
            {
                MoveAndAdd (x, y, c);

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
            if (i == _options.Count - 1)
            {
                // See Start Spacing explanation.
                int charsRight = text.Length / 2;
                legendRightSpacesCount = _config._endSpacing - charsRight;
            }

            // Option Right Spacing of Option
            SetAttribute (spaceAttr);

            if (isTextVertical)
            {
                y += legendRightSpacesCount;
            }
            else
            {
                x += legendRightSpacesCount;
            }

            switch (_config._linearRangeOrientation)
            {
                case Orientation.Horizontal
                    when _config._legendsOrientation == Orientation.Vertical:
                    x += _config._cachedInnerSpacing + 1;

                    break;
                case Orientation.Vertical
                    when _config._legendsOrientation == Orientation.Horizontal:
                    y += _config._cachedInnerSpacing + 1;

                    break;
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
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (!(mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked)
              || mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed)
              || mouse.Flags.HasFlag (MouseFlags.PositionReport)
              || mouse.Flags.HasFlag (MouseFlags.LeftButtonReleased)))
        {
            return false;
        }

        SetFocus ();

        if (!_dragPosition.HasValue && mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            if (mouse.Flags.HasFlag (MouseFlags.PositionReport))
            {
                _dragPosition = mouse.Position;
                _moveRenderPosition = ClampMovePosition ((Point)_dragPosition!);
                App?.Mouse.GrabMouse (this);
            }

            SetNeedsDraw ();

            return true;
        }

        bool success;
        int option;

        if (_dragPosition.HasValue
            && mouse.Flags.HasFlag (MouseFlags.PositionReport)
            && mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            // Continue Drag
            _dragPosition = mouse.Position;
            _moveRenderPosition = ClampMovePosition ((Point)_dragPosition!);

            // how far has user dragged from original location?
            if (Orientation == Orientation.Horizontal)
            {
                success = TryGetOptionByPosition (mouse.Position!.Value.X, 0, Math.Max (0, _config._cachedInnerSpacing / 2), out option);
            }
            else
            {
                success = TryGetOptionByPosition (0, mouse.Position!.Value.Y, Math.Max (0, _config._cachedInnerSpacing / 2), out option);
            }

            if (!_config._allowEmpty && success)
            {
                if (!OnOptionFocused (option, new (GetSetOptionDictionary (), FocusedOption)))
                {
                    SetFocusedOption ();
                }
            }

            SetNeedsDraw ();

            return true;
        }

        if ((_dragPosition.HasValue && mouse.Flags.HasFlag (MouseFlags.LeftButtonReleased))
            || mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
        {
            return mouse.Handled;
        }

        // End Drag
        App?.Mouse.UngrabMouse ();
        _dragPosition = null;
        _moveRenderPosition = null;

        switch (Orientation)
        {
            case Orientation.Horizontal:
                success = TryGetOptionByPosition (mouse.Position!.Value.X, 0, Math.Max (0, _config._cachedInnerSpacing / 2), out option);

                break;
            default:
                success = TryGetOptionByPosition (0, mouse.Position!.Value.Y, Math.Max (0, _config._cachedInnerSpacing / 2), out option);

                break;
        }

        if (success)
        {
            if (!OnOptionFocused (option, new (GetSetOptionDictionary (), FocusedOption)))
            {
                SetFocusedOption ();
            }
        }

        SetNeedsDraw ();

        mouse.Handled = true;

        return mouse.Handled;

        Point ClampMovePosition (Point position)
        {
            if (Orientation == Orientation.Horizontal)
            {
                int left = _config._startSpacing;
                int width = _options!.Count + (_options.Count - 1) * _config._cachedInnerSpacing;
                int right = left + width - 1;
                int clampedX = Clamp (position.X, left, right);
                position = new (clampedX, 0);
            }
            else
            {
                int top = _config._startSpacing;
                int height = _options!.Count + (_options.Count - 1) * _config._cachedInnerSpacing;
                int bottom = top + height - 1;
                int clampedY = Clamp (position.Y, top, bottom);
                position = new (0, clampedY);
            }

            return position;

            static int Clamp (int value, int min, int max) { return Math.Max (min, Math.Min (max, value)); }
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
        AddCommand (Command.Activate, () => Select ());
        AddCommand (Command.Accept, ctx => Accept (ctx));

        SetKeyBindings ();
    }

    // This is called during initialization and anytime orientation changes
    private void SetKeyBindings ()
    {
        if (_config._linearRangeOrientation == Orientation.Horizontal)
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
        KeyBindings.Add (Key.Space, Command.Activate);
    }

    private Dictionary<int, LinearRangeOption<T>> GetSetOptionDictionary () { return _setOptions.ToDictionary (e => e, e => _options! [e]); }

    /// <summary>
    ///     Sets or unsets <paramref name="optionIndex"/> based on <paramref name="set"/>.
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

                _options? [optionIndex].OnSet ();
            }
        }
        else
        {
            if (_setOptions.Contains (optionIndex))
            {
                _setOptions.Remove (optionIndex);

                _options? [optionIndex].OnUnSet ();
            }
        }

        // Raise slider changed event.
        OnOptionsChanged ();
    }

    private bool SetFocusedOption ()
    {
        if (_options is null or { Count: 0 })
        {
            return false;
        }

        var changed = false;

        switch (_config._type)
        {
            case LinearRangeType.Single:
            case LinearRangeType.LeftRange:
            case LinearRangeType.RightRange:

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
            case LinearRangeType.Multiple:
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

            case LinearRangeType.Range:
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

                // Raise LinearRange Option Changed Event.
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
        int next = _options is { } && FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption;

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

    internal bool Select () => SetFocusedOption ();

    internal bool Accept (ICommandContext? commandContext)
    {
        SetFocusedOption ();

        return RaiseAccepting (commandContext) == true;
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
        if (OnOptionFocused (_options!.Count - 1, new (GetSetOptionDictionary (), FocusedOption)))
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
