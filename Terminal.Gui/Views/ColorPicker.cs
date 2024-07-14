namespace Terminal.Gui;

/// <summary>
///     True color picker using HSL
/// </summary>
public class ColorPicker : View
{
    private TextField _tfHex;
    private Label _lbHex;

    private Color _selectedColor = Color.Black;

    private List<IColorBar> _bars = new ();
    private readonly Dictionary<IColorBar, TextField> _textFields = new ();
    private readonly ColorModelStrategy _strategy = new ();

    /// <summary>
    ///     Style settings for the color picker.  After making changes ensure you call
    ///     <see cref="ApplyStyleChanges"/>.
    /// </summary>
    public ColorPickerStyle Style { get; set; } = new ();

    /// <summary>
    ///     Fired when color is changed.
    /// </summary>
    public event EventHandler<ColorEventArgs> ColorChanged;

    private bool _updating;

    /// <summary>
    ///     The color selected in the picker
    /// </summary>
    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            try
            {
                _updating = true;

                if (_selectedColor != value)
                {
                    Color old = _selectedColor;
                    _selectedColor = value;
                    SetTextFieldToValue ();
                    UpdateBarsFromColor (value);

                    ColorChanged?.Invoke (
                                          this,
                                          new()
                                          {
                                              Color = value,
                                              PreviousColor = old
                                          });
                }
            }
            finally
            {
                _updating = false;
            }
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="ColorPicker"/>. Use
    /// <see cref="Style"/> to change color model. Use <see cref="SelectedColor"/>
    /// to change initial <see cref="Color"/>.
    /// </summary>
    public ColorPicker ()
    {
        CanFocus = true;
        Height = 4;
        Width = Dim.Fill ();
        ApplyStyleChanges ();
    }

    /// <summary>
    /// Rebuild the user interface to reflect the new state of <see cref="Style"/>.
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

            if (Style.ShowTextFields)
            {
                var tfValue = new TextField
                {
                    X = Pos.AnchorEnd (textFieldWidth),
                    Y = y,
                    Width = textFieldWidth
                };
                tfValue.Leave += UpdateSingleBarValueFromTextField;
                _textFields.Add (bar, tfValue);
                Add (tfValue);
            }

            y++;

            bar.ValueChanged += RebuildColor;

            _bars.Add (bar);

            Add (bar);
        }

        CreateTextField ();
        SetTextFieldToValue ();

        UpdateBarsFromColor (SelectedColor);
        RebuildColor (this, default (EventArgs<int>));
        SelectedColor = oldValue;
    }

    private void CreateTextField ()
    {
        _lbHex = new()
        {
            Text = "Hex:",
            X = 0,
            Y = 3
        };

        _tfHex = new()
        {
            Y = 3,
            X = 4,
            Width = 8
        };

        Add (_lbHex);
        Add (_tfHex);

        _tfHex.Leave += UpdateValueFromTextField;
    }

    private void UpdateSingleBarValueFromTextField (object sender, FocusEventArgs e)
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

    private void DisposeOldViews ()
    {
        foreach (ColorBar bar in _bars.Cast<ColorBar> ())
        {
            bar.ValueChanged -= RebuildColor;

            if (_textFields.TryGetValue (bar, out TextField tf))
            {
                tf.Leave -= UpdateSingleBarValueFromTextField;
                Remove (tf);
                tf.Dispose ();
            }

            Remove (bar);
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
            _tfHex.Leave -= UpdateValueFromTextField;
            _tfHex.Dispose ();
            _tfHex = null;
        }
    }

    private void UpdateValueFromTextField (object sender, FocusEventArgs e)
    {
        if (Color.TryParse (_tfHex.Text, out Color? newColor))
        {
            SelectedColor = newColor.Value;
        }
        else
        {
            // value is invalid, revert the value in the text field back to current state
            SetTextFieldToValue ();
        }
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);
        Attribute normal = GetNormalColor ();
        Driver.SetAttribute (new (SelectedColor, normal.Background));
        AddRune (13, 3, (Rune)'■');
    }

    private void UpdateBarsFromColor (Color color)
    {
        _strategy.SetBarsToColor (_bars, color, Style.ColorModel);
        SetTextFieldToValue ();
    }

    private void RebuildColor (object sender, EventArgs<int> e)
    {
        foreach (KeyValuePair<IColorBar, TextField> kvp in _textFields)
        {
            kvp.Value.Text = kvp.Key.Value.ToString ();
        }

        if (!_updating)
        {
            SelectedColor = _strategy.GetColorFromBars (_bars, Style.ColorModel);
        }

        SetTextFieldToValue ();
    }

    private void SetTextFieldToValue () { _tfHex.Text = _selectedColor.ToString ($"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}"); }
}