#nullable enable
using System.Text;


/// <summary>
///     Minimal mock <see cref="IConsoleDriver"/> implementation for parallelizable unit tests.
///     This is a lightweight stub that provides just enough driver functionality for tests that need
///     to set driver properties but don't need full Application initialization or I/O simulation.
/// </summary>
/// <remarks>
///     <para>
///         <strong>Purpose:</strong> This mock is specifically designed for tests in the
///         UnitTestsParallelizable project that need to test view behavior in isolation without
///         the overhead of full driver initialization. It's intentionally minimal and thread-safe.
///     </para>
///     <para>
///         <strong>Not a replacement for FakeDriver:</strong> For tests that need full Application
///         initialization, event firing, input simulation, or output verification, use
///         <see cref="FakeDriver"/> in the UnitTests project with <see cref="AutoInitShutdownAttribute"/>.
///     </para>
///     <para>
///         <strong>Thread Safety:</strong> Each test creates its own instance, making it safe for
///         parallel test execution.
///     </para>
/// </remarks>
internal class MockConsoleDriver : IConsoleDriver
{
    public event EventHandler<Attribute>? AttributeSet;

    private IClipboard? _clipboard;
    private Rectangle _screen;
    private Region? _clip;
    private int _col;
    private int _cols;
    private Cell [,]? _contents;
    private int _left;
    private int _row;
    private int _rows;
    private int _top;
    private bool _supportsTrueColor;
    private bool _force16Colors;
    private Attribute _currentAttribute;

    /// <inheritdoc />
    public IClipboard? Clipboard => _clipboard;

    /// <inheritdoc />
    public Rectangle Screen => _screen;

    /// <inheritdoc />
    public Region? Clip
    {
        get => _clip;
        set => _clip = value;
    }

    /// <inheritdoc />
    public int Col => _col;

    /// <inheritdoc />
    public int Cols
    {
        get => _cols;
        set => _cols = value;
    }

    /// <inheritdoc />
    public Cell [,]? Contents
    {
        get => _contents;
        set => _contents = value;
    }

    /// <inheritdoc />
    public int Left
    {
        get => _left;
        set => _left = value;
    }

    /// <inheritdoc />
    public int Row => _row;

    /// <inheritdoc />
    public int Rows
    {
        get => _rows;
        set => _rows = value;
    }

    /// <inheritdoc />
    public int Top
    {
        get => _top;
        set => _top = value;
    }

    /// <inheritdoc />
    public bool SupportsTrueColor => _supportsTrueColor;

    /// <inheritdoc />
    public bool Force16Colors
    {
        get => _force16Colors;
        set => _force16Colors = value;
    }

    /// <inheritdoc />
    public Attribute CurrentAttribute
    {
        get => _currentAttribute;
        set => _currentAttribute = value;
    }

    /// <inheritdoc />
    public string GetVersionInfo () { return string.Empty; }

    /// <inheritdoc />
    public void WriteRaw (string ansi) {  }

    /// <inheritdoc />
    public bool IsRuneSupported (Rune rune) { return true; }

    /// <inheritdoc />
    public bool IsValidLocation (Rune rune, int col, int row) { return true; }

    /// <inheritdoc />
    public void Move (int col, int row)
    {
        _col = col;
        _row = row;
    }

    /// <inheritdoc />
    public void AddRune (Rune rune) {  }

    /// <inheritdoc />
    public void AddRune (char c) {  }

    /// <inheritdoc />
    public void AddStr (string str) {  }

    /// <inheritdoc />
    public void ClearContents () { }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? ClearedContents;

    /// <inheritdoc />
    public void FillRect (Rectangle rect, Rune rune = default) { }

    /// <inheritdoc />
    public void FillRect (Rectangle rect, char c) {  }

    /// <inheritdoc />
    public bool GetCursorVisibility (out CursorVisibility visibility)
    {
        visibility = CursorVisibility.Invisible;
        return false;

    }

    /// <inheritdoc />
    public void Refresh () { }

    /// <inheritdoc />
    public bool SetCursorVisibility (CursorVisibility visibility) { throw new NotImplementedException (); }

    /// <inheritdoc />
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <inheritdoc />
    public void Suspend () {  }

    /// <inheritdoc />
    public void UpdateCursor () {}

    /// <inheritdoc />
    public void Init () { }

    /// <inheritdoc />
    public void End () {  }

    /// <inheritdoc />

    /// <inheritdoc />
    public Attribute SetAttribute (Attribute c)
    {
        Attribute oldAttribute = _currentAttribute;
        _currentAttribute = c;

        AttributeSet?.Invoke (this, c);

        return oldAttribute;
    }

    /// <inheritdoc />
    public Attribute GetAttribute ()
    {
        return _currentAttribute;
    }


    /// <inheritdoc />
    public Attribute MakeColor (in Color foreground, in Color background) { throw new NotImplementedException (); }

    /// <inheritdoc />
    public event EventHandler<MouseEventArgs>? MouseEvent;

    /// <inheritdoc />
    public event EventHandler<Key>? KeyDown;

    /// <inheritdoc />
    public event EventHandler<Key>? KeyUp;

    /// <inheritdoc />
    public void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool ctrl) { throw new NotImplementedException (); }

    /// <inheritdoc />
    public void QueueAnsiRequest (AnsiEscapeSequenceRequest request) { throw new NotImplementedException (); }

    /// <inheritdoc />
    public AnsiRequestScheduler GetRequestScheduler () { throw new NotImplementedException (); }
}
