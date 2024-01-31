namespace Terminal.Gui.InputTests {
    public class EscSeqReqTests {
        [Fact]
        public void Constructor_Defaults () {
            var escSeqReq = new EscSeqRequests ();
            Assert.NotNull (escSeqReq.Statuses);
            Assert.Empty (escSeqReq.Statuses);
        }

        [Fact]
        public void Add_Tests () {
            var escSeqReq = new EscSeqRequests ();
            escSeqReq.Add ("t");
            Assert.Single (escSeqReq.Statuses);
            Assert.Equal ("t", escSeqReq.Statuses[^1].Terminator);
            Assert.Equal (1, escSeqReq.Statuses[^1].NumRequests);
            Assert.Equal (1, escSeqReq.Statuses[^1].NumOutstanding);

            escSeqReq.Add ("t", 2);
            Assert.Single (escSeqReq.Statuses);
            Assert.Equal ("t", escSeqReq.Statuses[^1].Terminator);
            Assert.Equal (1, escSeqReq.Statuses[^1].NumRequests);
            Assert.Equal (1, escSeqReq.Statuses[^1].NumOutstanding);

            escSeqReq = new EscSeqRequests ();
            escSeqReq.Add ("t", 2);
            Assert.Single (escSeqReq.Statuses);
            Assert.Equal ("t", escSeqReq.Statuses[^1].Terminator);
            Assert.Equal (2, escSeqReq.Statuses[^1].NumRequests);
            Assert.Equal (2, escSeqReq.Statuses[^1].NumOutstanding);

            escSeqReq.Add ("t", 3);
            Assert.Single (escSeqReq.Statuses);
            Assert.Equal ("t", escSeqReq.Statuses[^1].Terminator);
            Assert.Equal (2, escSeqReq.Statuses[^1].NumRequests);
            Assert.Equal (2, escSeqReq.Statuses[^1].NumOutstanding);
        }

        [Fact]
        public void Remove_Tests () {
            var escSeqReq = new EscSeqRequests ();
            escSeqReq.Add ("t");
            escSeqReq.Remove ("t");
            Assert.Empty (escSeqReq.Statuses);

            escSeqReq.Add ("t", 2);
            escSeqReq.Remove ("t");
            Assert.Single (escSeqReq.Statuses);
            Assert.Equal ("t", escSeqReq.Statuses[^1].Terminator);
            Assert.Equal (2, escSeqReq.Statuses[^1].NumRequests);
            Assert.Equal (1, escSeqReq.Statuses[^1].NumOutstanding);

            escSeqReq.Remove ("t");
            Assert.Empty (escSeqReq.Statuses);
        }

        [Fact]
        public void Requested_Tests () {
            var escSeqReq = new EscSeqRequests ();
            Assert.False (escSeqReq.HasResponse ("t"));

            escSeqReq.Add ("t");
            Assert.False (escSeqReq.HasResponse ("r"));
            Assert.True (escSeqReq.HasResponse ("t"));
        }
    }
}
