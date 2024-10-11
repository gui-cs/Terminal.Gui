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
        Assert.Equal ("\x1B[0c", response);
    }

    [Theory]
    [InlineData ("\x1B[<0;10;20MHi\x1B[0c", "c", "\x1B[0c", "\x1B[<0;10;20MHi")]
    [InlineData ("\x1B[<1;15;25MYou\x1B[1c", "c", "\x1B[1c", "\x1B[<1;15;25MYou")]
    [InlineData ("\x1B[0cHi\x1B[0c", "c", "\x1B[0c", "Hi\x1B[0c")] // Consume the first response but pass through the second

    [InlineData ("\x1B[<0;0;0MHe\x1B[3c", "c", "\x1B[3c", "\x1B[<0;0;0MHe")] // Short input
    [InlineData ("\x1B[<0;1;2Da\x1B[0c\x1B[1c", "c", "\x1B[0c", "\x1B[<0;1;2Da\x1B[1c")] // Two responses, consume only the first
    [InlineData ("\x1B[1;1M\x1B[3cAn", "c", "\x1B[3c", "\x1B[1;1MAn")] // Response with a preceding escape sequence
    [InlineData ("hi\x1B[2c\x1B[<5;5;5m", "c", "\x1B[2c", "hi\x1B[<5;5;5m")] // Mixed normal and escape sequences
    [InlineData ("\x1B[3c\x1B[4c\x1B[<0;0;0MIn", "c", "\x1B[0c", "\x1B[3c\x1B[4c\x1B[<0;0;0MIn")] // Multiple consecutive responses
    [InlineData ("\x1B[<1;2;3M\x1B[0c\x1B[<1;2;3M\x1B[2c", "c", "\x1B[2c", "\x1B[<1;2;3M\x1B[0c\x1B[<1;2;3M")] // Interleaved responses
    [InlineData ("\x1B[<0;1;1MHi\x1B[6c\x1B[2c\x1B[<1;0;0MT", "c", "\x1B[6c", "\x1B[<0;1;1MHi\x1B[2c\x1B[<1;0;0MT")] // Mixed input with multiple responses
    [InlineData ("Te\x1B[<2;2;2M\x1B[7c", "c", "\x1B[7c", "Te\x1B[<2;2;2M")] // Text followed by escape sequence
    [InlineData ("\x1B[0c\x1B[<0;0;0M\x1B[3c\x1B[0c\x1B[1;0MT", "c", "\x1B[1;0M", "\x1B[<0;0;0M\x1B[3c\x1B[0cT")] // Multiple escape sequences, with expected response in between
    [InlineData ("\x1B[0;0M\x1B[<0;0;0M\x1B[3cT\x1B[1c", "c", "\x1B[1c", "\x1B[0;0M\x1B[<0;0;0MT")] // Edge case with leading escape
    [InlineData ("\x1B[3c\x1B[<0;0;0M\x1B[0c\x1B[<1;1;1MIn\x1B[1c", "c", "\x1B[1c", "\x1B[3c\x1B[<0;0;0M\x1B[0c\x1B[<1;1;1MIn")] // Multiple unexpected escape sequences
    [InlineData ("\x1B[<5;5;5M\x1B[7cEx\x1B[8c", "c", "\x1B[8c", "\x1B[<5;5;5MEx")] // Extra sequences with no expected responses

    // Random characters and mixed inputs
    [InlineData ("\x1B[<1;1;1MJJ\x1B[9c", "c", "\x1B[9c", "\x1B[<1;1;1MJJ")] // Mixed text
    [InlineData ("Be\x1B[0cAf", "c", "\x1B[0c", "BeAf")] // Escape in the middle of the string
    [InlineData ("\x1B[<0;0;0M\x1B[2cNot e", "c", "\x1B[2c", "\x1B[<0;0;0MNot e")] // Unexpected sequence followed by text
    [InlineData ("Just te\x1B[<0;0;0M\x1B[3c\x1B[2c\x1B[4c", "c", "\x1B[4c", "Just te\x1B[<0;0;0M\x1B[3c\x1B[2c")] // Multiple unexpected responses
    [InlineData ("\x1B[1;2;3M\x1B[0c\x1B[2;2M\x1B[0;0;0MTe", "c", "\x1B[0c", "\x1B[1;2;3M\x1B[2;2MTe")] // Multiple commands with responses
    [InlineData ("\x1B[<3;3;3Mabc\x1B[4cde", "c", "\x1B[4c", "\x1B[<3;3;3Mabcde")] // Escape sequences mixed with regular text

    // Edge cases
    [InlineData ("\x1B[0c\x1B[0c\x1B[0c", "c", "\x1B[0c", "\x1B[0c\x1B[0c")] // Multiple identical responses
    [InlineData ("", "c", "", "")] // Empty input
    [InlineData ("Normal", "c", "", "Normal")] // No escape sequences
    [InlineData ("\x1B[<0;0;0M", "c", "", "\x1B[<0;0;0M")] // Escape sequence only
    [InlineData ("\x1B[1;2;3M\x1B[0c", "c", "\x1B[0c", "\x1B[1;2;3M")] // Last response consumed

    [InlineData ("Inpu\x1B[0c\x1B[1;0;0M", "c", "\x1B[0c", "Inpu\x1B[1;0;0M")] // Single input followed by escape
    [InlineData ("\x1B[2c\x1B[<5;6;7MDa", "c", "\x1B[2c", "\x1B[<5;6;7MDa")] // Multiple escape sequences followed by text
    [InlineData ("\x1B[0cHi\x1B[1cGo", "c", "\x1B[1c", "HiGo")] // Normal text with multiple escape sequences

    [InlineData ("\x1B[<1;1;1MTe", "c", "\x1B[1c", "\x1B[<1;1;1MTe")] // Edge case of maximum length
    // Add more test cases here...
    public void TestInputSequences (string ansiStream, string expectedTerminator, string expectedResponse, string expectedOutput)
    {

        var swGenBatches = Stopwatch.StartNew ();
        int tests = 0;

        var permutations = GetBatchPermutations (ansiStream,7).ToArray ();

        swGenBatches.Stop ();
        var swRunTest = Stopwatch.StartNew ();

        foreach (var batchSet in permutations)
        {
            string response = string.Empty;

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

    public static IEnumerable<string []> GetBatchPermutations (string input, int maxDepth = 3)
    {
        // Call the recursive method to generate batches with an initial depth of 0
        return GenerateBatches (input, 0, maxDepth, 0);
    }

    private static IEnumerable<string []> GenerateBatches (string input, int start, int maxDepth, int currentDepth)
    {
        // If we have reached the maximum recursion depth, return no results
        if (currentDepth >= maxDepth)
        {
            yield break; // No more batches can be generated at this depth
        }

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

            // Recursively get batches from the remaining substring, increasing the depth
            foreach (var remainingBatches in GenerateBatches (input, i, maxDepth, currentDepth + 1))
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
