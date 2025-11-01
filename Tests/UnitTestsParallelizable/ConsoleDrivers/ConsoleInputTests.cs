using System.Collections.Concurrent;

namespace UnitTests_Parallelizable.ConsoleDriverTests;

public class ConsoleInputTests
{
    private class FakeInput : ConsoleInputImpl<char>
    {
        private readonly string [] _reads;

        public FakeInput (params string [] reads) { _reads = reads; }

        private int iteration;

        /// <inheritdoc/>
        protected override bool Peek () { return iteration < _reads.Length; }

        /// <inheritdoc/>
        protected override IEnumerable<char> Read () { return _reads [iteration++]; }
    }

    [Fact]
    public void Test_ThrowsIfNotInitialized ()
    {
        var input = new FakeInput ("Fish");

        var ex = Assert.Throws<Exception> (() => input.Run (new (true)));
        Assert.Equal ("Cannot run input before Initialization", ex.Message);
    }

    [Fact]
    public void Test_Simple ()
    {
        var input = new FakeInput ("Fish");
        ConcurrentQueue<char> queue = new ();

        input.Initialize (queue);

        var cts = new CancellationTokenSource ();
        cts.CancelAfter (25); // Cancel after 25 milliseconds
        CancellationToken token = cts.Token;
        Assert.Empty (queue);
        input.Run (token);

        Assert.Equal ("Fish", new (queue.ToArray ()));
    }
}
