using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Terminal.Gui;
using Xunit.Abstractions;

namespace UnitTests.ConsoleDrivers;
public class AnsiResponseParserTests (ITestOutputHelper output)
{
    AnsiResponseParser<int> _parser1 = new AnsiResponseParser<int> ();
    AnsiResponseParser _parser2 = new AnsiResponseParser ();

    [Fact]
    public void TestInputProcessing ()
    {
        string ansiStream = "\x1B[<0;10;20M" +   // ANSI escape for mouse move at (10, 20)
                            "Hello" +             // User types "Hello"
                            "\x1B[0c";            // Device Attributes response (e.g., terminal identification i.e. DAR)


        string? response1 = null;
        string? response2 = null;

        int i = 0;

        // Imagine that we are expecting a DAR
        _parser1.ExpectResponse ("c",(s)=> response1 = s);
        _parser2.ExpectResponse ("c", (s) => response2 = s);

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
        Assert.Null (response1);
        Assert.Null (response1);
        AssertConsumed (ansiStream, ref i);
        Assert.NotNull (response2);
        Assert.Equal ("\x1B[0c", response2);
        Assert.NotNull (response2);
        Assert.Equal ("\x1B[0c", response2);
    }

    [Theory]
    [InlineData ("\x1B[<0;10;20MHi\x1B[0c", "c", "\x1B[0c", "\x1B[<0;10;20MHi")]
    [InlineData ("\x1B[<1;15;25MYou\x1B[1c", "c", "\x1B[1c", "\x1B[<1;15;25MYou")]
    [InlineData ("\x1B[0cHi\x1B[0c", "c", "\x1B[0c", "Hi\x1B[0c")]
    [InlineData ("\x1B[<0;0;0MHe\x1B[3c", "c", "\x1B[3c", "\x1B[<0;0;0MHe")]
    [InlineData ("\x1B[<0;1;2Da\x1B[0c\x1B[1c", "c", "\x1B[0c", "\x1B[<0;1;2Da\x1B[1c")]
    [InlineData ("\x1B[1;1M\x1B[3cAn", "c", "\x1B[3c", "\x1B[1;1MAn")] 
    [InlineData ("hi\x1B[2c\x1B[<5;5;5m", "c", "\x1B[2c", "hi\x1B[<5;5;5m")]
    [InlineData ("\x1B[3c\x1B[4c\x1B[<0;0;0MIn", "c", "\u001b[3c", "\u001b[4c\u001b[<0;0;0MIn")]
    [InlineData ("\x1B[<1;2;3M\x1B[0c\x1B[<1;2;3M\x1B[2c", "c", "\x1B[0c", "\x1B[<1;2;3M\x1B[<1;2;3M\u001b[2c")]
    [InlineData ("\x1B[<0;1;1MHi\x1B[6c\x1B[2c\x1B[<1;0;0MT", "c", "\x1B[6c", "\x1B[<0;1;1MHi\x1B[2c\x1B[<1;0;0MT")]
    [InlineData ("Te\x1B[<2;2;2M\x1B[7c", "c", "\x1B[7c", "Te\x1B[<2;2;2M")]
    [InlineData ("\x1B[0c\x1B[<0;0;0M\x1B[3c\x1B[0c\x1B[1;0MT", "c", "\x1B[0c", "\x1B[<0;0;0M\x1B[3c\x1B[0c\x1B[1;0MT")]
    [InlineData ("\x1B[0;0M\x1B[<0;0;0M\x1B[3cT\x1B[1c", "c", "\u001b[3c", "\u001b[0;0M\u001b[<0;0;0MT\u001b[1c")]
    [InlineData ("\x1B[3c\x1B[<0;0;0M\x1B[0c\x1B[<1;1;1MIn\x1B[1c", "c", "\u001b[3c", "\u001b[<0;0;0M\u001b[0c\u001b[<1;1;1MIn\u001b[1c")]
    [InlineData ("\x1B[<5;5;5M\x1B[7cEx\x1B[8c", "c", "\x1B[7c", "\u001b[<5;5;5MEx\u001b[8c")]

    // Random characters and mixed inputs
    [InlineData ("\x1B[<1;1;1MJJ\x1B[9c", "c", "\x1B[9c", "\x1B[<1;1;1MJJ")] // Mixed text
    [InlineData ("Be\x1B[0cAf", "c", "\x1B[0c", "BeAf")] // Escape in the middle of the string
    [InlineData ("\x1B[<0;0;0M\x1B[2cNot e", "c", "\x1B[2c", "\x1B[<0;0;0MNot e")] // Unexpected sequence followed by text
    [InlineData ("Just te\x1B[<0;0;0M\x1B[3c\x1B[2c\x1B[4c", "c", "\x1B[3c", "Just te\x1B[<0;0;0M\x1B[2c\x1B[4c")] // Multiple unexpected responses
    [InlineData ("\x1B[1;2;3M\x1B[0c\x1B[2;2M\x1B[0;0;0MTe", "c", "\x1B[0c", "\x1B[1;2;3M\x1B[2;2M\x1B[0;0;0MTe")] // Multiple commands with responses
    [InlineData ("\x1B[<3;3;3Mabc\x1B[4cde", "c", "\x1B[4c", "\x1B[<3;3;3Mabcde")] // Escape sequences mixed with regular text

    // Edge cases
    [InlineData ("\x1B[0c\x1B[0c\x1B[0c", "c", "\x1B[0c", "\x1B[0c\x1B[0c")] // Multiple identical responses
    [InlineData ("", "c", "", "")] // Empty input
    [InlineData ("Normal", "c", "", "Normal")] // No escape sequences
    [InlineData ("\x1B[<0;0;0M", "c", "", "\x1B[<0;0;0M")] // Escape sequence only
    [InlineData ("\x1B[1;2;3M\x1B[0c", "c", "\x1B[0c", "\x1B[1;2;3M")] // Last response consumed

    [InlineData ("Inpu\x1B[0c\x1B[1;0;0M", "c", "\x1B[0c", "Inpu\x1B[1;0;0M")] // Single input followed by escape
    [InlineData ("\x1B[2c\x1B[<5;6;7MDa", "c", "\x1B[2c", "\x1B[<5;6;7MDa")] // Multiple escape sequences followed by text
    [InlineData ("\x1B[0cHi\x1B[1cGo", "c", "\x1B[0c", "Hi\u001b[1cGo")] // Normal text with multiple escape sequences

    [InlineData ("\x1B[<1;1;1MTe", "c", "", "\x1B[<1;1;1MTe")]
    // Add more test cases here...
    public void TestInputSequences (string ansiStream, string expectedTerminator, string expectedResponse, string expectedOutput)
    {

        var swGenBatches = Stopwatch.StartNew ();
        int tests = 0;

        var permutations = GetBatchPermutations (ansiStream,5).ToArray ();

        swGenBatches.Stop ();
        var swRunTest = Stopwatch.StartNew ();

        foreach (var batchSet in permutations)
        {
            string response1 = string.Empty;
            string response2 = string.Empty;

            // Register the expected response with the given terminator
            _parser1.ExpectResponse (expectedTerminator, s => response1 = s);
            _parser2.ExpectResponse (expectedTerminator, s => response2 = s);

            // Process the input
            StringBuilder actualOutput1 = new StringBuilder ();
            StringBuilder actualOutput2 = new StringBuilder ();

            foreach (var batch in batchSet)
            {
                var output1 = _parser1.ProcessInput (StringToBatch (batch));
                actualOutput1.Append (BatchToString (output1));

                var output2 = _parser2.ProcessInput (batch);
                actualOutput2.Append (output2);
            }

            // Assert the final output minus the expected response
            Assert.Equal (expectedOutput, actualOutput1.ToString());
            Assert.Equal (expectedResponse, response1);
            Assert.Equal (expectedOutput, actualOutput2.ToString ());
            Assert.Equal (expectedResponse, response2);
            tests++;
        }

        output.WriteLine ($"Tested {tests} in {swRunTest.ElapsedMilliseconds} ms (gen batches took {swGenBatches.ElapsedMilliseconds} ms)" );
    }

    private Tuple<char, int> [] StringToBatch (string batch)
    {
        return batch.Select ((k, i) => Tuple.Create (k, i)).ToArray ();
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
        var c2 = ansiStream [i];
        var c1 = NextChar (ansiStream, ref i);

        // Parser does not grab this key (i.e. driver can continue with regular operations)
        Assert.Equal ( c1,_parser1.ProcessInput (c1));
        Assert.Equal (expected,c1.Single().Item1);

        Assert.Equal (c2, _parser2.ProcessInput (c2.ToString()).Single());
        Assert.Equal (expected, c2 );
    }
    private void AssertConsumed (string ansiStream, ref int i)
    {
        // Parser grabs this key
        var c2 = ansiStream [i];
        var c1 = NextChar (ansiStream, ref i);

        Assert.Empty (_parser1.ProcessInput(c1));
        Assert.Empty (_parser2.ProcessInput (c2.ToString()));
    }
    private void AssertReleased (string ansiStream, ref int i, string expectedRelease)
    {
        var c2 = ansiStream [i];
        var c1 = NextChar (ansiStream, ref i);

        // Parser realizes it has grabbed content that does not belong to an outstanding request
        // Parser returns false to indicate to continue
        Assert.Equal(expectedRelease,BatchToString(_parser1.ProcessInput (c1)));


        Assert.Equal (expectedRelease, _parser2.ProcessInput (c2.ToString ()));
    }

    private string BatchToString (IEnumerable<Tuple<char, int>> processInput)
    {
        return new string(processInput.Select (a=>a.Item1).ToArray ());
    }

    private Tuple<char,int>[] NextChar (string ansiStream, ref int i)
    {
        return  StringToBatch(ansiStream [i++].ToString());
    }
}
