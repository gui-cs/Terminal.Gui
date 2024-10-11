using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace UnitTests.ConsoleDrivers;
public class AnsiResponseParserTests (ITestOutputHelper output)
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
        _parser.ExpectResponse ("c",(s)=> response = s);

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
            AssertIgnored (ansiStream,"Hello"[c], ref i);
        }

        // Now we have entered the actual DAR we should be consuming these
        for (int c = 0; c < "\x1B[0".Length; c++)
        {
            AssertConsumed (ansiStream, ref i);
        }

        // Consume the terminator 'c' and expect this to call the above event
        Assert.Null (response);
        AssertConsumed (ansiStream, ref i);
        Assert.NotNull (response);
        Assert.Equal ("\u001b[0c", response);
    }



    [Theory]
    [InlineData ("\x1B[<0;10;20MHello\x1B[0c", "c", "\u001b[0c", "\x1B[<0;10;20MHello")]
    [InlineData ("\x1B[<1;15;25MWorld\x1B[1c", "c", "\u001b[1c", "\x1B[<1;15;25MWorld")]
    // Add more test cases here...
    public void TestInputSequences (string ansiStream, string expectedTerminator, string expectedResponse, string expectedOutput)
    {
        var swGenBatches = Stopwatch.StartNew ();
        int tests = 0;

        var permutations = GetBatchPermutations (ansiStream).ToArray ();

        swGenBatches.Stop ();
        var swRunTest = Stopwatch.StartNew ();

        foreach (var batchSet in permutations)
        {
            string? response = null;

            // Register the expected response with the given terminator
            _parser.ExpectResponse (expectedTerminator, s => response = s);

            // Process the input
            StringBuilder actualOutput = new StringBuilder ();

            foreach (var batch in batchSet)
            {
                actualOutput.Append (_parser.ProcessInput (batch));
            }

            // Assert the final output minus the expected response
            Assert.Equal (expectedOutput, actualOutput.ToString());
            Assert.Equal (expectedResponse, response);
            tests++;
        }

        output.WriteLine ($"Tested {tests} in {swRunTest.ElapsedMilliseconds} ms (gen batches took {swGenBatches.ElapsedMilliseconds} ms)" );
    }

    public static IEnumerable<string []> GetBatchPermutations (string input)
    {
        // Call the recursive method to generate batches
        return GenerateBatches (input, 0);
    }

    private static IEnumerable<string []> GenerateBatches (string input, int start)
    {
        // If we have reached the end of the string, return an empty list
        if (start >= input.Length)
        {
            yield return new string [0];
            yield break;
        }

        // Iterate over the input string to create batches
        for (int i = start + 1; i <= input.Length; i++)
        {
            // Take a batch from 'start' to 'i'
            string batch = input.Substring (start, i - start);

            // Recursively get batches from the remaining substring
            foreach (var remainingBatches in GenerateBatches (input, i))
            {
                // Combine the current batch with the remaining batches
                var result = new string [1 + remainingBatches.Length];
                result [0] = batch;
                Array.Copy (remainingBatches, 0, result, 1, remainingBatches.Length);
                yield return result;
            }
        }
    }


    private void AssertIgnored (string ansiStream,char expected, ref int i)
    {
        var c = NextChar (ansiStream, ref i);

        // Parser does not grab this key (i.e. driver can continue with regular operations)
        Assert.Equal ( c,_parser.ProcessInput (c));
        Assert.Equal (expected,c.Single());
    }
    private void AssertConsumed (string ansiStream, ref int i)
    {
        // Parser grabs this key
        var c = NextChar (ansiStream, ref i);
        Assert.Empty (_parser.ProcessInput(c));
    }
    private void AssertReleased (string ansiStream, ref int i, string expectedRelease)
    {
        var c = NextChar (ansiStream, ref i);

        // Parser realizes it has grabbed content that does not belong to an outstanding request
        // Parser returns false to indicate to continue
        Assert.Equal(expectedRelease,_parser.ProcessInput (c));
    }
    private string NextChar (string ansiStream, ref int i)
    {
        return ansiStream [i++].ToString();
    }
}
