using UnitTests;
using Xunit.Abstractions;
using static Terminal.Gui.ViewBase.Pos;

namespace Terminal.Gui.LayoutTests;

public class PosViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Subtract_Operator ()
    {
        Application.Init (new FakeDriver ());

        var top = new Toplevel ();

        var view = new View { X = 0, Y = 0, Width = 20, Height = 20 };
        var field = new TextField { X = 0, Y = 0, Width = 20 };
        var count = 20;
        List<View> listViews = new ();

        for (var i = 0; i < count; i++)
        {
            field.Text = $"View {i}";
            var view2 = new View { X = 0, Y = field.Y, Width = 20, Text = field.Text };
            view.Add (view2);
            Assert.Equal ($"View {i}", view2.Text);
            Assert.Equal ($"Absolute({i})", field.Y.ToString ());
            listViews.Add (view2);

            Assert.Equal ($"Absolute({i})", field.Y.ToString ());
            field.Y += 1;
            Assert.Equal ($"Absolute({i + 1})", field.Y.ToString ());
        }

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 Assert.Equal ($"View {count - 1}", listViews [count - 1].Text);
                                 view.Remove (listViews [count - 1]);
                                 listViews [count - 1].Dispose ();

                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                                 field.Y -= 1;
                                 count--;
                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                             }
                         };

        Application.Iteration += (s, a) =>
                                 {
                                     while (count > 0)
                                     {
                                         field.NewKeyDownEvent (new (KeyCode.Enter));
                                     }

                                     Application.RequestStop ();
                                 };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);
        top.Dispose ();
        Assert.Equal (0, count);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }
}
