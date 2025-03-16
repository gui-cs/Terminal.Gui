using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using TerminalGuiFluentAssertions;

namespace UnitTests.FluentTests;
public class BasicFluentAssertionTests
{
    [Fact]
    public void GuiTestContext_StartsAndStopsWithoutError ()
    {
        using var context = With.A<Window> (40, 10);

        // No actual assertions are needed — if no exceptions are thrown, it's working
        context.Stop ();
    }

    [Fact]
    public void GuiTestContext_ForgotToStop ()
    {
        using var context = With.A<Window> (40, 10);
    }

    [Fact]
    public void TestWindowsResize ()
    {
        var lbl = new Label ()
                                {
                                    Width = Dim.Fill ()
                                };
        using var c = With.A<Window> (40, 10)
                          .Add (lbl )
                          .Assert (lbl.Frame.Width.Should().Be(38)) // Window has 2 border
                          .ResizeConsole (20,20)
                          .Assert (lbl.Frame.Width.Should ().Be (18))
                          .Stop ();
    }

    [Fact]
    public void ContextMenu_CrashesOnRight ()
    {
        var clicked = false;

        var ctx = new ContextMenu ();
        var menuItems = new MenuBarItem (
                                         [
                                             new ("_New File", string.Empty, () => { clicked = true; })
                                         ]
                                        );

        using var c = With.A<Window> (40, 10)
                          .WithContextMenu(ctx,menuItems)
                          // Click in main area inside border
                          .RightClick(1,1)
                          .LeftClick (2, 2)
                          /*.Assert (Application.Top.Focused.Should ().BeAssignableTo(typeof(MenuBarItem)))
                          .Down()
                          .Enter()*/
                          .Stop ();
        Assert.True (clicked);

    }
}
