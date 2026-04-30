using System.Collections.Concurrent;

namespace DriverTests.Output;

/// <summary>
///     Proves the race condition in <see cref="OutputBufferImpl"/> where
///     <see cref="OutputBufferImpl.ClearContents()"/> replaces the <c>Contents</c>
///     reference while <see cref="OutputBufferImpl.AddStr"/> is concurrently iterating.
///     See: https://github.com/gui-cs/Terminal.Gui/issues/5130
/// </summary>
public class OutputBufferImplConcurrencyTests
{
    /// <summary>
    ///     Concurrently calls <see cref="OutputBufferImpl.AddStr"/> and
    ///     <see cref="OutputBufferImpl.ClearContents()"/> from separate threads.
    ///     With the current broken <c>lock (Contents)</c> pattern this reproduces
    ///     crashes (AccessViolationException, NullReferenceException, or
    ///     IndexOutOfRangeException) within a few hundred iterations.
    /// </summary>
    [Fact]
    public void AddStr_And_ClearContents_Concurrent_DoesNotThrow ()
    {
        // Arrange
        OutputBufferImpl buffer = new ();
        buffer.SetSize (80, 25);

        ConcurrentBag<Exception> exceptions = [];
        const int ITERATIONS = 5_000;
        CancellationTokenSource cts = new ();

        // Writer thread — continuously writes strings
        Thread writer = new (() =>
                              {
                                  try
                                  {
                                      for (var i = 0; i < ITERATIONS && !cts.IsCancellationRequested; i++)
                                      {
                                          try
                                          {
                                              buffer.Move (0, i % 25);
                                              buffer.AddStr (new string ('A', 80));
                                          }
                                          catch (Exception ex) when (ex is not OutOfMemoryException)
                                          {
                                              exceptions.Add (ex);

                                              break;
                                          }
                                      }
                                  }
                                  catch (Exception ex)
                                  {
                                      exceptions.Add (ex);
                                  }
                              }) { IsBackground = true };

        // Resizer/clearer thread — continuously replaces Contents
        Thread clearer = new (() =>
                               {
                                   try
                                   {
                                       for (var i = 0; i < ITERATIONS && !cts.IsCancellationRequested; i++)
                                       {
                                           try
                                           {
                                               // Alternate sizes to maximize chance of IndexOutOfRange
                                               buffer.SetSize (i % 2 == 0 ? 80 : 40, i % 2 == 0 ? 25 : 10);
                                           }
                                           catch (Exception ex) when (ex is not OutOfMemoryException)
                                           {
                                               exceptions.Add (ex);

                                               break;
                                           }
                                       }
                                   }
                                   catch (Exception ex)
                                   {
                                       exceptions.Add (ex);
                                   }
                               }) { IsBackground = true };

        // Act
        writer.Start ();
        clearer.Start ();

        writer.Join (TimeSpan.FromSeconds (10));
        clearer.Join (TimeSpan.FromSeconds (10));
        cts.Cancel ();

        // Assert — with the bug present, we expect exceptions to have been collected.
        // Once fixed this test should pass with zero exceptions.
        Assert.True (exceptions.IsEmpty,
                     $"Caught {exceptions.Count} exception(s) during concurrent access. "
                     + $"First: {exceptions.FirstOrDefault ()?.GetType ().Name}: {exceptions.FirstOrDefault ()?.Message}");
    }
}
