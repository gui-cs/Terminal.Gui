namespace UnitTests.ConsoleDrivers;
public class AnsiResponseParserTests
{
    AnsiResponseParser _parser = new AnsiResponseParser ();

    [Fact]
    public void TestInputProcessing ()
    {
        string ansiStream = "\x1B[<0;10;20M" +   // ANSI escape for mouse move at (10, 20)
                            "Hello" +             // User types "Hello"
                            "\x1B[0c";            // Device Attributes response (e.g., terminal identification i.e. DAR)


        string? response = null;

        int i = 0;

        // Imagine that we are expecting a DAR
        _parser.ExpectResponse ('c',(s)=> response = s);

        // First char is Escape which we must consume incase what follows is the DAR
        AssertConsumed (ansiStream, ref i); // Esc

        for (int c = 0; c < "[<0;10;20".Length; c++)
        {
            AssertConsumed (ansiStream, ref i);
        }

        // We see the M terminator
        AssertReleased (ansiStream, ref i, "\x1B[<0;10;20M");

        // Regular user typing
        for (int c = 0; c < "Hello".Length; c++)
        {
            AssertIgnored (ansiStream, ref i);
        }

        // Now we have entered the actual DAR we should be consuming these
        for (int c = 0; c < "\x1B [0".Length; c++)
        {
            AssertConsumed (ansiStream, ref i);
        }

        // Consume the terminator 'c' and expect this to call the above event
        Assert.Null (response);
        AssertConsumed (ansiStream, ref i);
        Assert.NotNull (response);
        Assert.Equal ("\u001b[0c", response);
    }

    private void AssertIgnored (string ansiStream, ref int i)
    {
        // Parser does not grab this key (i.e. driver can continue with regular operations)
        Assert.False (_parser.ConsumeInput (NextChar (ansiStream, ref i), out var released));
        Assert.Null (released);
    }
    private void AssertConsumed (string ansiStream, ref int i)
    {
        // Parser grabs this key
        Assert.True (_parser.ConsumeInput( NextChar (ansiStream, ref i), out var released));
        Assert.Null (released);
    }
    private void AssertReleased (string ansiStream, ref int i, string expectedRelease)
    {
        // Parser realizes it has grabbed content that does not belong to an outstanding request
        // Parser returns false to indicate to continue
        Assert.False(_parser.ConsumeInput (NextChar (ansiStream,ref i), out var released));

        // Parser releases all the grabbed content back to the driver
        Assert.Equal ( released,expectedRelease);
    }
    private char NextChar (string ansiStream, ref int i)
    {
        return ansiStream [i++];
    }
}
