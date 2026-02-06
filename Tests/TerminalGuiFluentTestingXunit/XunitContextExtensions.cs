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
    /// <param name="context"></param>
    /// <param name="expected"></param>
    /// <returns></returns>
    public static GuiTestContext AssertCursorPosition (this GuiTestContext context, Point expected)
    {
        try
        {
            Assert.Equal (expected, context.GetCursorPosition ());
        }
        catch (Exception)
        {
            context.HardStop ();

            throw;
        }

        return context;
    }
}
