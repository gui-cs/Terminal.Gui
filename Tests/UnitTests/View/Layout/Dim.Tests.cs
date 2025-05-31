using System.Globalization;
using System.Text;
using UnitTests;
using Xunit.Abstractions;
using static Terminal.Gui.ViewBase.Dim;

namespace Terminal.Gui.LayoutTests;

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


    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [AutoInitShutdown]
    public void Only_DimAbsolute_And_DimFactor_As_A_Different_Procedure_For_Assigning_Value_To_Width_Or_Height ()
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        // Testing with the Button because it properly handles the Dim class.
        Toplevel t = new ();

        var w = new Window { Width = 100, Height = 100 };

        var f1 = new FrameView
        {
            X = 0,
            Y = 0,
            Width = Percent (50),
            Height = 5,
            Title = "f1"
        };

        var f2 = new FrameView
        {
            X = Pos.Right (f1),
            Y = 0,
            Width = Fill (),
            Height = 5,
            Title = "f2"
        };

        var v1 = new Button
        {
            X = Pos.X (f1) + 2,
            Y = Pos.Bottom (f1) + 2,
            Width = Width (f1) - 2,
            Height = Fill () - 2,
            ValidatePosDim = true,
            Text = "v1"
        };

        var v2 = new Button
        {
            X = Pos.X (f2) + 2,
            Y = Pos.Bottom (f2) + 2,
            Width = Width (f2) - 2,
            Height = Fill () - 2,
            ValidatePosDim = true,
            Text = "v2"
        };

        var v3 = new Button
        {
            Width = Percent (10),
            Height = Percent (10),
            ValidatePosDim = true,
            Text = "v3"
        };

        var v4 = new Button
        {
            Width = Absolute (50),
            Height = Absolute (50),
            ValidatePosDim = true,
            Text = "v4"
        };

        var v5 = new Button
        {
            Width = Width (v1) - Width (v3),
            Height = Height (v1) - Height (v3),
            ValidatePosDim = true,
            Text = "v5"
        };

        var v6 = new Button
        {
            X = Pos.X (f2),
            Y = Pos.Bottom (f2) + 2,
            Width = Percent (20, DimPercentMode.Position),
            Height = Percent (20, DimPercentMode.Position),
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

                       Assert.Equal ("Absolute(5)", f1.Height.ToString ());
                       Assert.Equal (49, f1.Frame.Width); // 50-1=49
                       Assert.Equal (5, f1.Frame.Height);

                       Assert.Equal ("Fill(Absolute(0))", f2.Width.ToString ());
                       Assert.Equal ("Absolute(5)", f2.Height.ToString ());
                       Assert.Equal (49, f2.Frame.Width); // 50-1=49
                       Assert.Equal (5, f2.Frame.Height);

                       Assert.Equal ($"Combine(View(Width,FrameView(){f1.Border.Frame})-Absolute(2))", v1.Width.ToString ());
                       Assert.Equal ("Combine(Fill(Absolute(0))-Absolute(2))", v1.Height.ToString ());
                       Assert.Equal (47, v1.Frame.Width); // 49-2=47
                       Assert.Equal (89, v1.Frame.Height); // 98-5-2-2=89

                       Assert.Equal (
                                     $"Combine(View(Width,FrameView(){f2.Frame})-Absolute(2))",
                                     v2.Width.ToString ()
                                    );
                       Assert.Equal ("Combine(Fill(Absolute(0))-Absolute(2))", v2.Height.ToString ());
                       Assert.Equal (47, v2.Frame.Width); // 49-2=47
                       Assert.Equal (89, v2.Frame.Height); // 98-5-2-2=89

                       Assert.Equal (9, v3.Frame.Width); // 98*10%=9
                       Assert.Equal (9, v3.Frame.Height); // 98*10%=9

                       Assert.Equal ("Absolute(50)", v4.Width.ToString ());
                       Assert.Equal ("Absolute(50)", v4.Height.ToString ());
                       Assert.Equal (50, v4.Frame.Width);
                       Assert.Equal (50, v4.Frame.Height);
                       Assert.Equal ($"Combine(View(Height,Button(){v1.Frame})-View(Height,Button(){v3.Viewport}))", v5.Height.ToString ( ));
                       Assert.Equal (38, v5.Frame.Width); // 47-9=38
                       Assert.Equal (80, v5.Frame.Height); // 89-9=80

                       Assert.Equal (9, v6.Frame.Width); // 47*20%=9
                       Assert.Equal (18, v6.Frame.Height); // 89*20%=18

                       w.Width = 200;
                       Assert.True (t.NeedsLayout);
                       w.Height = 200;
                       t.LayoutSubViews ();

                       Assert.Equal ("Absolute(200)", w.Width.ToString ());
                       Assert.Equal ("Absolute(200)", w.Height.ToString ());
                       Assert.Equal (200, w.Frame.Width);
                       Assert.Equal (200, w.Frame.Height);

                       f1.Text = "Frame1";
                       Assert.Equal (99, f1.Frame.Width); // 100-1=99
                       Assert.Equal (5, f1.Frame.Height);

                       f2.Text = "Frame2";
                       Assert.Equal ("Fill(Absolute(0))", f2.Width.ToString ());
                       Assert.Equal ("Absolute(5)", f2.Height.ToString ());
                       Assert.Equal (99, f2.Frame.Width); // 100-1=99
                       Assert.Equal (5, f2.Frame.Height);

                       v1.Text = "Button1";
                       Assert.Equal ($"Combine(View(Width,FrameView(){f1.Frame})-Absolute(2))", v1.Width.ToString ());
                       Assert.Equal ("Combine(Fill(Absolute(0))-Absolute(2))", v1.Height.ToString ());
                       Assert.Equal (97, v1.Frame.Width); // 99-2=97
                       Assert.Equal (189, v1.Frame.Height); // 198-2-7=189

                       v2.Text = "Button2";

                       Assert.Equal ($"Combine(View(Width,FrameView(){f2.Frame})-Absolute(2))", v2.Width.ToString ());
                       Assert.Equal ("Combine(Fill(Absolute(0))-Absolute(2))", v2.Height.ToString ());
                       Assert.Equal (97, v2.Frame.Width); // 99-2=97
                       Assert.Equal (189, v2.Frame.Height); // 198-2-7=189

                       v3.Text = "Button3";

                       // 198*10%=19 * Percent is related to the super-view if it isn't null otherwise the view width
                       Assert.Equal (19, v3.Frame.Width);

                       // 199*10%=19
                       Assert.Equal (19, v3.Frame.Height);

                       v4.Text = "Button4";
                       v4.Width = Auto (DimAutoStyle.Text);
                       v4.Height = Auto (DimAutoStyle.Text);
                       v4.Layout ();
                       Assert.Equal (Auto (DimAutoStyle.Text), v4.Width);
                       Assert.Equal (Auto (DimAutoStyle.Text), v4.Height);
                       Assert.Equal (11, v4.Frame.Width); // 11 is the text length and because is DimAbsolute
                       Assert.Equal (1, v4.Frame.Height); // 1 because is DimAbsolute

                       v5.Text = "Button5";

                       Assert.Equal ($"Combine(View(Width,Button(){v1.Frame})-View(Width,Button(){v3.Frame}))", v5.Width.ToString ());
                       Assert.Equal ($"Combine(View(Height,Button(){v1.Frame})-View(Height,Button(){v3.Frame}))", v5.Height.ToString ());

                       Assert.Equal (78, v5.Frame.Width); // 97-9=78
                       Assert.Equal (170, v5.Frame.Height); // 189-19=170

                       v6.Text = "Button6";
                       Assert.Equal (19, v6.Frame.Width); // 99*20%=19
                       Assert.Equal (38, v6.Frame.Height); // 198-7*20=18
                   };

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
        t.Dispose ();
    }
}
