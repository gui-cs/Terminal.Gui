using System.Drawing;
using TerminalGuiFluentTesting;
using Xunit;

namespace TerminalGuiFluentTestingXunit;

public static partial class XunitContextExtensions
{
    // Placeholder


    /// <summary>
    /// Asserts that the last set cursor position matches <paramref name="expected"/>
    /// </summary>
    /// <param name="helper"></param>
    /// <param name="expected"></param>
    /// <returns></returns>
    public static AppTestHelper AssertCursorPosition (this AppTestHelper helper, Point expected)
    {
        try
        {
            Assert.Equal (expected, helper.GetCursorPosition ());
        }
        catch (Exception)
        {
            helper.HardStop ();

            throw;
        }

        return helper;
    }
}
