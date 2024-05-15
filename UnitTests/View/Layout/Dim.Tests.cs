using System.Globalization;
using System.Text;
using Xunit.Abstractions;
using static Terminal.Gui.Dim;


// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.PosDimTests;

public class DimTests
{
    private readonly ITestOutputHelper _output;

    public DimTests (ITestOutputHelper output)
    {
        _output = output;
        Console.OutputEncoding = Encoding.Default;

        // Change current culture
        var culture = CultureInfo.CreateSpecificCulture ("en-US");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    [Fact]
    public void DimAbsolute_Calculate_ReturnsCorrectValue ()
    {
        var dim = new DimAbsolute (10);
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (10, result);
    }


    [Fact]
    public void DimView_Calculate_ReturnsCorrectValue ()
    {
        var view = new View { Width = 10 };
        var dim = new DimView (view, Dimension.Width);
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (10, result);
    }


    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // A new test that does not depend on Application is needed.
    [Fact]
    [AutoInitShutdown]
    public void Dim_Add_Operator ()
    {
        Toplevel top = new ();

        var view = new View { X = 0, Y = 0, Width = 20, Height = 0 };
        var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
        var count = 0;

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 field.Text = $"Label {count}";
                                 var label = new Label { X = 0, Y = view.Viewport.Height, /*Width = 20,*/ Text = field.Text };
                                 view.Add (label);
                                 Assert.Equal ($"Label {count}", label.Text);
                                 Assert.Equal ($"Absolute({count})", label.Y.ToString ());

                                 Assert.Equal ($"Absolute({count})", view.Height.ToString ());
                                 view.Height += 1;
                                 count++;
                                 Assert.Equal ($"Absolute({count})", view.Height.ToString ());
                             }
                         };

        Application.Iteration += (s, a) =>
                                 {
                                     while (count < 20)
                                     {
                                         field.NewKeyDownEvent (Key.Enter);
                                     }

                                     Application.RequestStop ();
                                 };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);
        top.Dispose ();

        Assert.Equal (20, count);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Dim_Referencing_SuperView_Does_Not_Throw ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };

        var view = new View
        {
            Width = Dim.Width (super), // this is allowed
            Height = Dim.Height (super), // this is allowed
            Text = "view"
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();

        Exception exception = Record.Exception (super.LayoutSubviews);
        Assert.Null (exception);
        super.Dispose ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [AutoInitShutdown]
    public void Dim_Subtract_Operator ()
    {
        Toplevel top = new ();

        var view = new View { X = 0, Y = 0, Width = 20, Height = 0 };
        var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
        var count = 20;
        List<Label> listLabels = new ();

        for (var i = 0; i < count; i++)
        {
            field.Text = $"Label {i}";
            var label = new Label { X = 0, Y = view.Viewport.Height, /*Width = 20,*/ Text = field.Text };
            view.Add (label);
            Assert.Equal ($"Label {i}", label.Text);
            Assert.Equal ($"Absolute({i})", label.Y.ToString ());
            listLabels.Add (label);

            Assert.Equal ($"Absolute({i})", view.Height.ToString ());
            view.Height += 1;
            Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
        }

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 Assert.Equal ($"Label {count - 1}", listLabels [count - 1].Text);
                                 view.Remove (listLabels [count - 1]);
                                 listLabels [count - 1].Dispose ();

                                 Assert.Equal ($"Absolute({count})", view.Height.ToString ());
                                 view.Height -= 1;
                                 count--;
                                 Assert.Equal ($"Absolute({count})", view.Height.ToString ());
                             }
                         };

        Application.Iteration += (s, a) =>
                                 {
                                     while (count > 0)
                                     {
                                         field.NewKeyDownEvent (Key.Enter);
                                     }

                                     Application.RequestStop ();
                                 };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);

        Assert.Equal (0, count);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Dim_SyperView_Referencing_SubView_Throws ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };
        var view2 = new View { Width = 10, Height = 10, Text = "view2" };

        var view = new View
        {
            Width = Dim.Width (view2), // this is not allowed
            Height = Dim.Height (view2), // this is not allowed
            Text = "view"
        };

        view.Add (view2);
        super.Add (view);
        super.BeginInit ();
        super.EndInit ();

        Assert.Throws<InvalidOperationException> (super.LayoutSubviews);
        super.Dispose ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void
        Dim_Validation_Do_Not_Throws_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
    {
        var t = new View { Width = 80, Height = 25, Text = "top" };

        var w = new Window { Width = Dim.Fill (), Height = Dim.Sized (10) };
        var v = new View { Width = Dim.Width (w) - 2, Height = Dim.Percent (10), Text = "v" };

        w.Add (v);
        t.Add (w);

        Assert.Equal (LayoutStyle.Absolute, t.LayoutStyle);
        Assert.Equal (LayoutStyle.Computed, w.LayoutStyle);
        Assert.Equal (LayoutStyle.Computed, v.LayoutStyle);

        t.LayoutSubviews ();
        Assert.Equal (2, v.Width = 2);
        Assert.Equal (2, v.Height = 2);

        // Force v to be LayoutStyle.Absolute;
        v.Frame = new Rectangle (0, 1, 3, 4);
        Assert.Equal (LayoutStyle.Absolute, v.LayoutStyle);
        t.LayoutSubviews ();

        Assert.Equal (2, v.Width = 2);
        Assert.Equal (2, v.Height = 2);
        t.Dispose ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Dim_Validation_Do_Not_Throws_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Null ()
    {
        var t = new View { Width = 80, Height = 25, Text = "top" };

        var w = new Window
        {
            X = 1,
            Y = 2,
            Width = 4,
            Height = 5,
            Title = "w"
        };
        t.Add (w);
        t.LayoutSubviews ();

        Assert.Equal (3, w.Width = 3);
        Assert.Equal (4, w.Height = 4);
        t.Dispose ();
    }


    // See #2461
    //[Fact]
    //public void Dim_Referencing_SuperView_Throws ()
    //{
    //	var super = new View ("super") {
    //		Width = 10,
    //		Height = 10
    //	};
    //	var view = new View ("view") {
    //		Width = Dim.Width (super),	// this is not allowed
    //		Height = Dim.Height (super),    // this is not allowed
    //	};

    //	super.Add (view);
    //	super.BeginInit ();
    //	super.EndInit ();
    //	Assert.Throws<InvalidOperationException> (() => super.LayoutSubviews ());
    //}

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void
        Dim_Validation_Does_Not_Throw_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
    {
        var t = new View { Width = 80, Height = 25, Text = "top" };

        var w = new Window { Width = Dim.Fill (), Height = Dim.Sized (10) };
        var v = new View { Width = Dim.Width (w) - 2, Height = Dim.Percent (10), Text = "v" };

        w.Add (v);
        t.Add (w);

        Assert.Equal (LayoutStyle.Absolute, t.LayoutStyle);
        Assert.Equal (LayoutStyle.Computed, w.LayoutStyle);
        Assert.Equal (LayoutStyle.Computed, v.LayoutStyle);

        t.LayoutSubviews ();
        Assert.Equal (2, v.Width = 2);
        Assert.Equal (2, v.Height = 2);

        // Force v to be LayoutStyle.Absolute;
        v.Frame = new Rectangle (0, 1, 3, 4);
        Assert.Equal (LayoutStyle.Absolute, v.LayoutStyle);
        t.LayoutSubviews ();

        Assert.Equal (2, v.Width = 2);
        Assert.Equal (2, v.Height = 2);
        t.Dispose ();
    }


    [Fact]
    public void DimHeight_Set_To_Null_Throws ()
    {
        Dim dim = Dim.Height (null);
        Assert.Throws<NullReferenceException> (() => dim.ToString ());
    }

    [Fact]
    [TestRespondersDisposed]
    public void DimHeight_SetsValue ()
    {
        var testVal = Rectangle.Empty;
        var testValview = new View { Frame = testVal };
        Dim dim = Dim.Height (testValview);
        Assert.Equal ($"View(Height,View(){testVal})", dim.ToString ());
        testValview.Dispose ();

        testVal = new Rectangle (1, 2, 3, 4);
        testValview = new View { Frame = testVal };
        dim = Dim.Height (testValview);
        Assert.Equal ($"View(Height,View(){testVal})", dim.ToString ());
        testValview.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void Internal_Tests ()
    {
        var dimFactor = new DimPercent (0.10F);
        Assert.Equal (10, dimFactor.Anchor (100));

        var dimAbsolute = new DimAbsolute (10);
        Assert.Equal (10, dimAbsolute.Anchor (0));

        var dimFill = new DimFill (1);
        Assert.Equal (99, dimFill.Anchor (100));

        var dimCombine = new DimCombine (true, dimFactor, dimAbsolute);
        Assert.Equal (dimCombine.Left, dimFactor);
        Assert.Equal (dimCombine.Right, dimAbsolute);
        Assert.Equal (20, dimCombine.Anchor (100));

        var view = new View { Frame = new Rectangle (20, 10, 20, 1) };
        var dimViewHeight = new DimView (view, Dimension.Height);
        Assert.Equal (1, dimViewHeight.Anchor (0));
        var dimViewWidth = new DimView (view, Dimension.Width);
        Assert.Equal (20, dimViewWidth.Anchor (0));

        view.Dispose ();
    }

    [Fact]
    public void New_Works ()
    {
        var dim = new Dim ();
        Assert.Equal ("Terminal.Gui.Dim", dim.ToString ());
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [AutoInitShutdown]
    public void Only_DimAbsolute_And_DimFactor_As_A_Different_Procedure_For_Assigning_Value_To_Width_Or_Height ()
    {
        // Testing with the Button because it properly handles the Dim class.
        Toplevel t = new ();

        var w = new Window { Width = 100, Height = 100 };

        var f1 = new FrameView
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent (50),
            Height = 5,
            Title = "f1"
        };

        var f2 = new FrameView
        {
            X = Pos.Right (f1),
            Y = 0,
            Width = Dim.Fill (),
            Height = 5,
            Title = "f2"
        };

        var v1 = new Button
        {
            X = Pos.X (f1) + 2,
            Y = Pos.Bottom (f1) + 2,
            Width = Dim.Width (f1) - 2,
            Height = Dim.Fill () - 2,
            ValidatePosDim = true,
            Text = "v1"
        };

        var v2 = new Button
        {
            X = Pos.X (f2) + 2,
            Y = Pos.Bottom (f2) + 2,
            Width = Dim.Width (f2) - 2,
            Height = Dim.Fill () - 2,
            ValidatePosDim = true,
            Text = "v2"
        };

        var v3 = new Button
        {
            Width = Dim.Percent (10),
            Height = Dim.Percent (10),
            ValidatePosDim = true,
            Text = "v3"
        };

        var v4 = new Button
        {
            Width = Dim.Sized (50),
            Height = Dim.Sized (50),
            ValidatePosDim = true,
            Text = "v4"
        };

        var v5 = new Button
        {
            Width = Dim.Width (v1) - Dim.Width (v3),
            Height = Dim.Height (v1) - Dim.Height (v3),
            ValidatePosDim = true,
            Text = "v5"
        };

        var v6 = new Button
        {
            X = Pos.X (f2),
            Y = Pos.Bottom (f2) + 2,
            Width = Dim.Percent (20, true),
            Height = Dim.Percent (20, true),
            ValidatePosDim = true,
            Text = "v6"
        };

        w.Add (f1, f2, v1, v2, v3, v4, v5, v6);
        t.Add (w);

        t.Ready += (s, e) =>
                   {
                       Assert.Equal ("Absolute(100)", w.Width.ToString ());
                       Assert.Equal ("Absolute(100)", w.Height.ToString ());
                       Assert.Equal (100, w.Frame.Width);
                       Assert.Equal (100, w.Frame.Height);

                       Assert.Equal ("Percent(0.5,False)", f1.Width.ToString ());
                       Assert.Equal ("Absolute(5)", f1.Height.ToString ());
                       Assert.Equal (49, f1.Frame.Width); // 50-1=49
                       Assert.Equal (5, f1.Frame.Height);

                       Assert.Equal ("Fill(0)", f2.Width.ToString ());
                       Assert.Equal ("Absolute(5)", f2.Height.ToString ());
                       Assert.Equal (49, f2.Frame.Width); // 50-1=49
                       Assert.Equal (5, f2.Frame.Height);

#if DEBUG
                       Assert.Equal ($"Combine(View(Width,FrameView(f1){f1.Border.Frame})-Absolute(2))", v1.Width.ToString ());
#else
                       Assert.Equal ($"Combine(View(Width,FrameView(){f1.Border.Frame})-Absolute(2))", v1.Width.ToString ());
#endif
                       Assert.Equal ("Combine(Fill(0)-Absolute(2))", v1.Height.ToString ());
                       Assert.Equal (47, v1.Frame.Width); // 49-2=47
                       Assert.Equal (89, v1.Frame.Height); // 98-5-2-2=89

#if DEBUG
                       Assert.Equal (
                                     $"Combine(View(Width,FrameView(f2){f2.Frame})-Absolute(2))",
                                     v2.Width.ToString ()
#else
                       Assert.Equal (
                                     $"Combine(View(Width,FrameView(){f2.Frame})-Absolute(2))",
                                     v2.Width.ToString ()
#endif
                                    );
#if DEBUG
                       Assert.Equal ("Combine(Fill(0)-Absolute(2))", v2.Height.ToString ());
#else
                       Assert.Equal ("Combine(Fill(0)-Absolute(2))", v2.Height.ToString ());
#endif
                       Assert.Equal (47, v2.Frame.Width); // 49-2=47
                       Assert.Equal (89, v2.Frame.Height); // 98-5-2-2=89

                       Assert.Equal ("Percent(0.1,False)", v3.Width.ToString ());
                       Assert.Equal ("Percent(0.1,False)", v3.Height.ToString ());
                       Assert.Equal (9, v3.Frame.Width); // 98*10%=9
                       Assert.Equal (9, v3.Frame.Height); // 98*10%=9

                       Assert.Equal ("Absolute(50)", v4.Width.ToString ());
                       Assert.Equal ("Absolute(50)", v4.Height.ToString ());
                       Assert.Equal (50, v4.Frame.Width);
                       Assert.Equal (50, v4.Frame.Height);
#if DEBUG
                       Assert.Equal ($"Combine(View(Width,Button(v1){v1.Frame})-View(Width,Button(v3){v3.Viewport}))", v5.Width.ToString ());
#else
                       Assert.Equal ($"Combine(View(Height,Button(){v1.Frame})-View(Height,Button(){v3.Viewport}))", v5.Height.ToString ( ));
#endif
                       Assert.Equal (38, v5.Frame.Width);  // 47-9=38
                       Assert.Equal (80, v5.Frame.Height); // 89-9=80

                       Assert.Equal ("Percent(0.2,True)", v6.Width.ToString ());
                       Assert.Equal ("Percent(0.2,True)", v6.Height.ToString ());
                       Assert.Equal (9, v6.Frame.Width);   // 47*20%=9
                       Assert.Equal (18, v6.Frame.Height); // 89*20%=18

                       w.Width = 200;
                       Assert.True (t.LayoutNeeded);
                       w.Height = 200;
                       t.LayoutSubviews ();

                       Assert.Equal ("Absolute(200)", w.Width.ToString ());
                       Assert.Equal ("Absolute(200)", w.Height.ToString ());
                       Assert.Equal (200, w.Frame.Width);
                       Assert.Equal (200, w.Frame.Height);

                       f1.Text = "Frame1";
                       Assert.Equal ("Percent(0.5,False)", f1.Width.ToString ());
                       Assert.Equal ("Absolute(5)", f1.Height.ToString ());
                       Assert.Equal (99, f1.Frame.Width); // 100-1=99
                       Assert.Equal (5, f1.Frame.Height);

                       f2.Text = "Frame2";
                       Assert.Equal ("Fill(0)", f2.Width.ToString ());
                       Assert.Equal ("Absolute(5)", f2.Height.ToString ());
                       Assert.Equal (99, f2.Frame.Width); // 100-1=99
                       Assert.Equal (5, f2.Frame.Height);

                       v1.Text = "Button1";
#if DEBUG
                       Assert.Equal ($"Combine(View(Width,FrameView(f1){f1.Frame})-Absolute(2))", v1.Width.ToString ());
#else
                       Assert.Equal ($"Combine(View(Width,FrameView(){f1.Frame})-Absolute(2))", v1.Width.ToString ());
#endif
                       Assert.Equal ("Combine(Fill(0)-Absolute(2))", v1.Height.ToString ());
                       Assert.Equal (97, v1.Frame.Width);   // 99-2=97
                       Assert.Equal (189, v1.Frame.Height); // 198-2-7=189

                       v2.Text = "Button2";

#if DEBUG
                       Assert.Equal ($"Combine(View(Width,FrameView(f2){f2.Frame})-Absolute(2))", v2.Width.ToString ());
#else
                       Assert.Equal ($"Combine(View(Width,FrameView(){f2.Frame})-Absolute(2))", v2.Width.ToString ());
#endif
                       Assert.Equal ("Combine(Fill(0)-Absolute(2))", v2.Height.ToString ());
                       Assert.Equal (97, v2.Frame.Width);   // 99-2=97
                       Assert.Equal (189, v2.Frame.Height); // 198-2-7=189

                       v3.Text = "Button3";
                       Assert.Equal ("Percent(0.1,False)", v3.Width.ToString ());
                       Assert.Equal ("Percent(0.1,False)", v3.Height.ToString ());

                       // 198*10%=19 * Percent is related to the super-view if it isn't null otherwise the view width
                       Assert.Equal (19, v3.Frame.Width);
                       // 199*10%=19
                       Assert.Equal (19, v3.Frame.Height);

                       v4.Text = "Button4";
                       v4.Width = Auto(DimAutoStyle.Text);
                       v4.Height = Auto (DimAutoStyle.Text);
                       Assert.Equal (Dim.Auto (DimAutoStyle.Text), v4.Width);
                       Assert.Equal (Dim.Auto (DimAutoStyle.Text), v4.Height);
                       Assert.Equal (11, v4.Frame.Width); // 11 is the text length and because is DimAbsolute
                       Assert.Equal (1, v4.Frame.Height); // 1 because is DimAbsolute

                       v5.Text = "Button5";

#if DEBUG
                       Assert.Equal ($"Combine(View(Width,Button(v1){v1.Frame})-View(Width,Button(v3){v3.Frame}))", v5.Width.ToString ());
                       Assert.Equal ($"Combine(View(Height,Button(v1){v1.Frame})-View(Height,Button(v3){v3.Frame}))", v5.Height.ToString ());
#else
                       Assert.Equal ($"Combine(View(Width,Button(){v1.Frame})-View(Width,Button(){v3.Frame}))", v5.Width.ToString ());
                       Assert.Equal ($"Combine(View(Height,Button(){v1.Frame})-View(Height,Button(){v3.Frame}))", v5.Height.ToString ());
#endif

                       Assert.Equal (78, v5.Frame.Width);   // 97-9=78
                       Assert.Equal (170, v5.Frame.Height); // 189-19=170

                       v6.Text = "Button6";
                       Assert.Equal ("Percent(0.2,True)", v6.Width.ToString ());
                       Assert.Equal ("Percent(0.2,True)", v6.Height.ToString ());
                       Assert.Equal (19, v6.Frame.Width);  // 99*20%=19
                       Assert.Equal (38, v6.Frame.Height); // 198-7*20=18
                   };

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Referencing_SuperView_Does_Not_Throw ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };

        var view = new View
        {
            Width = Dim.Width (super), // this is allowed
            Height = Dim.Height (super), // this is allowed
            Text = "view"
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();

        Exception exception = Record.Exception (super.LayoutSubviews);
        Assert.Null (exception);
        super.Dispose ();
    }

    [Fact]
    public void DimSized_Equals ()
    {
        var n1 = 0;
        var n2 = 0;
        Dim dim1 = Dim.Sized (n1);
        Dim dim2 = Dim.Sized (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 1;
        dim1 = Dim.Sized (n1);
        dim2 = Dim.Sized (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = -1;
        dim1 = Dim.Sized (n1);
        dim2 = Dim.Sized (n2);
        Assert.Equal (dim1, dim2);

        n1 = 0;
        n2 = 1;
        dim1 = Dim.Sized (n1);
        dim2 = Dim.Sized (n2);
        Assert.NotEqual (dim1, dim2);
    }

    [Fact]
    public void DimSized_SetsValue ()
    {
        Dim dim = Dim.Sized (0);
        Assert.Equal ("Absolute(0)", dim.ToString ());

        var testVal = 5;
        dim = Dim.Sized (testVal);
        Assert.Equal ($"Absolute({testVal})", dim.ToString ());

        testVal = -1;
        dim = Dim.Sized (testVal);
        Assert.Equal ($"Absolute({testVal})", dim.ToString ());
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void SyperView_Referencing_SubView_Throws ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };
        var view2 = new View { Width = 10, Height = 10, Text = "view2" };

        var view = new View
        {
            Width = Dim.Width (view2), // this is not allowed
            Height = Dim.Height (view2), // this is not allowed
            Text = "view"
        };

        view.Add (view2);
        super.Add (view);
        super.BeginInit ();
        super.EndInit ();

        Assert.Throws<InvalidOperationException> (super.LayoutSubviews);
        super.Dispose ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Validation_Does_Not_Throw_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Null ()
    {
        var t = new View { Width = 80, Height = 25, Text = "top" };

        var w = new Window
        {
            X = 1,
            Y = 2,
            Width = 4,
            Height = 5,
            Title = "w"
        };
        t.Add (w);
        t.LayoutSubviews ();

        Assert.Equal (3, w.Width = 3);
        Assert.Equal (4, w.Height = 4);
        t.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void DimWidth_Equals ()
    {
        var testRect1 = Rectangle.Empty;
        var view1 = new View { Frame = testRect1 };
        var testRect2 = Rectangle.Empty;
        var view2 = new View { Frame = testRect2 };

        Dim dim1 = Dim.Width (view1);
        Dim dim2 = Dim.Width (view1);

        // FIXED: Dim.Width should support Equals() and this should change to Equal.
        Assert.Equal (dim1, dim2);

        dim2 = Dim.Width (view2);
        Assert.NotEqual (dim1, dim2);

        testRect1 = new Rectangle (0, 1, 2, 3);
        view1 = new View { Frame = testRect1 };
        testRect2 = new Rectangle (0, 1, 2, 3);
        dim1 = Dim.Width (view1);
        dim2 = Dim.Width (view1);

        // FIXED: Dim.Width should support Equals() and this should change to Equal.
        Assert.Equal (dim1, dim2);

        testRect1 = new Rectangle (0, -1, 2, 3);
        view1 = new View { Frame = testRect1 };
        testRect2 = new Rectangle (0, -1, 2, 3);
        dim1 = Dim.Width (view1);
        dim2 = Dim.Width (view1);

        // FIXED: Dim.Width should support Equals() and this should change to Equal.
        Assert.Equal (dim1, dim2);

        testRect1 = new Rectangle (0, -1, 2, 3);
        view1 = new View { Frame = testRect1 };
        testRect2 = Rectangle.Empty;
        view2 = new View { Frame = testRect2 };
        dim1 = Dim.Width (view1);
        dim2 = Dim.Width (view2);
        Assert.NotEqual (dim1, dim2);
#if DEBUG_IDISPOSABLE

        // HACK: Force clean up of Responders to avoid having to Dispose all the Views created above.
        Responder.Instances.Clear ();
        Assert.Empty (Responder.Instances);
#endif
    }

    [Fact]
    public void DimWidth_Set_To_Null_Throws ()
    {
        Dim dim = Dim.Width (null);
        Assert.Throws<NullReferenceException> (() => dim.ToString ());
    }

    [Fact]
    [TestRespondersDisposed]
    public void DimWidth_SetsValue ()
    {
        var testVal = Rectangle.Empty;
        var testValView = new View { Frame = testVal };
        Dim dim = Dim.Width (testValView);
        Assert.Equal ($"View(Width,View(){testVal})", dim.ToString ());
        testValView.Dispose ();

        testVal = new Rectangle (1, 2, 3, 4);
        testValView = new View { Frame = testVal };
        dim = Dim.Width (testValView);
        Assert.Equal ($"View(Width,View(){testVal})", dim.ToString ());
        testValView.Dispose ();
    }
}
