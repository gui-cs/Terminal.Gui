using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.ConsoleDrivers.V2;
public class ConsoleInputTests
{
    class FakeInput : ConsoleInput<char>
    {
        private readonly string [] _reads;

        public FakeInput (params string [] reads ) { _reads = reads; }

        int iteration = 0;
        /// <inheritdoc />
        protected override bool Peek ()
        {
            return iteration < _reads.Length;
        }

        /// <inheritdoc />
        protected override IEnumerable<char> Read ()
        {
            return _reads [iteration++];
        }
    }

    [Fact]
    public void Test_ThrowsIfNotInitialized ()
    {
        var input = new FakeInput ("Fish");

        var ex = Assert.Throws<Exception>(()=>input.Run (new (canceled: true)));
        Assert.Equal ("Cannot run input before Initialization", ex.Message);
    }


    [Fact]
    public void Test_Simple ()
    {
        var input = new FakeInput ("Fish");
        var queue = new ConcurrentQueue<char> ();

        input.Initialize (queue);

        var cts = new CancellationTokenSource ();
        cts.CancelAfter (25); // Cancel after 25 milliseconds
        CancellationToken token = cts.Token;
        Assert.Empty (queue);
        input.Run (token);

        Assert.Equal ("Fish",new string (queue.ToArray ()));
    }
}
