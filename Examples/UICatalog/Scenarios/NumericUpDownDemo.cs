#nullable enable 
using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("NumericUpDown", "Demonstrates the NumericUpDown View")]
[ScenarioCategory ("Controls")]
public class NumericUpDownDemo : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        NumericUpDownEditor<int> intEditor = new ()
        {
            X = 0,
            Y = 0,
            Title = "int",
        };

        app.Add (intEditor);

        NumericUpDownEditor<float> floatEditor = new ()
        {
            X = Pos.Right (intEditor),
            Y = 0,
            Title = "float",
        };
        app.Add (floatEditor);

        app.Initialized += AppInitialized;

        void AppInitialized (object? sender, EventArgs e)
        {
            floatEditor!.NumericUpDown!.Increment = 0.1F;
            floatEditor!.NumericUpDown!.Format = "{0:0.0}";
        }

        intEditor.SetFocus ();

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}

internal class NumericUpDownEditor<T> : View where T : notnull
{
    private NumericUpDown<T>? _numericUpDown;

    internal NumericUpDown<T>? NumericUpDown
    {
        get => _numericUpDown;
        set
        {
            if (value == _numericUpDown)
            {
                return;
            }
            _numericUpDown = value;

            if (_numericUpDown is { } && _value is { })
            {
                _value.Text = _numericUpDown.Text;
            }
        }
    }

    private TextField? _value;
    private TextField? _format;
    private TextField? _increment;

    internal NumericUpDownEditor ()
    {
        _numericUpDown = null;
        Title = "NumericUpDownEditor";
        BorderStyle = LineStyle.Single;
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        TabStop = TabBehavior.TabGroup;
        CanFocus = true;

        Initialized += NumericUpDownEditorInitialized;

        return;

        void NumericUpDownEditorInitialized (object? sender, EventArgs e)
        {
            Label label = new ()
            {
                Title = "_Value: ",
                Width = 12,
            };
            label.TextFormatter.Alignment = Alignment.End;
            _value = new ()
            {
                X = Pos.Right (label),
                Y = Pos.Top (label),
                Width = 8,
                Title = "Value",
            };
            _value.Accepting += ValuedOnAccept;

            void ValuedOnAccept (object? sender, EventArgs e)
            {
                if (_numericUpDown is null)
                {
                    return;
                }

                try
                {
                    if (string.IsNullOrEmpty (_value.Text))
                    {
                        // Handle empty or null text if needed
                        _numericUpDown.Value = default!;
                    }
                    else
                    {
                        // Parse _value.Text and then convert to type T
                        _numericUpDown.Value = (T)Convert.ChangeType (_value.Text, typeof (T));
                    }

                    _value.ColorScheme = SuperView!.ColorScheme;

                }
                catch (System.FormatException)
                {
                    _value.ColorScheme = Colors.ColorSchemes ["Error"];
                }
                catch (InvalidCastException)
                {
                    _value.ColorScheme = Colors.ColorSchemes ["Error"];
                }
                finally
                {
                }

            }
            Add (label, _value);

            label = new ()
            {
                Y = Pos.Bottom (_value),
                Width = 12,
                Title = "_Format: ",
            };
            label.TextFormatter.Alignment = Alignment.End;

            _format = new ()
            {
                X = Pos.Right (label),
                Y = Pos.Top (label),
                Title = "Format",
                Width = Dim.Width (_value),
            };
            _format.Accepting += FormatOnAccept;

            void FormatOnAccept (object? o, EventArgs eventArgs)
            {
                if (_numericUpDown is null)
                {
                    return;
                }

                try
                {
                    // Test format to ensure it's valid
                    _ = string.Format (_format.Text, _value);
                    _numericUpDown.Format = _format.Text;

                    _format.ColorScheme = SuperView!.ColorScheme;

                }
                catch (System.FormatException)
                {
                    _format.ColorScheme = Colors.ColorSchemes ["Error"];
                }
                catch (InvalidCastException)
                {
                    _format.ColorScheme = Colors.ColorSchemes ["Error"];
                }
                finally
                {
                }
            }

            Add (label, _format);

            label = new ()
            {
                Y = Pos.Bottom (_format),
                Width = 12,
                Title = "_Increment: ",
            };
            label.TextFormatter.Alignment = Alignment.End;
            _increment = new ()
            {
                X = Pos.Right (label),
                Y = Pos.Top (label),
                Title = "Increment",
                Width = Dim.Width (_value),
            };

            _increment.Accepting += IncrementOnAccept;

            void IncrementOnAccept (object? o, EventArgs eventArgs)
            {
                if (_numericUpDown is null)
                {
                    return;
                }

                try
                {
                    if (string.IsNullOrEmpty (_value.Text))
                    {
                        // Handle empty or null text if needed
                        _numericUpDown.Increment = default!;
                    }
                    else
                    {
                        // Parse _value.Text and then convert to type T
                        _numericUpDown.Increment = (T)Convert.ChangeType (_increment.Text, typeof (T));
                    }

                    _increment.ColorScheme = SuperView!.ColorScheme;

                }
                catch (System.FormatException)
                {
                    _increment.ColorScheme = Colors.ColorSchemes ["Error"];
                }
                catch (InvalidCastException)
                {
                    _increment.ColorScheme = Colors.ColorSchemes ["Error"];
                }
                finally
                {
                }
            }

            Add (label, _increment);

            _numericUpDown = new ()
            {
                X = Pos.Center (),
                Y = Pos.Bottom (_increment) + 1,
                Increment = (dynamic)1,
            };

            _numericUpDown.ValueChanged += NumericUpDownOnValueChanged;

            void NumericUpDownOnValueChanged (object? o, EventArgs<T> eventArgs)
            {
                _value.Text = _numericUpDown.Text;
            }

            _numericUpDown.IncrementChanged += NumericUpDownOnIncrementChanged;

            void NumericUpDownOnIncrementChanged (object? o, EventArgs<T> eventArgs)
            {
                _increment.Text = _numericUpDown.Increment.ToString ();
            }

            Add (_numericUpDown);

            _value.Text = _numericUpDown.Text;
            _format.Text = _numericUpDown.Format;
            _increment.Text = _numericUpDown.Increment.ToString ();
        }
    }


}
