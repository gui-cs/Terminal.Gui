#nullable enable


namespace Terminal.Gui.Views;

/// <summary>
///     Color Picker supporting RGB, HSL, and HSV color models. Supports choosing colors with
///     sliders and color names from the <see cref="IColorNameResolver"/>.
/// </summary>
public partial class ColorPicker : View, IDesignable
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
        Width = Dim.Auto (minimumContentDim: 32);
        ApplyStyleChanges ();
    }

    private readonly Dictionary<IColorBar, TextField> _textFields = new ();
    private readonly ColorModelStrategy _strategy = new ();
    private TextField? _tfHex;
    private Label? _lbHex;

    private TextField? _tfName;
    private Label? _lbName;

    private Color _selectedColor = Color.Black;

    // TODO: Add interface
    private readonly IColorNameResolver _colorNameResolver = new MultiStandardColorNameResolver ();

    private List<IColorBar> _bars = new ();

    /// <summary>
    ///     Rebuild the user interface to reflect the new state of <see cref="Style"/>.
    /// </summary>
    public void ApplyStyleChanges ()
    {
        Color oldValue = _selectedColor;
        DisposeOldViews ();

        var y = 0;
        const int textFieldWidth = 4;

        foreach (ColorBar bar in _strategy.CreateBars (Style.ColorModel))
        {
            bar.Y = y;
            bar.Width = Dim.Fill (Style.ShowTextFields ? textFieldWidth : 0);

            TextField? tfValue = null;
            if (Style.ShowTextFields)
            {
                tfValue = new TextField
                {
                    X = Pos.AnchorEnd (textFieldWidth),
                    Y = y,
                    Width = textFieldWidth
                };
                tfValue.HasFocusChanged += UpdateSingleBarValueFromTextField;
                tfValue.Accepting += (s, _) => UpdateSingleBarValueFromTextField (s);
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

        SetNeedsLayout ();
    }

    /// <summary>
    ///     Fired when color is changed.
    /// </summary>
    public event EventHandler<ResultEventArgs<Color>>? ColorChanged;

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        Attribute normal = GetAttributeForRole (VisualRole.Normal);
        SetAttribute (new (SelectedColor, normal.Background, Enabled ? TextStyle.None : TextStyle.Faint));
        int y = _bars.Count + (Style.ShowColorName ? 1 : 0);
        AddRune (13, y, (Rune)'■');

        return true;
    }

    /// <summary>
    ///     The color selected in the picker
    /// </summary>
    public Color SelectedColor
    {
        get => _selectedColor;
        set => SetSelectedColor (value, true);
    }

    /// <summary>
    ///     Style settings for the color picker.  After making changes ensure you call
    ///     <see cref="ApplyStyleChanges"/>.
    /// </summary>
    public ColorPickerStyle Style { get; set; } = new ();

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

        var auto = new AppendAutocomplete (_tfName);

        auto.SuggestionGenerator = new SingleWordSuggestionGenerator
        {
            AllSuggestions = _colorNameResolver.GetColorNames ().ToList ()
        };
        _tfName.Autocomplete = auto;

        _tfName.HasFocusChanged += UpdateValueFromName;
        _tfName.Accepting += (s, _) => UpdateValueFromName ();
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
        _tfHex.Accepting += (_, _) => UpdateValueFromTextField ();
    }

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

        _bars = new ();
        _textFields.Clear ();

        if (_lbHex != null)
        {
            Remove (_lbHex);
            _lbHex.Dispose ();
            _lbHex = null;
        }

        if (_tfHex != null)
        {
            Remove (_tfHex);
            _tfHex.Dispose ();
            _tfHex = null;
        }

        if (_lbName != null)
        {
            Remove (_lbName);
            _lbName.Dispose ();
            _lbName = null;
        }

        if (_tfName != null)
        {
            Remove (_tfName);
            _tfName.Dispose ();
            _tfName = null;
        }
    }

    private void RebuildColorFromBar (object? sender, EventArgs<int> e) { SetSelectedColor (_strategy.GetColorFromBars (_bars, Style.ColorModel), false); }

    private void SetSelectedColor (Color value, bool syncBars)
    {
        if (_selectedColor != value)
        {
            Color old = _selectedColor;
            _selectedColor = value;

            ColorChanged?.Invoke (
                                  this,
                                  new (value));
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
            _tfName.Text = _colorNameResolver.TryNameColor (_selectedColor, out string? name) ? name : string.Empty;
        }

        if (_tfHex != null)
        {
            _tfHex.Text = colorHex;
        }

        SetNeedsLayout ();
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
        UpdateValueFromName ();
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

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        SelectedColor = Color.BrightRed;

        return true;
    }

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        DisposeOldViews ();
        base.Dispose (disposing);
    }
}
