#nullable enable
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a default implementation of <see cref="IListDataSource"/> that renders <see cref="ListView"/> items
///     using <see cref="object.ToString()"/>.
/// </summary>
public class ListWrapper<T> : IListDataSource, IDisposable
{
    /// <summary>
    ///     Creates a new instance of <see cref="ListWrapper{T}"/> that wraps the specified
    ///     <see cref="ObservableCollection{T}"/>.
    /// </summary>
    /// <param name="source"></param>
    public ListWrapper (ObservableCollection<T>? source)
    {
        if (source is { })
        {
            _count = source.Count;
            _marks = new (_count);
            _source = source;
            _source.CollectionChanged += Source_CollectionChanged;
            MaxItemLength = GetMaxLengthItem ();
        }
    }

    private readonly ObservableCollection<T>? _source;
    private int _count;
    private BitArray? _marks;

    private bool _suspendCollectionChangedEvent;

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public int Count => _source?.Count ?? 0;

    /// <inheritdoc/>
    public int MaxItemLength { get; private set; }

    /// <inheritdoc/>
    public bool SuspendCollectionChangedEvent
    {
        get => _suspendCollectionChangedEvent;
        set
        {
            _suspendCollectionChangedEvent = value;

            if (!_suspendCollectionChangedEvent)
            {
                CheckAndResizeMarksIfRequired ();
            }
        }
    }

    /// <inheritdoc/>
    public void Render (
        ListView container,
        bool marked,
        int item,
        int col,
        int line,
        int width,
        int viewportX = 0
    )
    {
        container.Move (Math.Max (col - viewportX, 0), line);

        if (_source is null)
        {
            return;
        }

        object? t = _source [item];

        if (t is null)
        {
            RenderString (container, "", col, line, width);
        }
        else
        {
            if (t is string s)
            {
                RenderString (container, s, col, line, width, viewportX);
            }
            else
            {
                RenderString (container, t.ToString ()!, col, line, width, viewportX);
            }
        }
    }

    /// <inheritdoc/>
    public bool IsMarked (int item)
    {
        if (item >= 0 && item < _count)
        {
            return _marks! [item];
        }

        return false;
    }

    /// <inheritdoc/>
    public void SetMark (int item, bool value)
    {
        if (item >= 0 && item < _count)
        {
            _marks! [item] = value;
        }
    }

    /// <inheritdoc/>
    public IList ToList () { return _source ?? []; }

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_source is { })
        {
            _source.CollectionChanged -= Source_CollectionChanged;
        }
    }

    /// <summary>
    ///     INTERNAL: Searches the underlying collection for the first string element that starts with the specified search value,
    ///     using a case-insensitive comparison.
    /// </summary>
    /// <remarks>
    ///     The comparison is performed in a case-insensitive manner using invariant culture rules. Only
    ///     elements of type string are considered; other types in the collection are ignored.
    /// </remarks>
    /// <param name="search">
    ///     The string value to compare against the start of each string element in the collection. Cannot be
    ///     null.
    /// </param>
    /// <returns>
    ///     The zero-based index of the first matching string element if found; otherwise, -1 if no match is found or the
    ///     collection is empty.
    /// </returns>
    internal int StartsWith (string search)
    {
        if (_source is null || _source?.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < _source!.Count; i++)
        {
            object? t = _source [i];

            if (t is string u)
            {
                if (u.ToUpper ().StartsWith (search.ToUpperInvariant ()))
                {
                    return i;
                }
            }
            else if (t is string s && s.StartsWith (search, StringComparison.InvariantCultureIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void CheckAndResizeMarksIfRequired ()
    {
        if (_source != null && _count != _source.Count && _marks is { })
        {
            _count = _source.Count;
            var newMarks = new BitArray (_count);

            for (var i = 0; i < Math.Min (_marks.Length, newMarks.Length); i++)
            {
                newMarks [i] = _marks [i];
            }

            _marks = newMarks;

            MaxItemLength = GetMaxLengthItem ();
        }
    }

    private int GetMaxLengthItem ()
    {
        if (_source is null || _source?.Count == 0)
        {
            return 0;
        }

        var maxLength = 0;

        for (var i = 0; i < _source!.Count; i++)
        {
            object? t = _source [i];

            if (t is null)
            {
                continue;
            }

            int l;

            l = t is string u ? u.GetColumns () : t.ToString ()!.Length;

            if (l > maxLength)
            {
                maxLength = l;
            }
        }

        return maxLength;
    }

    private static void RenderString (View driver, string str, int col, int line, int width, int viewportX = 0)
    {
        if (string.IsNullOrEmpty (str) || viewportX >= str.GetColumns ())
        {
            // Empty string or viewport beyond string - just fill with spaces
            for (var i = 0; i < width; i++)
            {
                driver.AddRune ((Rune)' ');
            }

            return;
        }

        int runeLength = str.ToRunes ().Length;
        int startIndex = Math.Min (viewportX, Math.Max (0, runeLength - 1));
        string substring = str.Substring (startIndex);
        string u = TextFormatter.ClipAndJustify (substring, width, Alignment.Start);
        driver.AddStr (u);
        width -= u.GetColumns ();

        while (width-- > 0)
        {
            driver.AddRune ((Rune)' ');
        }
    }

    private void Source_CollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!SuspendCollectionChangedEvent)
        {
            CheckAndResizeMarksIfRequired ();
            CollectionChanged?.Invoke (sender, e);
        }
    }
}
