#nullable enable
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class NavigationTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter? _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Runnable_TabGroup_Forward_Backward (TestDriver d)
    {
        var v1 = new View { Id = "v1", CanFocus = true };
        var v2 = new View { Id = "v2", CanFocus = true };
        var v3 = new View { Id = "v3", CanFocus = true };
        var v4 = new View { Id = "v4", CanFocus = true };
        var v5 = new View { Id = "v5", CanFocus = true };
        var v6 = new View { Id = "v6", CanFocus = true };

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((app) =>
                                            {
                                                var w1 = new Window { Id = "w1" };
                                                w1.Add (v1, v2);
                                                var w2 = new Window { Id = "w2" };
                                                w2.Add (v3, v4);
                                                var w3 = new Window { Id = "w3" };
                                                w3.Add (v5, v6);
                                                View top = app?.TopRunnableView!;
                                                app?.TopRunnableView!.Add (w1, w2, w3);
                                            })
                                     .AssertTrue (v5.HasFocus)
                                     .EnqueueKeyEvent (Key.F6)
                                     .AssertTrue (v1.HasFocus)
                                     .EnqueueKeyEvent (Key.F6)
                                     .AssertTrue (v3.HasFocus)
                                     .EnqueueKeyEvent (Key.F6.WithShift)
                                     .AssertTrue (v1.HasFocus)
                                     .EnqueueKeyEvent (Key.F6.WithShift)
                                     .AssertTrue (v5.HasFocus)
                                     .EnqueueKeyEvent (Key.F6.WithShift)
                                     .AssertTrue (v3.HasFocus)
                                     .EnqueueKeyEvent (Key.F6)
                                     .AssertTrue (v5.HasFocus)
                                     .EnqueueKeyEvent (Key.F6)
                                     .AssertTrue (v1.HasFocus)
                                     .EnqueueKeyEvent (Key.F6)
                                     .AssertTrue (v3.HasFocus)
                                     .EnqueueKeyEvent (Key.F6.WithShift)
                                     .AssertTrue (v1.HasFocus)
                                     .EnqueueKeyEvent (Key.F6.WithShift)
                                     .AssertTrue (v5.HasFocus)
                                     .EnqueueKeyEvent (Key.F6.WithShift)
                                     .AssertTrue (v3.HasFocus)
                                     .EnqueueKeyEvent (Key.Tab)
                                     .AssertTrue (v4.HasFocus)
                                     .EnqueueKeyEvent (Key.F6)
                                     .AssertTrue (v5.HasFocus)
                                     .EnqueueKeyEvent (Key.F6)
                                     .AssertTrue (v1.HasFocus)
                                     .EnqueueKeyEvent (Key.F6.WithShift)
                                     .AssertTrue (v5.HasFocus)
                                     .EnqueueKeyEvent (Key.Tab)
                                     .AssertTrue (v6.HasFocus)
                                     .EnqueueKeyEvent (Key.F6.WithShift)
                                     .AssertTrue (v4.HasFocus)
                                     .EnqueueKeyEvent (Key.F6)
                                     .AssertTrue (v6.HasFocus);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        Assert.False (v4.HasFocus);
        Assert.False (v5.HasFocus);
    }
}
