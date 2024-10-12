using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace UnitTests.ConsoleDrivers;
public class AnsiResponseParserTests (ITestOutputHelper output)
{
    AnsiResponseParser<int> _parser1 = new AnsiResponseParser<int> ();
    AnsiResponseParser _parser2 = new AnsiResponseParser ();

    /// <summary>
    /// Used for the T value in batches that are passed to the  AnsiResponseParser&lt;int&gt;  (parser1)
    /// </summary>
    private int tIndex = 0;

    [Fact]
    public void TestInputProcessing ()
    {
        string ansiStream = "\u001b[<0;10;20M" +   // ANSI escape for mouse move at (10, 20)
                            "Hello" +             // User types "Hello"
                            "\u001b[0c";            // Device Attributes response (e.g., terminal identification i.e. DAR)


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
        AssertReleased (ansiStream, ref i, "\u001b[<0;10;20M");

        // Regular user typing
        for (int c = 0; c < "Hello".Length; c++)
        {
            AssertIgnored (ansiStream,"Hello"[c], ref i);
        }

        // Now we have entered the actual DAR we should be consuming these
        for (int c = 0; c < "\u001b[0".Length; c++)
        {
            AssertConsumed (ansiStream, ref i);
        }

        // Consume the terminator 'c' and expect this to call the above event
        Assert.Null (response1);
        Assert.Null (response1);
        AssertConsumed (ansiStream, ref i);
        Assert.NotNull (response2);
        Assert.Equal ("\u001b[0c", response2);
        Assert.NotNull (response2);
        Assert.Equal ("\u001b[0c", response2);
    }

    [Theory]
    [InlineData ("\u001b[<0;10;20MHi\u001b[0c", "c", "\u001b[0c", "\u001b[<0;10;20MHi")]
    [InlineData ("\u001b[<1;15;25MYou\u001b[1c", "c", "\u001b[1c", "\u001b[<1;15;25MYou")]
    [InlineData ("\u001b[0cHi\u001b[0c", "c", "\u001b[0c", "Hi\u001b[0c")]
    [InlineData ("\u001b[<0;0;0MHe\u001b[3c", "c", "\u001b[3c", "\u001b[<0;0;0MHe")]
    [InlineData ("\u001b[<0;1;2Da\u001b[0c\u001b[1c", "c", "\u001b[0c", "\u001b[<0;1;2Da\u001b[1c")]
    [InlineData ("\u001b[1;1M\u001b[3cAn", "c", "\u001b[3c", "\u001b[1;1MAn")] 
    [InlineData ("hi\u001b[2c\u001b[<5;5;5m", "c", "\u001b[2c", "hi\u001b[<5;5;5m")]
    [InlineData ("\u001b[3c\u001b[4c\u001b[<0;0;0MIn", "c", "\u001b[3c", "\u001b[4c\u001b[<0;0;0MIn")]
    [InlineData ("\u001b[<1;2;3M\u001b[0c\u001b[<1;2;3M\u001b[2c", "c", "\u001b[0c", "\u001b[<1;2;3M\u001b[<1;2;3M\u001b[2c")]
    [InlineData ("\u001b[<0;1;1MHi\u001b[6c\u001b[2c\u001b[<1;0;0MT", "c", "\u001b[6c", "\u001b[<0;1;1MHi\u001b[2c\u001b[<1;0;0MT")]
    [InlineData ("Te\u001b[<2;2;2M\u001b[7c", "c", "\u001b[7c", "Te\u001b[<2;2;2M")]
    [InlineData ("\u001b[0c\u001b[<0;0;0M\u001b[3c\u001b[0c\u001b[1;0MT", "c", "\u001b[0c", "\u001b[<0;0;0M\u001b[3c\u001b[0c\u001b[1;0MT")]
    [InlineData ("\u001b[0;0M\u001b[<0;0;0M\u001b[3cT\u001b[1c", "c", "\u001b[3c", "\u001b[0;0M\u001b[<0;0;0MT\u001b[1c")]
    [InlineData ("\u001b[3c\u001b[<0;0;0M\u001b[0c\u001b[<1;1;1MIn\u001b[1c", "c", "\u001b[3c", "\u001b[<0;0;0M\u001b[0c\u001b[<1;1;1MIn\u001b[1c")]
    [InlineData ("\u001b[<5;5;5M\u001b[7cEx\u001b[8c", "c", "\u001b[7c", "\u001b[<5;5;5MEx\u001b[8c")]

    // Random characters and mixed inputs
    [InlineData ("\u001b[<1;1;1MJJ\u001b[9c", "c", "\u001b[9c", "\u001b[<1;1;1MJJ")] // Mixed text
    [InlineData ("Be\u001b[0cAf", "c", "\u001b[0c", "BeAf")] // Escape in the middle of the string
    [InlineData ("\u001b[<0;0;0M\u001b[2cNot e", "c", "\u001b[2c", "\u001b[<0;0;0MNot e")] // Unexpected sequence followed by text
    [InlineData ("Just te\u001b[<0;0;0M\u001b[3c\u001b[2c\u001b[4c", "c", "\u001b[3c", "Just te\u001b[<0;0;0M\u001b[2c\u001b[4c")] // Multiple unexpected responses
    [InlineData ("\u001b[1;2;3M\u001b[0c\u001b[2;2M\u001b[0;0;0MTe", "c", "\u001b[0c", "\u001b[1;2;3M\u001b[2;2M\u001b[0;0;0MTe")] // Multiple commands with responses
    [InlineData ("\u001b[<3;3;3Mabc\u001b[4cde", "c", "\u001b[4c", "\u001b[<3;3;3Mabcde")] // Escape sequences mixed with regular text

    // Edge cases
    [InlineData ("\u001b[0c\u001b[0c\u001b[0c", "c", "\u001b[0c", "\u001b[0c\u001b[0c")] // Multiple identical responses
    [InlineData ("", "c", "", "")] // Empty input
    [InlineData ("Normal", "c", "", "Normal")] // No escape sequences
    [InlineData ("\u001b[<0;0;0M", "c", "", "\u001b[<0;0;0M")] // Escape sequence only
    [InlineData ("\u001b[1;2;3M\u001b[0c", "c", "\u001b[0c", "\u001b[1;2;3M")] // Last response consumed

    [InlineData ("Inpu\u001b[0c\u001b[1;0;0M", "c", "\u001b[0c", "Inpu\u001b[1;0;0M")] // Single input followed by escape
    [InlineData ("\u001b[2c\u001b[<5;6;7MDa", "c", "\u001b[2c", "\u001b[<5;6;7MDa")] // Multiple escape sequences followed by text
    [InlineData ("\u001b[0cHi\u001b[1cGo", "c", "\u001b[0c", "Hi\u001b[1cGo")] // Normal text with multiple escape sequences

    [InlineData ("\u001b[<1;1;1MTe", "c", "", "\u001b[<1;1;1MTe")]
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
            tIndex = 0;
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

    [Fact]
    public void ReleasesEscapeAfterTimeout ()
    {
        string input = "\u001b";
        int i = 0;

        // Esc on its own looks like it might be an esc sequence so should be consumed
        AssertConsumed (input,ref i);

        // We should know when the state changed
        Assert.Equal (ParserState.ExpectingBracket, _parser1.State);
        Assert.Equal (ParserState.ExpectingBracket, _parser2.State);

        Assert.Equal (DateTime.Now.Date, _parser1.StateChangedAt.Date);
        Assert.Equal (DateTime.Now.Date, _parser2.StateChangedAt.Date);

        AssertManualReleaseIs (input);
    }


    [Fact]
    public void TwoExcapesInARow ()
    {
        // Example user presses Esc key then a DAR comes in
        string input = "\u001b\u001b";
        int i = 0;

        // First Esc gets grabbed
        AssertConsumed (input, ref i);

        // Upon getting the second Esc we should release the first
        AssertReleased (input, ref i, "\u001b",0);

        // Assume 50ms or something has passed, lets force release as no new content

        // It should be the second escape that gets released (i.e. index 1)
        AssertManualReleaseIs ("\u001b",1);
    }

    [Fact]
    public void TwoExcapesInARowWithTextBetween ()
    {
        // Example user presses Esc key and types at the speed of light (normally the consumer should be handling Esc timeout)
        // then a DAR comes in.
        string input = "\u001bfish\u001b";
        int i = 0;

        // First Esc gets grabbed
        AssertConsumed (input, ref i); // Esc
        Assert.Equal (ParserState.ExpectingBracket,_parser1.State);
        Assert.Equal (ParserState.ExpectingBracket, _parser2.State);

        // Because next char is 'f' we do not see a bracket so release both
        AssertReleased (input, ref i, "\u001bf", 0,1); // f

        Assert.Equal (ParserState.Normal, _parser1.State);
        Assert.Equal (ParserState.Normal, _parser2.State);

        AssertReleased (input, ref i,"i",2);
        AssertReleased (input, ref i, "s", 3);
        AssertReleased (input, ref i, "h", 4);

        AssertConsumed (input, ref i); // Second Esc

        // Assume 50ms or something has passed, lets force release as no new content
        AssertManualReleaseIs ("\u001b", 5);
    }

    private Tuple<char, int> [] StringToBatch (string batch)
    {
        return batch.Select ((k) => Tuple.Create (k, tIndex++)).ToArray ();
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

    private void AssertReleased (string ansiStream, ref int i, string expectedRelease, params int[] expectedTValues)
    {
        var c2 = ansiStream [i];
        var c1 = NextChar (ansiStream, ref i);

        // Parser realizes it has grabbed content that does not belong to an outstanding request
        // Parser returns false to indicate to continue
        var released1 = _parser1.ProcessInput (c1).ToArray ();
        Assert.Equal (expectedRelease, BatchToString (released1));

        if (expectedTValues.Length > 0)
        {
            Assert.True (expectedTValues.SequenceEqual (released1.Select (kv=>kv.Item2)));
        }

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
    private void AssertManualReleaseIs (string expectedRelease, params int [] expectedTValues)
    {

        // Consumer is responsible for determining this based on  e.g. after 50ms
        var released1 = _parser1.Release ().ToArray ();
        Assert.Equal (expectedRelease, BatchToString (released1));

        if (expectedTValues.Length > 0)
        {
            Assert.True (expectedTValues.SequenceEqual (released1.Select (kv => kv.Item2)));
        }

        Assert.Equal (expectedRelease, _parser2.Release ());

        Assert.Equal (ParserState.Normal, _parser1.State);
        Assert.Equal (ParserState.Normal, _parser2.State);
    }
}
