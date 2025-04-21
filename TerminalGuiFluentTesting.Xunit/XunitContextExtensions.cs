using Xunit;

namespace TerminalGuiFluentTesting;

public static class XunitContextExtensions
{
    public static GuiTestContext AssertTrue (this GuiTestContext context, bool? condition)
    {
        context.Then (
                      () =>
                      {
                          Assert.True (condition);
                      });
        return context;
    }
    public static GuiTestContext AssertEqual (this GuiTestContext context, object? expected, object? actual)
    {
        context.Then (
                      () =>
                      {
                          Assert.Equal (expected,actual);
                      });
        return context;
    }
}
