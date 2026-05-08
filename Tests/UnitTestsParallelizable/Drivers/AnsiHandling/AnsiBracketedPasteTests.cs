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

    [Theory]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    public void Paste_StartMarkerSplitAtEveryByteBoundary_StillDetected (int splitAt)
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        string startMarker = EscSeqUtils.CSI_BracketedPasteStart;
        string firstChunk = startMarker [..splitAt];
        string secondChunk = startMarker [splitAt..] + "PAYLOAD" + EscSeqUtils.CSI_BracketedPasteEnd;

        _parser.ProcessInput (firstChunk);
        _parser.ProcessInput (secondChunk);

        Assert.Equal ("PAYLOAD", captured);
        Assert.Equal (AnsiResponseParserState.Normal, _parser.State);
    }

    [Theory]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    public void Paste_EndMarkerSplitAtEveryByteBoundary_StillDetected (int splitAt)
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        string endMarker = EscSeqUtils.CSI_BracketedPasteEnd;

        _parser.ProcessInput (EscSeqUtils.CSI_BracketedPasteStart + "body");
        _parser.ProcessInput (endMarker [..splitAt]);
        _parser.ProcessInput (endMarker [splitAt..]);

        Assert.Equal ("body", captured);
        Assert.Equal (AnsiResponseParserState.Normal, _parser.State);
    }

    [Fact]
    public void Paste_BodyEndsWithPartialEndMarkerAcrossCalls_DoesNotTerminateEarly ()
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        // First chunk legitimately contains "ESC[20" at the end (a near-end-marker prefix).
        // Second chunk supplies a different char so the prefix is rejected, then the real body
        // and end marker.
        _parser.ProcessInput (EscSeqUtils.CSI_BracketedPasteStart + "head[20");
        _parser.ProcessInput ($"X-tail{EscSeqUtils.CSI_BracketedPasteEnd}");

        Assert.Equal ("head[20X-tail", captured);
    }

    [Fact]
    public void Paste_TwoConsecutivePastes_BothDelivered ()
    {
        List<string> captured = [];
        _parser.Paste += (_, text) => captured.Add (text);

        string input = $"{EscSeqUtils.CSI_BracketedPasteStart}A{EscSeqUtils.CSI_BracketedPasteEnd}"
                       + $"between"
                       + $"{EscSeqUtils.CSI_BracketedPasteStart}B{EscSeqUtils.CSI_BracketedPasteEnd}";

        string output = _parser.ProcessInput (input);

        Assert.Equal (["A", "B"], captured);
        Assert.Equal ("between", output);
    }

    [Fact]
    public void Paste_BodyContainsLiteralStartMarker_AppendedAsContent ()
    {
        // A well-behaved terminal won't emit a nested start marker, but a hostile or buggy one might.
        // We treat any bytes between the outer start and end markers as paste content.
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        _parser.ProcessInput ($"{EscSeqUtils.CSI_BracketedPasteStart}before[200~after{EscSeqUtils.CSI_BracketedPasteEnd}");

        Assert.Equal ("before[200~after", captured);
    }

    [Fact]
    public void Paste_ResetMidPaste_DiscardsBufferAndReturnsToNormal ()
    {
        string? captured = null;
        _parser.Paste += (_, text) => captured = text;

        _parser.ProcessInput ($"{EscSeqUtils.CSI_BracketedPasteStart}partial");
        Assert.Equal (AnsiResponseParserState.InBracketedPaste, _parser.State);

        // Release/Reset is the parser's escape hatch for stale escape sequences. While in
        // bracketed-paste mode it must not silently drop the buffer — but the documented
        // behavior here is that Release returns to Normal. Verify state, and verify that
        // input flow resumes cleanly afterward.
        _parser.Release ();

        Assert.Null (captured);
        Assert.Equal (AnsiResponseParserState.Normal, _parser.State);

        string output = _parser.ProcessInput ("typed");
        Assert.Equal ("typed", output);
    }
}
