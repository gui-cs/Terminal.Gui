namespace Terminal.Gui.Views;

/// <summary>
///     A linear range view that allows selection of a single option from a typed list of options.
/// </summary>
/// <typeparam name="T">The data type of the options.</typeparam>
/// <remarks>
///     <para>
///         Exposes the current selection through <see cref="Value"/> as <typeparamref name="T"/>?,
///         which is <see langword="null"/> when no option is selected.
///     </para>
///     <para>
///         To switch the current selection programmatically, set <see cref="Value"/>. To observe
///         selection changes, subscribe to <see cref="LinearRangeViewBase{TOption,TValue}.ValueChanged"/>.
///     </para>
/// </remarks>
public class LinearSelector<T> : LinearRangeViewBase<T, T>
{
    private T? _value;

    /// <summary>Initializes a new instance of <see cref="LinearSelector{T}"/>.</summary>
    public LinearSelector () : base (LinearRangeRenderMode.Single) { }

    /// <summary>Initializes a new instance of <see cref="LinearSelector{T}"/>.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearSelector (List<T>? options, Orientation orientation = Orientation.Horizontal)
        : base (options, orientation, LinearRangeRenderMode.Single) { }

    /// <inheritdoc/>
    public override T? Value
    {
        get => _value;
        set
        {
            T? current = _value;

            if (EqualityComparer<T?>.Default.Equals (current, value))
            {
                return;
            }

            if (RaiseValueChanging (current, value))
            {
                return;
            }

            _value = value;

            // Sync indices to match value.
            if (value is null)
            {
                ApplySelectedIndices ([]);
            }
            else
            {
                int idx = IndexOfData (value);

                if (idx >= 0)
                {
                    ApplySelectedIndices ([idx]);
                }
                else
                {
                    // Value not present among options: clear selection but keep field.
                    ApplySelectedIndices ([]);
                }
            }

            RaiseValueChanged (current, _value);
        }
    }

    /// <inheritdoc/>
    protected override void OnSelectionChanged ()
    {
        T? previous = _value;
        T? newValue = SelectedIndices.Count > 0 ? Options [SelectedIndices [0]].Data : default;

        if (EqualityComparer<T?>.Default.Equals (previous, newValue))
        {
            return;
        }

        _value = newValue;
        RaiseValueChanged (previous, newValue);
    }
}
