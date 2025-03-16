using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminalGuiFluentAssertions;

namespace UnitTests.FluentTests;
public class BasicFluentAssertionTests
{
    [Fact]
    public void GuiTestContext_StartsAndStopsWithoutError ()
    {
        var context = With.A<Window> (40, 10);

        // No actual assertions are needed — if no exceptions are thrown, it's working
        context.Stop ();
    }

    [Fact]
    public void Test ()
    {
        var myView = new TextField () { Width = 10 };



        /*
        using var ctx = With.A<Window> (20, 10)
                            .Add (myView, 3, 2)
                            .Focus (myView)
                            .Type ("Hello");

        Assert.Equal ("Hello", myView.Text);*/
    }
}
