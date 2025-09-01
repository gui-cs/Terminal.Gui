#nullable enable
using System.ComponentModel;
using System.Numerics;

namespace Terminal.Gui.Views;

/// <summary>
///     Enables the user to increase or decrease a value with the mouse or keyboard in type-safe way.
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

        if (!(type == typeof (object) || NumericHelper.SupportsType (type)))
        {
            throw new InvalidOperationException ("T must be a numeric type that supports addition and subtraction.");
        }

        // `object` is supported only for AllViewsTester
        if (type != typeof (object))
        {
            if (NumericHelper.TryGetHelper (typeof (T), out INumericHelper? helper))
            {
                Increment = (T)helper!.One;
                Value = (T)helper!.Zero;
            }
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
            Width = Dim.Auto (minimumContentDim: Dim.Func (_ => string.Format (Format, Value).GetColumns())),
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
                    Command.Up,
                    (ctx) =>
                    {
                        if (type == typeof (object))
                        {
                            return false;
                        }

                        // BUGBUG: If this is uncommented, the numericupdown in a shortcut will not work
                        //if (RaiseSelecting (ctx) is true)
                        //{
                        //    return true;
                        //}

                        if (Value is { } v && Increment is { } i && NumericHelper.TryGetHelper (typeof (T), out INumericHelper? helper))
                        {
                            Value = (T)helper!.Add (v, i);
                        }
                        return true;
                    });

        AddCommand (
                    Command.Down,
                    (ctx) =>
                    {
                        if (type == typeof (object))
                        {
                            return false;
                        }

                        // BUGBUG: If this is uncommented, the numericupdown in a shortcut will not work
                        //if (RaiseSelecting (ctx) is true)
                        //{
                        //    return true;
                        //}

                        if (Value is { } v && Increment is { } i && NumericHelper.TryGetHelper (typeof (T), out INumericHelper? helper))
                        {
                            Value = (T)helper!.Subtract (v, i);
                        }
                        return true;
                    });

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);

        SetText ();

        return;

        void OnDownButtonOnAccept (object? s, CommandEventArgs e)
        {
            InvokeCommand (Command.Down);
            e.Handled = true;
        }

        void OnUpButtonOnAccept (object? s, CommandEventArgs e)
        {
            InvokeCommand (Command.Up);
            e.Handled = true;
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
    ///         <see cref="HandledEventArgs"/><c>.Handled</c> to <see langword="true"/>.
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
            if (_increment is { } oldVal && value is { } newVal && oldVal.Equals (newVal))
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

    // Prevent the drawing of Text
    /// <inheritdoc />
    protected override bool OnDrawingText () { return true; }

    /// <summary>
    ///     Attempts to convert the specified <paramref name="value"/> to type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to which the value should be converted.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">
    ///     When this method returns, contains the converted value if the conversion succeeded,
    ///     or the default value of <typeparamref name="T"/> if the conversion failed.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the conversion was successful; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryConvert<T> (object value, out T? result)
    {
        try
        {
            result = (T)Convert.ChangeType (value, typeof (T));

            return true;
        }
        catch
        {
            result = default (T);

            return false;
        }
    }
}

/// <summary>
///     Enables the user to increase or decrease an <see langword="int"/> by clicking on the up or down buttons.
/// </summary>
public class NumericUpDown : NumericUpDown<int>
{ }

internal interface INumericHelper
{
    object One { get; }
    object Zero { get; }
    object Add (object a, object b);
    object Subtract (object a, object b);
}

internal static class NumericHelper
{
    private static readonly Dictionary<Type, INumericHelper> _helpers = new ();

    static NumericHelper ()
    {
        // Register known INumber<T> types
        Register<int> ();
        Register<long> ();
        Register<float> ();
        Register<double> ();
        Register<decimal> ();
        // Add more as needed
    }

    private static void Register<T> () where T : INumber<T>
    {
        _helpers [typeof (T)] = new NumericHelperImpl<T> ();
    }

    public static bool TryGetHelper (Type t, out INumericHelper? helper)
        => _helpers.TryGetValue (t, out helper);

    private class NumericHelperImpl<T> : INumericHelper where T : INumber<T>
    {
        public object One => T.One;
        public object Zero => T.Zero;
        public object Add (object a, object b) => (T)a + (T)b;
        public object Subtract (object a, object b) => (T)a - (T)b;
    }

    public static bool SupportsType (Type type) => _helpers.ContainsKey (type);
}
