namespace Terminal.Gui.InputTests;

public class EscSeqRequestsTests
{
    [Fact]
    public void Add_Tests ()
    {
        EscSeqRequests.Add ("t");
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Add ("t", 2);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (3, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (3, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Clear ();
        EscSeqRequests.Add ("t", 2);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Add ("t", 3);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (5, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (5, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Clear ();
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        Assert.NotNull (EscSeqRequests.Statuses);
        Assert.Empty (EscSeqRequests.Statuses);
    }

    [Fact]
    public void Remove_Tests ()
    {
        EscSeqRequests.Add ("t");
        EscSeqRequests.Remove ("t");
        Assert.Empty (EscSeqRequests.Statuses);

        EscSeqRequests.Add ("t", 2);
        EscSeqRequests.Remove ("t");
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Remove ("t");
        Assert.Empty (EscSeqRequests.Statuses);

        EscSeqRequests.Clear ();
    }

    [Fact]
    public void HasResponse_Tests ()
    {
        Assert.False (EscSeqRequests.HasResponse ("t"));

        EscSeqRequests.Add ("t");
        Assert.False (EscSeqRequests.HasResponse ("r"));
        Assert.True (EscSeqRequests.HasResponse ("t"));
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Remove ("t");
        Assert.Empty (EscSeqRequests.Statuses);
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    public void Add_Null_Or_Empty_Terminator_Throws (string terminator)
    {
        if (terminator is null)
        {
            Assert.Throws<ArgumentNullException> (() => EscSeqRequests.Add (terminator));
        }
        else
        {
            Assert.Throws<ArgumentException> (() => EscSeqRequests.Add (terminator));
        }
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    public void HasResponse_Null_Or_Empty_Terminator_Does_Not_Throws (string terminator)
    {
        EscSeqRequests.Add ("t");

        Assert.False (EscSeqRequests.HasResponse (terminator));

        EscSeqRequests.Clear ();
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    public void Remove_Null_Or_Empty_Terminator_Throws (string terminator)
    {
        EscSeqRequests.Add ("t");

        if (terminator is null)
        {
            Assert.Throws<ArgumentNullException> (() => EscSeqRequests.Remove (terminator));
        }
        else
        {
            Assert.Throws<ArgumentException> (() => EscSeqRequests.Remove (terminator));
        }

        EscSeqRequests.Clear ();
    }

    [Fact]
    public void Requests_Responses_Tests ()
    {
        // This is simulated response from a CSI_ReportTerminalSizeInChars
        ConsoleKeyInfo [] cki =
        [
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('8', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('1', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new ('0', 0, false, false, false),
            new ('t', 0, false, false, false)
        ];
        ConsoleKeyInfo newConsoleKeyInfo = default;
        ConsoleKey key = default;
        ConsoleModifiers mod = default;

        Assert.Empty (EscSeqRequests.Statuses);

        EscSeqRequests.Add ("t");
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqUtils.DecodeEscSeq (
                                  ref newConsoleKeyInfo,
                                  ref key,
                                  cki,
                                  ref mod,
                                  out string c1Control,
                                  out string code,
                                  out string [] values,
                                  out string terminating,
                                  out bool isKeyMouse,
                                  out List<MouseFlags> mouseFlags,
                                  out Point pos,
                                  out bool isResponse,
                                  null
                                 );

        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (default, newConsoleKeyInfo);
        Assert.Equal (default, key);
        Assert.Equal (10, cki.Length);
        Assert.Equal (default, mod);
        Assert.Equal ("CSI", c1Control);
        Assert.Null (code);
        // ReSharper disable once HeuristicUnreachableCode
        Assert.Equal (3, values.Length);
        Assert.Equal ("8", values [0]);
        Assert.Equal ("t", terminating);
        Assert.False (isKeyMouse);
        Assert.Single (mouseFlags);
        Assert.Equal (default, mouseFlags [^1]);
        Assert.Equal (Point.Empty, pos);
        Assert.True (isResponse);
    }
}
