#nullable enable

namespace Terminal.Gui;

// TODO: Declare support for INotifyPropertyChanging and INotifyPropertyChanged when all properties are wired up for it.
/// <summary>
///     True color picker using HSL
/// </summary>
[MustDisposeResource]
public sealed class ColorPicker : View
{
    /// <summary>
    ///     Creates a new instance of <see cref="ColorPicker"/>. Use
    ///     <see cref="Style"/> to change color model. Use <see cref="SelectedColor"/>
    ///     to change initial <see cref="Color"/>.
    /// </summary>
    public ColorPicker ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabStop;
        Height = Dim.Auto ();
        Width = Dim.Auto ();
        ApplyStyleChanges ();
    }

    private readonly Dictionary<IColorBar, TextField> _textFields = [];
    private readonly ColorModelStrategy _strategy = new ();
    private TextField? _tfHex;
    private Label? _lbHex;

    private TextField? _tfName;
    private Label? _lbName;

    private Color _selectedColor = Color.Black;

    // TODO: Add interface
    private readonly IColorNameResolver _colorNameResolver = new W3CColors ();

    private List<IColorBar> _bars = [];

    /// <summary>
    ///     Rebuild the user interface to reflect the new state of <see cref="Style"/>.
    /// </summary>
    public void ApplyStyleChanges ()
    {
        Color oldValue = _selectedColor;
        DisposeOldViews ();

        var y = 0;
        const int DEFAULT_TEXT_FIELD_WIDTH = 4;

        foreach (ColorBar bar in _strategy.CreateBars (Style.ColorModel))
        {
            bar.Y = y;
            bar.Width = Dim.Fill (Style.ShowTextFields ? DEFAULT_TEXT_FIELD_WIDTH : 0);

            TextField? tfValue = null;

            if (Style.ShowTextFields)
            {
                tfValue = new()
                {
                    X = Pos.AnchorEnd (DEFAULT_TEXT_FIELD_WIDTH),
                    Y = y,
                    Width = DEFAULT_TEXT_FIELD_WIDTH
                };
                tfValue.HasFocusChanged += UpdateSingleBarValueFromTextField;
                tfValue.Accept += (s, _)=>UpdateSingleBarValueFromTextField(s);
                _textFields.Add (bar, tfValue);
            }

            y++;

            bar.ValueChanged += RebuildColorFromBar;

            _bars.Add (bar);

            Add (bar);

            if (tfValue is { })
            {
                Add (tfValue);
            }
        }

        if (Style.ShowColorName)
        {
            CreateNameField ();
        }

        CreateTextField ();
        SelectedColor = oldValue;

        if (IsInitialized)
        {
            LayoutSubviews ();
        }
    }

    /// <summary>
    ///     Fired when color is changed.
    /// </summary>
    public event EventHandler<ColorEventArgs>? ColorChanged;

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);
        Attribute normal = GetNormalColor ();
        Driver.SetAttribute (new (SelectedColor, normal.Background));
        int y = _bars.Count + (Style.ShowColorName ? 1 : 0);
        AddRune (13, y, (Rune)'■');
    }

    /// <summary>
    ///     The color selected in the picker
    /// </summary>
    public Color SelectedColor
    {
        get => _selectedColor;
        set => SetSelectedColor (value, true);
    }

    private ColorPickerStyle _style = new ();

    /// <summary>
    ///     Style settings for the color picker.  After making changes ensure you call
    ///     <see cref="ApplyStyleChanges"/>.
    /// </summary>
    public ColorPickerStyle Style
    {
        get => _style;
        set => SetField (ref _style, value);
    }

    private void CreateNameField ()
    {
        _lbName = new ()
        {
            Text = "Name:",
            X = 0,
            Y = 3
        };

        _tfName = new ()
        {
            Y = 3,
            X = 6,
            Width = 20 // width of "LightGoldenRodYellow" - the longest w3c color name
        };

        Add (_lbName);
        Add (_tfName);

        _tfName.Autocomplete = new AppendAutocomplete (_tfName)
        {
            SuggestionGenerator = new SingleWordSuggestionGenerator
            {
                AllSuggestions = _colorNameResolver.GetColorNames ().ToList ()
            }
        };

        _tfName.HasFocusChanged += UpdateValueFromName;
        _tfName.Accept += (s, _) => UpdateValueFromName ();
    }

    private void CreateTextField ()
    {
        int y = _bars.Count;

        if (Style.ShowColorName)
        {
            y++;
        }

        _lbHex = new ()
        {
            Text = "Hex:",
            X = 0,
            Y = y
        };

        _tfHex = new ()
        {
            Y = y,
            X = 4,
            Width = 8,
        };

        Add (_lbHex);
        Add (_tfHex);

        _tfHex.HasFocusChanged += UpdateValueFromTextField;
        _tfHex.Accept += (_,_)=> UpdateValueFromTextField();
    }

    // BUG: CONCURRENCY: This needs to be synchronized.
    // The way this method accesses th fields is dangerous.
    /// <exception cref="InvalidCastException">An element in the sequence cannot be cast to type <see name="ColorBar" />.</exception>
    private void DisposeOldViews ()
    {
        foreach (ColorBar bar in _bars.Cast<ColorBar> ())
        {
            bar.ValueChanged -= RebuildColorFromBar;

            if (_textFields.TryGetValue (bar, out TextField? tf))
            {
                Remove (tf);
                tf.Dispose ();
            }

            Remove (bar);
            bar.Dispose ();
        }

        // We might as well do this earlier.
        // Grab a reference to the previous list, then assign a new list, then run the loop over the local reference to the old list.
        _bars = [];
        _textFields.Clear ();

        if (_lbHex is { })
        {
            Remove (_lbHex);
            _lbHex.Dispose ();
            _lbHex = null;
        }

        if (_tfHex is { })
        {
            Remove (_tfHex);
            _tfHex.Dispose ();
            _tfHex = null;
        }

        if (_lbName is { })
        {
            Remove (_lbName);
            _lbName.Dispose ();
            _lbName = null;
        }

        if (_tfName is { })
        {
            Remove (_tfName);
            _tfName.Dispose ();
            _tfName = null;
        }
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private void RebuildColorFromBar (object? sender, EventArgs<int> e)
    {
        SetSelectedColor (_strategy.GetColorFromBars (_bars, Style.ColorModel), false);
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private void SetSelectedColor (Color value, bool syncBars)
    {
        if (_selectedColor != value)
        {
            RaisePropertyChanging (this, new (nameof (SelectedColor)));
            _selectedColor = value;
            RaisePropertyChanged (this, new (nameof (SelectedColor)));

            ColorChanged?.Invoke (this, new (value));
        }

        SyncSubViewValues (syncBars);
    }

    private void SyncSubViewValues (bool syncBars)
    {
        if (syncBars)
        {
            _strategy.SetBarsToColor (_bars, _selectedColor, Style.ColorModel);
        }

        foreach (KeyValuePair<IColorBar, TextField> kvp in _textFields)
        {
            kvp.Value.Text = kvp.Key.Value.ToString ();
        }

        var colorHex = _selectedColor.ToString ($"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}");

        if (_tfName != null)
        {
            _tfName.Text = _colorNameResolver.TryNameColor (_selectedColor, out string name) ? name : string.Empty;
        }

        if (_tfHex != null)
        {
            _tfHex.Text = colorHex;
        }
    }

    private void UpdateSingleBarValueFromTextField (object? sender, HasFocusEventArgs e)
    {
        // if the new value of Focused is true then it is an enter event so ignore
        if (e.NewValue)
        {
            return;
        }

        // it is a leave event so update
        UpdateSingleBarValueFromTextField (sender);
    }
    private void UpdateSingleBarValueFromTextField (object? sender)
    {

        foreach (KeyValuePair<IColorBar, TextField> kvp in _textFields)
        {
            if (kvp.Value == sender)
            {
                if (int.TryParse (kvp.Value.Text, out int v))
                {
                    kvp.Key.Value = v;
                }
            }
        }
    }

    private void UpdateValueFromName (object? sender, HasFocusEventArgs e)
    {
        // if the new value of Focused is true then it is an enter event so ignore
        if (e.NewValue)
        {
            return;
        }

        // it is a leave event so update
        UpdateValueFromName();
    }
    private void UpdateValueFromName ()
    {
        if (_tfName == null)
        {
            return;
        }

        if (_colorNameResolver.TryParseColor (_tfName.Text, out Color newColor))
        {
            SelectedColor = newColor;
        }
        else
        {
            // value is invalid, revert the value in the text field back to current state
            SyncSubViewValues (false);
        }
    }

    private void UpdateValueFromTextField (object? sender, HasFocusEventArgs e)
    {
        // if the new value of Focused is true then it is an enter event so ignore
        if (e.NewValue)
        {
            return;
        }

        // it is a leave event so update
        UpdateValueFromTextField ();
    }
    private void UpdateValueFromTextField ()
    {
        if (_tfHex == null)
        {
            return;
        }

        if (Color.TryParse (_tfHex.Text, out Color? newColor))
        {
            SelectedColor = newColor.Value;
        }
        else
        {
            // value is invalid, revert the value in the text field back to current state
            SyncSubViewValues (false);
        }
    }

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        DisposeOldViews ();
        base.Dispose (disposing);
    }
}
