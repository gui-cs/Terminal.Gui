namespace DriverTests.AnsiHandling;

// Claude - Opus 4.7
[Collection ("Driver Tests")]
public class AnsiBracketedPasteTests
{
    private readonly AnsiResponseParser _parser = new (new SystemTimeProvider ());

    [Fact]
    public void Paste_FullSequence_RaisesEventWithStrippedMarkers ()
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        string output = _parser.ProcessInput ($"{EscSeqUtils.CSI_BracketedPasteStart}hello world{EscSeqUtils.CSI_BracketedPasteEnd}");

        Assert.Equal ("hello world", captured);
        Assert.Equal (string.Empty, output);
        Assert.Equal (AnsiResponseParserState.Normal, _parser.State);
    }

    [Fact]
    public void Paste_SurroundedByNormalInput_OnlyPastedTextIsDelivered ()
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        string output = _parser.ProcessInput ($"before{EscSeqUtils.CSI_BracketedPasteStart}PASTE{EscSeqUtils.CSI_BracketedPasteEnd}after");

        Assert.Equal ("PASTE", captured);
        Assert.Equal ("beforeafter", output);
    }

    [Fact]
    public void Paste_SplitAcrossProcessInputCalls_AccumulatesAndDelivers ()
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        // Start marker arrives by itself
        string output1 = _parser.ProcessInput (EscSeqUtils.CSI_BracketedPasteStart);
        Assert.Null (captured);
        Assert.Equal (string.Empty, output1);
        Assert.Equal (AnsiResponseParserState.InBracketedPaste, _parser.State);

        // Body arrives in the middle
        string output2 = _parser.ProcessInput ("multi-line\npaste\rbody");
        Assert.Null (captured);
        Assert.Equal (string.Empty, output2);

        // End marker arrives by itself
        string output3 = _parser.ProcessInput (EscSeqUtils.CSI_BracketedPasteEnd);
        Assert.Equal ("multi-line\npaste\rbody", captured);
        Assert.Equal (string.Empty, output3);
        Assert.Equal (AnsiResponseParserState.Normal, _parser.State);
    }

    [Fact]
    public void Paste_ContainingControlChars_PreservesBytesVerbatim ()
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        string payload = "tab\there\nnewline\rcr";

        string output = _parser.ProcessInput ($"{EscSeqUtils.CSI_BracketedPasteStart}{payload}{EscSeqUtils.CSI_BracketedPasteEnd}");

        Assert.Equal (payload, captured);
        Assert.Equal (string.Empty, output);
    }

    [Fact]
    public void Paste_ContainingPartialEndMarkerPrefix_DoesNotTerminateEarly ()
    {
        // Payload contains "ESC[201" without the trailing tilde — must NOT be treated as the end
        // marker. The real end marker arrives later.
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        string trickyPayload = "[201"; // looks like the start of the end marker but missing '~'

        string output = _parser.ProcessInput ($"{EscSeqUtils.CSI_BracketedPasteStart}{trickyPayload}rest{EscSeqUtils.CSI_BracketedPasteEnd}");

        Assert.Equal ("[201rest", captured);
        Assert.Equal (string.Empty, output);
    }

    [Fact]
    public void Paste_EmptyPayload_RaisesEventWithEmptyString ()
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        string output = _parser.ProcessInput ($"{EscSeqUtils.CSI_BracketedPasteStart}{EscSeqUtils.CSI_BracketedPasteEnd}");

        Assert.Equal (string.Empty, captured);
        Assert.Equal (string.Empty, output);
    }

    [Fact]
    public void Paste_OversizedPayload_TruncatesAndReturnsToNormal ()
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        // Build a payload larger than the max paste length so the parser must flush mid-paste.
        var longBody = new string ('x', AnsiResponseParserBase.MaxBracketedPasteLength + 100);

        _parser.ProcessInput ($"{EscSeqUtils.CSI_BracketedPasteStart}{longBody}");

        Assert.NotNull (captured);
        Assert.Equal (AnsiResponseParserBase.MaxBracketedPasteLength, captured!.Length);
        Assert.Equal (AnsiResponseParserState.Normal, _parser.State);
    }

    [Fact]
    public void Paste_WithoutEnableEvent_StillBuffersUntilEndMarker ()
    {
        // No subscriber on Paste — parser still consumes the markers without leaking to output.
        string output = _parser.ProcessInput ($"{EscSeqUtils.CSI_BracketedPasteStart}body{EscSeqUtils.CSI_BracketedPasteEnd}");

        Assert.Equal (string.Empty, output);
        Assert.Equal (AnsiResponseParserState.Normal, _parser.State);
    }
}
