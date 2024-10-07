#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Enables the user to increase or decrease a value with the mouse or keyboard.
/// </summary>
/// <remarks>
///     Supports the following types: <see cref="int"/>, <see cref="long"/>, <see cref="double"/>, <see cref="double"/>,
///     <see cref="decimal"/>. Attempting to use any other type will result in an <see cref="InvalidOperationException"/>.
/// </remarks>
public class NumericUpDown<T> : View where T : notnull
{
    private readonly Button _down;

    // TODO: Use a TextField instead of a Label
    private readonly View _number;
    private readonly Button _up;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NumericUpDown{T}"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public NumericUpDown ()
    {
        Type type = typeof (T);

        if (!(type == typeof (object)
              || type == typeof (int)
              || type == typeof (long)
              || type == typeof (double)
              || type == typeof (float)
              || type == typeof (double)
              || type == typeof (decimal)))
        {
            throw new InvalidOperationException ("T must be a numeric type that supports addition and subtraction.");
        }

        // `object` is supported only for AllViewsTester
        if (type != typeof (object))
        {
            Increment = (dynamic)1;
            Value = (dynamic)0;
        }

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        _down = new ()
        {
            Height = 1,
            Width = 1,
            NoPadding = true,
            NoDecorations = true,
            Title = $"{Glyphs.DownArrow}",
            WantContinuousButtonPressed = true,
            CanFocus = false,
            ShadowStyle = ShadowStyle.None,
        };

        _number = new ()
        {
            Text = Value?.ToString () ?? "Err",
            X = Pos.Right (_down),
            Y = Pos.Top (_down),
            Width = Dim.Auto (minimumContentDim: Dim.Func (() => string.Format (Format, Value).Length)),
            Height = 1,
            TextAlignment = Alignment.Center,
            CanFocus = true,
        };

        _up = new ()
        {
            X = Pos.Right (_number),
            Y = Pos.Top (_number),
            Height = 1,
            Width = 1,
            NoPadding = true,
            NoDecorations = true,
            Title = $"{Glyphs.UpArrow}",
            WantContinuousButtonPressed = true,
            CanFocus = false,
            ShadowStyle = ShadowStyle.None,
        };

        CanFocus = true;

        _down.Accepting += OnDownButtonOnAccept;
        _up.Accepting += OnUpButtonOnAccept;

        Add (_down, _number, _up);

        AddCommand (
                    Command.ScrollUp,
                    () =>
                    {
                        if (type == typeof (object))
                        {
                            return false;
                        }

                        if (Value is { } && Increment is { })
                        {
                            Value = (dynamic)Value + (dynamic)Increment;
                        }

                        return true;
                    });

        AddCommand (
                    Command.ScrollDown,
                    () =>
                    {
                        if (type == typeof (object))
                        {
                            return false;
                        }

                        if (Value is { } && Increment is { })
                        {
                            Value = (dynamic)Value - (dynamic)Increment;
                        }


                        return true;
                    });

        KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
        KeyBindings.Add (Key.CursorDown, Command.ScrollDown);

        SetText ();

        return;

        void OnDownButtonOnAccept (object? s, CommandEventArgs e)
        {
            InvokeCommand (Command.ScrollDown);
            e.Cancel = true;
        }

        void OnUpButtonOnAccept (object? s, CommandEventArgs e)
        {
            InvokeCommand (Command.ScrollUp);
            e.Cancel = true;
        }
    }

    private T _value = default!;

    /// <summary>
    ///     Gets or sets the value that will be incremented or decremented.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ValueChanging"/> and <see cref="ValueChanged"/> events are raised when the value changes.
    ///         The <see cref="ValueChanging"/> event can be canceled the change setting
    ///         <see cref="CancelEventArgs{T}"/><c>.Cancel</c> to <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public T Value
    {
        get => _value;
        set
        {
            if (_value.Equals (value))
            {
                return;
            }

            T oldValue = value;
            CancelEventArgs<T> args = new (in _value, ref value);
            ValueChanging?.Invoke (this, args);

            if (args.Cancel)
            {
                return;
            }

            _value = value;
            SetText ();
            ValueChanged?.Invoke (this, new (in value));
        }
    }

    /// <summary>
    ///     Raised when the value is about to change. Set <see cref="CancelEventArgs{T}"/><c>.Cancel</c> to true to prevent the
    ///     change.
    /// </summary>
    public event EventHandler<CancelEventArgs<T>>? ValueChanging;

    /// <summary>
    ///     Raised when the value has changed.
    /// </summary>
    public event EventHandler<EventArgs<T>>? ValueChanged;

    private string _format = "{0}";

    /// <summary>
    ///     Gets or sets the format string used to display the value. The default is "{0}".
    /// </summary>
    public string Format
    {
        get => _format;
        set
        {
            if (_format == value)
            {
                return;
            }

            _format = value;

            FormatChanged?.Invoke (this, new (value));
            SetText ();
        }
    }

    /// <summary>
    ///     Raised when <see cref="Format"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs<string>>? FormatChanged;

    private void SetText ()
    {
        _number.Text = string.Format (Format, _value);
        Text = _number.Text;
    }

    private T? _increment;

    /// <summary>
    /// </summary>
    public T? Increment
    {
        get => _increment;
        set
        {
            if (_increment is { } && value is { } && (dynamic)_increment == (dynamic)value)
            {
                return;
            }

            _increment = value;

            IncrementChanged?.Invoke (this, new (value!));
        }
    }

    /// <summary>
    ///     Raised when <see cref="Increment"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs<T>>? IncrementChanged;
}

/// <summary>
///     Enables the user to increase or decrease an <see langword="int"/> by clicking on the up or down buttons.
/// </summary>
public class NumericUpDown : NumericUpDown<int>
{ }
