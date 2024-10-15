namespace Terminal.Gui.InputTests;

public class EscSeqReqTests
{
    [Fact]
    public void Add_Tests ()
    {
        var escSeqReq = new EscSeqRequests ();
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.Single (escSeqReq.Statuses);
        Assert.Equal ("t", escSeqReq.Statuses [^1].AnsiRequest.Terminator);
        Assert.Equal (1, escSeqReq.Statuses [^1].NumRequests);
        Assert.Equal (1, escSeqReq.Statuses [^1].NumOutstanding);

        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.Single (escSeqReq.Statuses);
        Assert.Equal ("t", escSeqReq.Statuses [^1].AnsiRequest.Terminator);
        Assert.Equal (2, escSeqReq.Statuses [^1].NumRequests);
        Assert.Equal (2, escSeqReq.Statuses [^1].NumOutstanding);

        escSeqReq = new ();
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.Single (escSeqReq.Statuses);
        Assert.Equal ("t", escSeqReq.Statuses [^1].AnsiRequest.Terminator);
        Assert.Equal (2, escSeqReq.Statuses [^1].NumRequests);
        Assert.Equal (2, escSeqReq.Statuses [^1].NumOutstanding);

        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.Single (escSeqReq.Statuses);
        Assert.Equal ("t", escSeqReq.Statuses [^1].AnsiRequest.Terminator);
        Assert.Equal (3, escSeqReq.Statuses [^1].NumRequests);
        Assert.Equal (3, escSeqReq.Statuses [^1].NumOutstanding);
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        var escSeqReq = new EscSeqRequests ();
        Assert.NotNull (escSeqReq.Statuses);
        Assert.Empty (escSeqReq.Statuses);
    }

    [Fact]
    public void Remove_Tests ()
    {
        var escSeqReq = new EscSeqRequests ();
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        escSeqReq.HasResponse ("t", out EscSeqReqStatus seqReqStatus);
        escSeqReq.Remove (seqReqStatus);
        Assert.Empty (escSeqReq.Statuses);

        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        escSeqReq.HasResponse ("t", out seqReqStatus);
        escSeqReq.Remove (seqReqStatus);
        Assert.Single (escSeqReq.Statuses);
        Assert.Equal ("t", escSeqReq.Statuses [^1].AnsiRequest.Terminator);
        Assert.Equal (2, escSeqReq.Statuses [^1].NumRequests);
        Assert.Equal (1, escSeqReq.Statuses [^1].NumOutstanding);

        escSeqReq.HasResponse ("t", out seqReqStatus);
        escSeqReq.Remove (seqReqStatus);
        Assert.Empty (escSeqReq.Statuses);
    }

    [Fact]
    public void Requested_Tests ()
    {
        var escSeqReq = new EscSeqRequests ();
        Assert.False (escSeqReq.HasResponse ("t", out EscSeqReqStatus seqReqStatus));
        Assert.Null (seqReqStatus);

        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.False (escSeqReq.HasResponse ("r", out seqReqStatus));
        Assert.Null (seqReqStatus);
        Assert.True (escSeqReq.HasResponse ("t", out seqReqStatus));
        Assert.NotNull (seqReqStatus);
    }
}
