using Terminal.Gui;
using TerminalGuiFluentTesting;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class TreeViewFluentTests
{
    private readonly TextWriter _out;

    public TreeViewFluentTests (ITestOutputHelper outputHelper) { _out = new TestOutputWriter (outputHelper); }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void TreeView_AllowReOrdering (V2TestDriver d)
    {
        var tv = new TreeView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        TreeNode car;
        TreeNode lorry;
        TreeNode bike;

        var root = new TreeNode ("Root")
        {
            Children =
            [
                car = new ("Car"),
                lorry = new ("Lorry"),
                bike = new ("Bike")
            ]
        };

        tv.AddObject (root);

        using GuiTestContext context =
            With.A<Window> (40, 10, d)
                .Add (tv)
                .Focus (tv)
                .WaitIteration ()
                .ScreenShot ("Before expanding", _out)
                .AssertEqual (root, tv.GetObjectOnRow (0))
                .Then (() => Assert.Null (tv.GetObjectOnRow (1)))
                .Right ()
                .ScreenShot ("After expanding", _out)
                .AssertEqual (root, tv.GetObjectOnRow (0))
                .AssertEqual (car, tv.GetObjectOnRow (1))
                .AssertEqual (lorry, tv.GetObjectOnRow (2))
                .AssertEqual (bike, tv.GetObjectOnRow (3))
                .Then (
                       () =>
                       {
                           // Re order
                           root.Children = [bike, car, lorry];
                           tv.RefreshObject (root);
                       })
                .WaitIteration ()
                .ScreenShot ("After re-order", _out)
                .AssertEqual (root, tv.GetObjectOnRow (0))
                .AssertEqual (bike, tv.GetObjectOnRow (1))
                .AssertEqual (car, tv.GetObjectOnRow (2))
                .AssertEqual (lorry, tv.GetObjectOnRow (3))
                .WriteOutLogs (_out);

        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void TreeViewReOrder_PreservesExpansion (V2TestDriver d)
    {
        var tv = new TreeView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        TreeNode car;
        TreeNode lorry;
        TreeNode bike;

        TreeNode mrA;
        TreeNode mrB;

        TreeNode mrC;

        TreeNode mrD;
        TreeNode mrE;

        var root = new TreeNode ("Root")
        {
            Children =
            [
                car = new ("Car")
                {
                    Children =
                    [
                        mrA = new ("Mr A"),
                        mrB = new ("Mr B")
                    ]
                },
                lorry = new ("Lorry")
                {
                    Children =
                    [
                        mrC = new ("Mr C")
                    ]
                },
                bike = new ("Bike")
                {
                    Children =
                    [
                        mrD = new ("Mr D"),
                        mrE = new ("Mr E")
                    ]
                }
            ]
        };

        tv.AddObject (root);
        tv.ExpandAll ();

        using GuiTestContext context =
            With.A<Window> (40, 13, d)
                .Add (tv)
                .WaitIteration ()
                .ScreenShot ("Initial State", _out)
                .AssertEqual (root, tv.GetObjectOnRow (0))
                .AssertEqual (car, tv.GetObjectOnRow (1))
                .AssertEqual (mrA, tv.GetObjectOnRow (2))
                .AssertEqual (mrB, tv.GetObjectOnRow (3))
                .AssertEqual (lorry, tv.GetObjectOnRow (4))
                .AssertEqual (mrC, tv.GetObjectOnRow (5))
                .AssertEqual (bike, tv.GetObjectOnRow (6))
                .AssertEqual (mrD, tv.GetObjectOnRow (7))
                .AssertEqual (mrE, tv.GetObjectOnRow (8))
                .Then (
                       () =>
                       {
                           // Re order
                           root.Children = [bike, car, lorry];
                           tv.RefreshObject (root);
                       })
                .WaitIteration ()
                .ScreenShot ("After re-order", _out)
                .AssertEqual (root, tv.GetObjectOnRow (0))
                .AssertEqual (bike, tv.GetObjectOnRow (1))
                .AssertEqual (mrD, tv.GetObjectOnRow (2))
                .AssertEqual (mrE, tv.GetObjectOnRow (3))
                .AssertEqual (car, tv.GetObjectOnRow (4))
                .AssertEqual (mrA, tv.GetObjectOnRow (5))
                .AssertEqual (mrB, tv.GetObjectOnRow (6))
                .AssertEqual (lorry, tv.GetObjectOnRow (7))
                .AssertEqual (mrC, tv.GetObjectOnRow (8))
                .WriteOutLogs (_out);

        context.Stop ();
    }
}
