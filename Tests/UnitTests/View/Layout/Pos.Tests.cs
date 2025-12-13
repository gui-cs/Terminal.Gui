#nullable enable

namespace UnitTests.LayoutTests;

public class PosTests ()
{
    [Fact]
    public void
        Pos_Validation_Do_Not_Throws_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
    {
        Application.Init (DriverRegistry.Names.ANSI);

        Runnable t = new ();

        var w = new Window { X = Pos.Left (t) + 2, Y = Pos.Absolute (2) };

        var v = new View { X = Pos.Center (), Y = Pos.Percent (10) };

        w.Add (v);
        t.Add (w);

        t.IsModalChanged += (s, e) =>
                   {
                       v.Frame = new Rectangle (2, 2, 10, 10);
                       Assert.Equal (2, v.X = 2);
                       Assert.Equal (2, v.Y = 2);
                   };

        Application.StopAfterFirstIteration = true;

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }


    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void PosCombine_WHY_Throws ()
    {
        Application.Init (DriverRegistry.Names.ANSI);

        Runnable t = new Runnable ();

        var w = new Window { X = Pos.Left (t) + 2, Y = Pos.Top (t) + 2 };
        var f = new FrameView ();
        var v1 = new View { X = Pos.Left (w) + 2, Y = Pos.Top (w) + 2 };
        var v2 = new View { X = Pos.Left (v1) + 2, Y = Pos.Top (v1) + 2 };

        f.Add (v1); // v2 not added
        w.Add (f);
        t.Add (w);

        f.X = Pos.X (v2) - Pos.X (v1);
        f.Y = Pos.Y (v2) - Pos.Y (v1);

        Assert.Throws<LayoutException> (() => Application.Run (t));
        t.Dispose ();
        Application.Shutdown ();

        v2.Dispose ();
    }


    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [SetupFakeApplication]
    public void Pos_Add_Operator ()
    {
        Runnable top = new ();

        var view = new View { X = 0, Y = 0, Width = 20, Height = 20 };
        var field = new TextField { X = 0, Y = 0, Width = 20 };
        var count = 0;

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 field.Text = $"View {count}";
                                 var view2 = new View { X = 0, Y = field.Y, Width = 20, Text = field.Text };
                                 view.Add (view2);
                                 Assert.Equal ($"View {count}", view2.Text);
                                 Assert.Equal ($"Absolute({count})", view2.Y.ToString ());

                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                                 field.Y += 1;
                                 count++;
                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                             }
                         };

        Application.Iteration += OnInstanceOnIteration;

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);
        Application.Iteration -= OnInstanceOnIteration;

        Assert.Equal (20, count);

        top.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();

        return;

        void OnInstanceOnIteration (object? s, EventArgs<IApplication?> a)
        {
            while (count < 20)
            {
                field.NewKeyDownEvent (Key.Enter);
            }

            Application.RequestStop ();
        }
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void Pos_Subtract_Operator ()
    {
        Application.Init (DriverRegistry.Names.ANSI);

        Runnable top = new ();

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

        Application.Iteration += OnApplicationOnIteration;

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;

        Assert.Equal (0, count);

        top.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            while (count > 0)
            {
                field.NewKeyDownEvent (Key.Enter);
            }

            Application.RequestStop ();
        }
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void Pos_Validation_Do_Not_Throws_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Null ()
    {
        Application.Init (DriverRegistry.Names.ANSI);

        Runnable t = new ();

        var w = new Window { X = 1, Y = 2, Width = 3, Height = 5 };
        t.Add (w);

        t.IsModalChanged += (s, e) =>
                   {
                       Assert.Equal (2, w.X = 2);
                       Assert.Equal (2, w.Y = 2);
                   };

        Application.StopAfterFirstIteration = true;

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }


    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void Validation_Does_Not_Throw_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Null ()
    {
        Application.Init (DriverRegistry.Names.ANSI);

        Runnable t = new Runnable ();

        var w = new Window { X = 1, Y = 2, Width = 3, Height = 5 };
        t.Add (w);

        t.IsModalChanged += (s, e) =>
                   {
                       Assert.Equal (2, w.X = 2);
                       Assert.Equal (2, w.Y = 2);
                   };

        Application.StopAfterFirstIteration = true;

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }
}
