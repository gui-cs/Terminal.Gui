using System.ComponentModel;
using Terminal.Gui;

/// <summary>
///     Enables the user to increase or decrease a value by clicking on the up or down buttons.
/// </summary>
/// <remarks>
///     Supports the following types: <see cref="int"/>, <see cref="long"/>, <see cref="float"/>, <see cref="double"/>,
///     <see cref="decimal"/>.
///     Supports only one digit of precision.
/// </remarks>
public class NumericUpDown<T> : View
{
    private readonly Button _down;

    // TODO: Use a TextField instead of a Label
    private readonly View _number;
    private readonly Button _up;

    public NumericUpDown ()
    {
        Type type = typeof (T);

        if (!(type == typeof (int) || type == typeof (long) || type == typeof (float) || type == typeof (double) || type == typeof (decimal)))
        {
            // Support object for AllViewsTester
            if (type != typeof (object))
            {
                throw new InvalidOperationException ("T must be a numeric type that supports addition and subtraction.");
            }
        }

        Width = Dim.Auto (DimAutoStyle.Content); //Dim.Function (() => Digits + 2); // button + 3 for number + button
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
            ShadowStyle = ShadowStyle.None
        };

        _number = new ()
        {
            Text = Value?.ToString () ?? "Err",
            X = Pos.Right (_down),
            Y = Pos.Top (_down),
            Width = Dim.Func (() => Digits),
            Height = 1,
            TextAlignment = Alignment.Center,
            CanFocus = true
        };

        _up = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.Top (_number),
            Height = 1,
            Width = 1,
            NoPadding = true,
            NoDecorations = true,
            Title = $"{Glyphs.UpArrow}",
            WantContinuousButtonPressed = true,
            CanFocus = false,
            ShadowStyle = ShadowStyle.None
        };

        CanFocus = true;

        _down.Accept += OnDownButtonOnAccept;
        _up.Accept += OnUpButtonOnAccept;

        Add (_down, _number, _up);

        AddCommand (
                    Command.ScrollUp,
                    () =>
                    {
                        if (type == typeof (object))
                        {
                            return false;
                        }

                        Value = (dynamic)Value + 1;
                        _number.Text = Value?.ToString () ?? string.Empty;

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

                        Value = (dynamic)Value - 1;
                        _number.Text = Value.ToString () ?? string.Empty;

                        return true;
                    });

        KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
        KeyBindings.Add (Key.CursorDown, Command.ScrollDown);

        return;

        void OnDownButtonOnAccept (object s, HandledEventArgs e) { InvokeCommand (Command.ScrollDown); }

        void OnUpButtonOnAccept (object s, HandledEventArgs e) { InvokeCommand (Command.ScrollUp); }
    }

    private void _up_Enter (object sender, FocusEventArgs e) { throw new NotImplementedException (); }

    private T _value;

    /// <summary>
    ///     The value that will be incremented or decremented.
    /// </summary>
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
            CancelEventArgs<T> args = new (ref _value, ref value);
            ValueChanging?.Invoke (this, args);

            if (args.Cancel)
            {
                return;
            }

            _value = value;
            _number.Text = _value.ToString ();
            ValueChanged?.Invoke (this, new (ref _value));
        }
    }

    /// <summary>
    ///     Fired when the value is about to change. Set <see cref="CancelEventArgs{T}.Cancel"/> to true to prevent the change.
    /// </summary>
    [CanBeNull]
    public event EventHandler<CancelEventArgs<T>> ValueChanging;

    /// <summary>
    ///     Fired when the value has changed.
    /// </summary>
    [CanBeNull]
    public event EventHandler<EventArgs<T>> ValueChanged;

    /// <summary>
    ///     The number of digits to display. The <see cref="View.Viewport"/> will be resized to fit this number of characters
    ///     plus the buttons. The default is 3.
    /// </summary>
    public int Digits { get; set; } = 3;
}


/// <summary>
///     Enables the user to increase or decrease an <see langword="int"/> by clicking on the up or down buttons.
/// </summary>
public class NumericUpDown : NumericUpDown<int>
{

}
