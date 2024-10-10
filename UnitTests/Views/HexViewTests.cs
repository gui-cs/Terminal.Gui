#nullable enable
using System.Text;
using JetBrains.Annotations;

namespace Terminal.Gui.ViewsTests;

public class HexViewTests
{
    [Theory]
    [InlineData (0, 4)]
    [InlineData (9, 4)]
    [InlineData (20, 4)]
    [InlineData (24, 4)]
    [InlineData (30, 4)]
    [InlineData (31, 4)]
    [InlineData (32, 4)]
    [InlineData (33, 4)]
    [InlineData (34, 4)]
    [InlineData (35, 4)]
    [InlineData (36, 4)]
    [InlineData (37, 4)]
    [InlineData (50, 8)]
    [InlineData (51, 8)]
    public void BytesPerLine_Calculates_Correctly (int width, int expectedBpl)
    {
        var hv = new HexView (LoadStream (null, out long _)) { Width = width, Height = 10 };
        hv.LayoutSubviews ();

        Assert.Equal (expectedBpl, hv.BytesPerLine);
    }

    [Theory]
    [InlineData ("01234", 20, 4)]
    [InlineData ("012345", 20, 4)]
    public void xuz (string str, int width, int expectedBpl)
    {
        var hv = new HexView (LoadStream (str, out long _)) { Width = width, Height = 10 };
        hv.LayoutSubviews ();

        Assert.Equal (expectedBpl, hv.BytesPerLine);
    }


    [Fact]
    public void AllowEdits_Edits_ApplyEdits ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 20, Height = 20 };

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubviews ();

        Assert.Empty (hv.Edits);
        hv.AllowEdits = false;
        Assert.True (hv.NewKeyDownEvent (Key.Home));
        Assert.False (hv.NewKeyDownEvent (Key.A));
        Assert.Empty (hv.Edits);
        Assert.Equal (126, hv.Source.Length);

        hv.AllowEdits = true;
        Assert.True (hv.NewKeyDownEvent (Key.D4));
        Assert.True (hv.NewKeyDownEvent (Key.D1));
        Assert.Single (hv.Edits);
        Assert.Equal (65, hv.Edits.ToList () [0].Value);
        Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
        Assert.Equal (126, hv.Source.Length);

        // Appends byte
        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.True (hv.NewKeyDownEvent (Key.D4));
        Assert.True (hv.NewKeyDownEvent (Key.D2));
        Assert.Equal (2, hv.Edits.Count);
        Assert.Equal (66, hv.Edits.ToList () [1].Value);
        Assert.Equal ('B', (char)hv.Edits.ToList () [1].Value);
        Assert.Equal (126, hv.Source.Length);

        hv.ApplyEdits ();
        Assert.Empty (hv.Edits);
        Assert.Equal (127, hv.Source.Length);
    }

    [Fact]
    public void ApplyEdits_With_Argument ()
    {
        byte [] buffer = Encoding.Default.GetBytes ("Fest");
        var original = new MemoryStream ();
        original.Write (buffer, 0, buffer.Length);
        original.Flush ();
        var copy = new MemoryStream ();
        original.Position = 0;
        original.CopyTo (copy);
        copy.Flush ();
        var hv = new HexView (copy) { Width = Dim.Fill (), Height = Dim.Fill () };

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubviews ();

        var readBuffer = new byte [hv.Source.Length];
        hv.Source.Position = 0;
        hv.Source.Read (readBuffer);
        Assert.Equal ("Fest", Encoding.Default.GetString (readBuffer));

        Assert.True (hv.NewKeyDownEvent (Key.D5));
        Assert.True (hv.NewKeyDownEvent (Key.D4));
        readBuffer [hv.Edits.ToList () [0].Key] = hv.Edits.ToList () [0].Value;
        Assert.Equal ("Test", Encoding.Default.GetString (readBuffer));

        hv.ApplyEdits (original);
        original.Position = 0;
        original.Read (buffer);
        copy.Position = 0;
        copy.Read (readBuffer);
        Assert.Equal ("Test", Encoding.Default.GetString (buffer));
        Assert.Equal ("Test", Encoding.Default.GetString (readBuffer));
        Assert.Equal (Encoding.Default.GetString (buffer), Encoding.Default.GetString (readBuffer));
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var hv = new HexView ();
        Assert.NotNull (hv.Source);
        Assert.IsAssignableFrom<MemoryStream> (hv.Source);
        Assert.True (hv.CanFocus);
        Assert.True (hv.AllowEdits);

        hv = new (new MemoryStream ());
        Assert.NotNull (hv.Source);
        Assert.IsAssignableFrom<Stream> (hv.Source);
        Assert.True (hv.CanFocus);
        Assert.True (hv.AllowEdits);
    }

    [Fact]
    public void CursorPosition_Encoding_Default ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 100, Height = 100 };
        Application.Top = new Toplevel ();
        Application.Top.Add (hv);

        Application.Top.LayoutSubviews ();

        Assert.Equal (new (0, 0), hv.CursorPosition);
        Assert.Equal (20, hv.BytesPerLine);

        Assert.True (hv.NewKeyDownEvent (Key.Tab));
        Assert.Equal (new (0, 0), hv.CursorPosition);

        Assert.True (hv.NewKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (hv.CursorPosition.X, hv.BytesPerLine - 1);
        Assert.True (hv.NewKeyDownEvent (Key.Home));

        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (new (1, 0), hv.CursorPosition);

        Assert.True (hv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (new (1, 1), hv.CursorPosition);

        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.Equal (new (2, 2), hv.CursorPosition);
        int col = hv.CursorPosition.X;
        int line = hv.CursorPosition.Y;
        int offset = line * (hv.BytesPerLine - col);
        Assert.Equal (hv.Address, col * line + offset);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    public void CursorPosition_Encoding_Unicode ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = Dim.Fill (), Height = Dim.Fill () };
        Application.Top = new Toplevel ();
        Application.Top.Add (hv);

        hv.LayoutSubviews ();

        Assert.Equal (new (0, 0), hv.CursorPosition);

        Assert.True (hv.NewKeyDownEvent (Key.Tab));
        Assert.True (hv.NewKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (hv.CursorPosition.X, hv.BytesPerLine);
        Assert.True (hv.NewKeyDownEvent (Key.Home));

        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (new (2, 1), hv.CursorPosition);

        Assert.True (hv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (new (2, 2), hv.CursorPosition);

        Assert.True (hv.NewKeyDownEvent (Key.End));
        int col = hv.CursorPosition.X;
        int line = hv.CursorPosition.Y;
        int offset = (line - 1) * (hv.BytesPerLine - col);
        Assert.Equal (hv.Address, col * line + offset);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    public void DiscardEdits_Method ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 20, Height = 20 };

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubviews ();

        Assert.True (hv.NewKeyDownEvent (Key.D4));
        Assert.True (hv.NewKeyDownEvent (Key.D1));
        Assert.Single (hv.Edits);
        Assert.Equal (65, hv.Edits.ToList () [0].Value);
        Assert.Equal ('A', (char)hv.Edits.ToList () [0].Value);
        Assert.Equal (126, hv.Source.Length);

        hv.DiscardEdits ();
        Assert.Empty (hv.Edits);
    }

    [Fact]
    public void DisplayStart_Source ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 20, Height = 20 };

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubviews ();

        Assert.Equal (0, hv.DisplayStart);

        Assert.True (hv.NewKeyDownEvent (Key.PageDown));
        Assert.Equal (4 * hv.Frame.Height, hv.DisplayStart);
        Assert.Equal (hv.Source.Length, hv.Source.Position);

        Assert.True (hv.NewKeyDownEvent (Key.End));

        // already on last page and so the DisplayStart is the same as before
        Assert.Equal (4 * hv.Frame.Height, hv.DisplayStart);
        Assert.Equal (hv.Source.Length, hv.Source.Position);
    }

    [Fact]
    public void Edited_Event ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 20, Height = 20 };

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubviews ();

        KeyValuePair<long, byte> keyValuePair = default;
        hv.Edited += (s, e) => keyValuePair = new (e.Address, e.NewValue);

        Assert.True (hv.NewKeyDownEvent (Key.D4));
        Assert.True (hv.NewKeyDownEvent (Key.D6));

        Assert.Equal (0, (int)keyValuePair.Key);
        Assert.Equal (70, keyValuePair.Value);
        Assert.Equal ('F', (char)keyValuePair.Value);
    }

    [Fact]
    public void Exceptions_Tests ()
    {
        Assert.Throws<ArgumentNullException> (() => new HexView (null));
        Assert.Throws<ArgumentException> (() => new HexView (new NonSeekableStream (new MemoryStream ())));
    }

    [Fact]
    public void KeyBindings_Test_Movement_LeftSide ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 20, Height = 10 };
        Application.Top = new Toplevel ();
        Application.Top.Add (hv);

        hv.LayoutSubviews ();

        Assert.Equal (MEM_STRING_LENGTH, hv.Source.Length);
        Assert.Equal (0, hv.Address);
        Assert.Equal (4, hv.BytesPerLine);

        // right side only needed to press one time
        Assert.True (hv.NewKeyDownEvent (Key.Tab));

        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (1, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (0, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (4, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (0, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.PageDown));
        Assert.Equal (40, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.PageUp));
        Assert.Equal (0, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.Equal (MEM_STRING_LENGTH, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.Home));
        Assert.Equal (0, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (3, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.CursorLeft.WithCtrl));
        Assert.Equal (0, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.CursorDown.WithCtrl));
        Assert.Equal (36, hv.Address);

        Assert.True (hv.NewKeyDownEvent (Key.CursorUp.WithCtrl));
        Assert.Equal (0, hv.Address);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    public void Position_Using_Encoding_Default ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 20, Height = 20 };
        hv.LayoutSubviews ();

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubviews ();
        Assert.Equal (MEM_STRING_LENGTH, hv.Source.Length);
        Assert.Equal (MEM_STRING_LENGTH, hv.Source.Position);
        Assert.Equal (0, hv.Address);

        // left side needed to press twice
        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (MEM_STRING_LENGTH, hv.Source.Position);
        Assert.Equal (1, hv.Address);
        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (MEM_STRING_LENGTH, hv.Source.Position);
        Assert.Equal (2, hv.Address);

        // right side only needed to press one time
        Assert.True (hv.NewKeyDownEvent (Key.Tab));
        Assert.Equal (MEM_STRING_LENGTH, hv.Source.Position);
        Assert.Equal (2, hv.Address);
        Assert.True (hv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (MEM_STRING_LENGTH, hv.Source.Position);
        Assert.Equal (1, hv.Address);

        // last position is equal to the source length
        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.Equal (MEM_STRING_LENGTH, hv.Source.Position);
        Assert.Equal (64, hv.Address);
        Assert.Equal (hv.Address - 1, hv.Source.Length);
    }

    [Fact]
    public void Position_Using_Encoding_Unicode ()
    {
        var hv = new HexView (LoadStream (null, out _, true)) { Width = 20, Height = 20 };

        // Needed because HexView relies on LayoutComplete to calc sizes
        hv.LayoutSubviews ();
        Assert.Equal (126, hv.Source.Length);
        Assert.Equal (126, hv.Source.Position);
        Assert.Equal (1, hv.Address);

        // left side needed to press twice
        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (126, hv.Source.Position);
        Assert.Equal (1, hv.Address);
        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (126, hv.Source.Position);
        Assert.Equal (2, hv.Address);

        // right side only needed to press one time
        Assert.True (hv.NewKeyDownEvent (Key.Tab));
        Assert.Equal (126, hv.Source.Position);
        Assert.Equal (2, hv.Address);
        Assert.True (hv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (126, hv.Source.Position);
        Assert.Equal (1, hv.Address);

        // last position is equal to the source length
        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.Equal (126, hv.Source.Position);
        Assert.Equal (127, hv.Address);
        Assert.Equal (hv.Address - 1, hv.Source.Length);
    }

    [Fact]
    public void PositionChanged_Event ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 20, Height = 10 };
        Application.Top = new Toplevel ();
        Application.Top.Add (hv);

        Application.Top.LayoutSubviews ();

        HexViewEventArgs hexViewEventArgs = null;
        hv.PositionChanged += (s, e) => hexViewEventArgs = e;

        Assert.Equal (12, hv.BytesPerLine);

        Assert.True (hv.NewKeyDownEvent (Key.CursorRight)); // left side must press twice
        Assert.True (hv.NewKeyDownEvent (Key.CursorRight));
        Assert.True (hv.NewKeyDownEvent (Key.CursorDown));

        Assert.Equal (12, hexViewEventArgs.BytesPerLine);
        Assert.Equal (new (2, 2), hexViewEventArgs.CursorPosition);
        Assert.Equal (14, hexViewEventArgs.Address);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    public void Source_Sets_DisplayStart_And_Position_To_Zero_If_Greater_Than_Source_Length ()
    {
        var hv = new HexView (LoadStream (null, out _)) { Width = 10, Height = 5 };
        Application.Top = new Toplevel ();
        Application.Top.Add (hv);

        hv.LayoutSubviews ();

        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.Equal (62, hv.DisplayStart);
        Assert.Equal (64, hv.Address);

        hv.Source = new MemoryStream ();
        Assert.Equal (0, hv.DisplayStart);
        Assert.Equal (0, hv.Address - 1);

        hv.Source = LoadStream (null, out _);
        hv.Width = Dim.Fill ();
        hv.Height = Dim.Fill ();
        Application.Top.LayoutSubviews ();
        Assert.Equal (0, hv.DisplayStart);
        Assert.Equal (0, hv.Address - 1);

        Assert.True (hv.NewKeyDownEvent (Key.End));
        Assert.Equal (0, hv.DisplayStart);
        Assert.Equal (64, hv.Address);

        hv.Source = new MemoryStream ();
        Assert.Equal (0, hv.DisplayStart);
        Assert.Equal (0, hv.Address - 1);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    private const string MEM_STRING = "Hello world.\nThis is a test of the Emergency Broadcast System.\n";
    private const int MEM_STRING_LENGTH = 63;

    private Stream LoadStream (string? memString, out long numBytesInMemString, bool unicode = false)
    {
        var stream = new MemoryStream ();
        byte [] bArray;

        Assert.Equal (MEM_STRING_LENGTH, MEM_STRING.Length);

        if (memString is null)
        {
            memString = MEM_STRING;
        }

        if (unicode)
        {
            bArray = Encoding.Unicode.GetBytes (memString);
        }
        else
        {
            bArray = Encoding.Default.GetBytes (memString);
        }
        numBytesInMemString = bArray.Length;

        stream.Write (bArray);

        return stream;
    }

    private class NonSeekableStream (Stream baseStream) : Stream
    {
        public override bool CanRead => baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => baseStream.CanWrite;
        public override long Length => throw new NotSupportedException ();

        public override long Position
        {
            get => baseStream.Position;
            set => throw new NotSupportedException ();
        }

        public override void Flush () { baseStream.Flush (); }
        public override int Read (byte [] buffer, int offset, int count) { return baseStream.Read (buffer, offset, count); }
        public override long Seek (long offset, SeekOrigin origin) { throw new NotImplementedException (); }
        public override void SetLength (long value) { throw new NotSupportedException (); }
        public override void Write (byte [] buffer, int offset, int count) { baseStream.Write (buffer, offset, count); }
    }
}
