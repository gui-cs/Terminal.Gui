#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     .Net MaskedTextProvider Provider for TextValidateField.
///     <para></para>
///     <para>
///         <a
///             href="https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.maskedtextprovider?view=net-5.0">
///             Wrapper around MaskedTextProvider
///         </a>
///     </para>
///     <para>
///         <a
///             href="https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.maskedtextbox.mask?view=net-5.0">
///             Masking elements
///         </a>
///     </para>
/// </summary>
public class NetMaskedTextProvider : ITextValidateProvider
{
    private MaskedTextProvider? _provider;

    /// <summary>Empty Constructor</summary>
    public NetMaskedTextProvider (string mask) { Mask = mask; }

    /// <summary>Mask property</summary>
    public string Mask
    {
        get => _provider!.Mask;
        set
        {
            string current = _provider != null
                                 ? _provider.ToString (false, false)
                                 : string.Empty;
            _provider = new (value == string.Empty ? "&&&&&&" : value);

            if (!string.IsNullOrEmpty (current))
            {
                _provider.Set (current);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<string>>? TextChanged;

    /// <inheritdoc/>
    public string Text
    {
        get => _provider!.ToString ();
        set => _provider!.Set (value);
    }

    /// <inheritdoc/>
    public bool IsValid => _provider!.MaskCompleted;

    /// <inheritdoc/>
    public bool Fixed => true;

    /// <inheritdoc/>
    public string DisplayText => _provider.ToDisplayString ();

    /// <inheritdoc/>
    public int Cursor (int pos)
    {
        if (pos < 0)
        {
            return CursorStart ();
        }

        if (pos > _provider!.Length)
        {
            return CursorEnd ();
        }

        int p = _provider.FindEditPositionFrom (pos, false);

        if (p == -1)
        {
            p = _provider.FindEditPositionFrom (pos, true);
        }

        return p;
    }

    /// <inheritdoc/>
    public int CursorStart ()
    {
        return _provider!.IsEditPosition (0)
                   ? 0
                   : _provider.FindEditPositionFrom (0, true);
    }

    /// <inheritdoc/>
    public int CursorEnd ()
    {
        return _provider!.IsEditPosition (_provider.Length - 1)
                   ? _provider.Length - 1
                   : _provider.FindEditPositionFrom (_provider.Length, false);
    }

    /// <inheritdoc/>
    public int CursorLeft (int pos)
    {
        int c = _provider!.FindEditPositionFrom (pos - 1, false);

        return c == -1 ? pos : c;
    }

    /// <inheritdoc/>
    public int CursorRight (int pos)
    {
        int c = _provider!.FindEditPositionFrom (pos + 1, true);

        return c == -1 ? pos : c;
    }

    /// <inheritdoc/>
    public bool Delete (int pos)
    {
        string oldValue = Text;
        bool result = _provider!.Replace (' ', pos); // .RemoveAt (pos);

        if (result)
        {
            OnTextChanged (new (in oldValue));
        }

        return result;
    }

    /// <inheritdoc/>
    public bool InsertAt (char ch, int pos)
    {
        string oldValue = Text;
        bool result = _provider!.Replace (ch, pos);

        if (result)
        {
            OnTextChanged (new (in oldValue));
        }

        return result;
    }

    /// <inheritdoc/>
    public void OnTextChanged (EventArgs<string> args) { TextChanged?.Invoke (this, args); }
}
