#nullable enable
using UnitTests;
using Xunit.Abstractions;

namespace ViewsTests;

public class DialogTests (ITestOutputHelper output) : FakeDriverBase
{
    [Fact]
    public void Add_Button_Works ()
    {
        using IApplication? app = Application.Create ();
        app.Init ("fake");

        var title = "1234";
        var btn1Text = "yes";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";

        // We test with one button first, but do this to get the width right for 2
        int width = $@"{Glyphs.VLine} {btn1} {btn2} {Glyphs.VLine}".Length;
        app.Driver?.SetScreenSize (width, 1);

        // Override CM
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        // Default (center)
        Dialog dlg = new ()
        {
            Buttons = [new () { Text = btn1Text }]
        };
        dlg.Title = title;
        dlg.Width = width;
        dlg.Height = 1;
        dlg.ButtonAlignment = Alignment.Center;

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc.)
        dlg.Border!.Thickness = new (1, 0, 1, 0);
        var buttonRow = $"{Glyphs.VLine}    {btn1}     {Glyphs.VLine}";

        app.StopAfterFirstIteration = true;
        app.Run (dlg);

        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);

        // Now add a second button
        buttonRow = $"{Glyphs.VLine} {btn1} {btn2} {Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });

        app.StopAfterFirstIteration = true;
        app.Run (dlg);

        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);

        // Justify
        dlg.Dispose ();
        dlg = new ()
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.Fill,
            Buttons = [new () { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc.)
        dlg.Border!.Thickness = new (1, 0, 1, 0);
        app.StopAfterFirstIteration = true;
        app.Run (dlg);

        buttonRow = $"{Glyphs.VLine}{btn1}         {Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);

        // Now add a second button
        buttonRow = $"{Glyphs.VLine}{btn1}   {btn2}{Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });

        app.StopAfterFirstIteration = true;
        app.Run (dlg);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);

        // Right
        dlg.Dispose ();
        dlg = new ()
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.End,
            Buttons = [new () { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc.)
        dlg.Border!.Thickness = new (1, 0, 1, 0);

        app.StopAfterFirstIteration = true;
        app.Run (dlg);

        buttonRow = $"{Glyphs.VLine}{new (' ', width - btn1.Length - 2)}{btn1}{Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);

        // Now add a second button
        buttonRow = $"{Glyphs.VLine}  {btn1} {btn2}{Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });

        app.StopAfterFirstIteration = true;
        app.Run (dlg);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);

        dlg.Dispose ();
        // Left
        dlg = new ()
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.Start,
            Buttons = [new () { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc.)
        dlg.Border!.Thickness = new (1, 0, 1, 0);
        app.StopAfterFirstIteration = true;
        app.Run (dlg);

        buttonRow = $"{Glyphs.VLine}{btn1}{new (' ', width - btn1.Length - 2)}{Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);

        // Now add a second button
        buttonRow = $"{Glyphs.VLine}{btn1} {btn2}  {Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });

        app.StopAfterFirstIteration = true;
        app.Run (dlg);
        dlg.Dispose ();
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
    }



#if X
    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four ()
    {
        SessionToken? sessionToken = null;

        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ]|"
        var btn1Text = "yes";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";
        var btn3Text = "maybe";
        var btn3 = $"{Glyphs.LeftBracket} {btn3Text} {Glyphs.RightBracket}";
        var btn4Text = "never";
        var btn4 = $"{Glyphs.LeftBracket} {btn4Text} {Glyphs.RightBracket}";

        var buttonRow = $"{Glyphs.VLine} {btn1} {btn2} {btn3} {btn4} {Glyphs.VLine}";
        int width = buttonRow.Length;

        app.Driver?.SetScreenSize (buttonRow.Length, 3);

        // Default - Center
        (sessionToken, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text },
                                                        new Button { Text = btn4Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{Glyphs.VLine}{btn1}  {btn2}  {btn3} {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );

        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        buttonRow = $"{Glyphs.VLine}  {btn1} {btn2} {btn3} {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        buttonRow = $"{Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}  {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_On_Too_Small_Width ()
    {
        SessionToken? sessionToken = null;

        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ][ never ]|"
        var btn1Text = "yes";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";
        var btn3Text = "maybe";
        var btn3 = $"{Glyphs.LeftBracket} {btn3Text} {Glyphs.RightBracket}";
        var btn4Text = "never";
        var btn4 = $"{Glyphs.LeftBracket} {btn4Text} {Glyphs.RightBracket}";
        var buttonRow = string.Empty;

        var width = 30;
        app.Driver?.SetScreenSize (width, 1);

        // Default - Center
        buttonRow =
            $"{Glyphs.VLine} yes {Glyphs.RightBracket}{btn2}{btn3}{Glyphs.LeftBracket} never{Glyphs.VLine}";

        (sessionToken, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text },
                                                        new Button { Text = btn4Text }
                                                       );
        Assert.Equal (new (width, 1), dlg.Frame.Size);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} yes {Glyphs.LeftBracket} no {Glyphs.LeftBracket} maybe {Glyphs.LeftBracket} never {Glyphs.RightBracket}{Glyphs.VLine}";

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        buttonRow = $"{Glyphs.VLine}es {Glyphs.RightBracket}{btn2}{btn3}{btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        buttonRow = $"{Glyphs.VLine}{btn1}{btn2}{btn3}{Glyphs.LeftBracket} neve{Glyphs.VLine}";

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_WideOdd ()
    {
        SessionToken? sessionToken = null;

        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ]|"
        var btn1Text = "really long button 1";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "really long button 2";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";
        var btn3Text = "really long button 3";
        var btn3 = $"{Glyphs.LeftBracket} {btn3Text} {Glyphs.RightBracket}";
        var btn4Text = "really long button 44"; // 44 is intentional to make length different from rest
        var btn4 = $"{Glyphs.LeftBracket} {btn4Text} {Glyphs.RightBracket}";

        // Note extra spaces to make dialog even wider
        //                         123456                          1234567
        var buttonRow = $"{Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {Glyphs.VLine}";
        int width = buttonRow.Length;
        app.Driver?.SetScreenSize (buttonRow.Length, 1);

        // Default - Center
        (sessionToken, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text },
                                                        new Button { Text = btn4Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        buttonRow = $"{Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        buttonRow = $"{Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_Wider ()
    {
        SessionToken? sessionToken = null;

        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ]|"
        var btn1Text = "yes";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";
        var btn3Text = "你你你你你"; // This is a wide char
        var btn3 = $"{Glyphs.LeftBracket} {btn3Text} {Glyphs.RightBracket}";

        // Requires a Nerd Font
        var btn4Text = "\uE36E\uE36F\uE370\uE371\uE372\uE373";
        var btn4 = $"{Glyphs.LeftBracket} {btn4Text} {Glyphs.RightBracket}";

        // Note extra spaces to make dialog even wider
        //                         123456                           123456
        var buttonRow = $"{Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {Glyphs.VLine}";
        int width = buttonRow.GetColumns ();
        app.Driver?.SetScreenSize (width, 3);

        // Default - Center
        (sessionToken, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text },
                                                        new Button { Text = btn4Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        buttonRow = $"{Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        buttonRow = $"{Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_One ()
    {
        SessionToken? sessionToken = null;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ ok ]|"
        var btnText = "ok";

        var buttonRow =
            $"{Glyphs.VLine}  {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}  {Glyphs.VLine}";
        int width = buttonRow.Length;

        app.Driver?.SetScreenSize (width, 1);

        (sessionToken, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btnText }
                                                       );

        // Center
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}    {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        buttonRow =
            $"{Glyphs.VLine}    {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}    {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Wider
        buttonRow =
            $"{Glyphs.VLine}   {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}   {Glyphs.VLine}";
        width = buttonRow.Length;

        app.Driver?.SetScreenSize (width, 1);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Center,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}      {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        buttonRow =
            $"{Glyphs.VLine}      {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}      {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Three ()
    {
        SessionToken? sessionToken = null;

        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ]|"
        var btn1Text = "yes";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";
        var btn3Text = "maybe";
        var btn3 = $"{Glyphs.LeftBracket} {btn3Text} {Glyphs.RightBracket}";

        var buttonRow = $@"{Glyphs.VLine} {btn1} {btn2} {btn3} {Glyphs.VLine}";
        int width = buttonRow.Length;

        app.Driver?.SetScreenSize (buttonRow.Length, 3);

        (sessionToken, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        buttonRow = $@"{Glyphs.VLine}{btn1}  {btn2}  {btn3}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        buttonRow = $@"{Glyphs.VLine}  {btn1} {btn2} {btn3}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        buttonRow = $@"{Glyphs.VLine}{btn1} {btn2} {btn3}  {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Two ()
    {
        SessionToken? sessionToken = null;

        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ]|"
        var btn1Text = "yes";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";

        var buttonRow = $@"{Glyphs.VLine} {btn1} {btn2} {Glyphs.VLine}";
        int width = buttonRow.Length;

        app.Driver?.SetScreenSize (buttonRow.Length, 3);

        (sessionToken, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        buttonRow = $@"{Glyphs.VLine}{btn1}   {btn2}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        buttonRow = $@"{Glyphs.VLine}  {btn1} {btn2}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        buttonRow = $@"{Glyphs.VLine}{btn1} {btn2}  {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (sessionToken, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Two_Hidden ()
    {
        SessionToken? sessionToken = null;

        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ]|"
        var btn1Text = "yes";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";

        var buttonRow = $@"{Glyphs.VLine} {btn1} {btn2} {Glyphs.VLine}";
        int width = buttonRow.Length;

        app.Driver?.SetScreenSize (buttonRow.Length, 3);

        // Default (Center)
        Button button1 = new () { Text = btn1Text };
        Button button2 = new () { Text = btn2Text };
        (sessionToken, Dialog dlg) = BeginButtonTestDialog (title, width, Alignment.Center, button1, button2);
        button1.Visible = false;
        AutoInitShutdownAttribute.RunIteration ();

        buttonRow = $@"{Glyphs.VLine}         {btn2} {Glyphs.VLine}";

        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Justify
        Assert.Equal (width, buttonRow.Length);
        button1 = new () { Text = btn1Text };
        button2 = new () { Text = btn2Text };
        (sessionToken, dlg) = BeginButtonTestDialog (title, width, Alignment.Fill, button1, button2);
        button1.Visible = false;
        AutoInitShutdownAttribute.RunIteration ();

        buttonRow = $@"{Glyphs.VLine}          {btn2}{Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Right
        Assert.Equal (width, buttonRow.Length);
        button1 = new () { Text = btn1Text };
        button2 = new () { Text = btn2Text };
        (sessionToken, dlg) = BeginButtonTestDialog (title, width, Alignment.End, button1, button2);
        button1.Visible = false;
        AutoInitShutdownAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();

        // Left
        Assert.Equal (width, buttonRow.Length);
        button1 = new () { Text = btn1Text };
        button2 = new () { Text = btn2Text };
        (sessionToken, dlg) = BeginButtonTestDialog (title, width, Alignment.Start, button1, button2);
        button1.Visible = false;
        AutoInitShutdownAttribute.RunIteration ();

        buttonRow = $@"{Glyphs.VLine}        {btn2}  {Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Can_Access_Cancel_Property_After_Run ()
    {
        Dialog dlg = new ();

        ApplicationImpl.Instance.StopAfterFirstIteration = true;

        app.Run (dlg);

#if DEBUG_IDISPOSABLE
        Assert.False (dlg.WasDisposed);
#endif

        Assert.False (dlg.Canceled);

        // Run it again is possible because it isn't disposed yet
        app.Run (dlg);

        // Run another view without dispose the prior will throw an assertion
#if DEBUG_IDISPOSABLE
        Dialog dlg2 = new ();

        //   Exception exception = Record.Exception (() => app.Run (dlg2));
        //     Assert.NotNull (exception);

        dlg.Dispose ();

        // Now it's possible to tun dlg2 without throw
        app.Run (dlg2);

        Assert.True (dlg.WasDisposed);
        Assert.False (dlg2.WasDisposed);

        dlg2.Dispose ();

        app.Shutdown ();
        Assert.True (dlg2.WasDisposed);
#endif
    }

    [Fact (Skip = "Convoluted test that needs to be rewritten")]
    [AutoInitShutdown]
    public void Dialog_In_Window_With_Size_One_Button_Aligns ()
    {
        app.Driver?.SetScreenSize (20, 5);

        // Override CM
        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var win = new Window ();

        var iterations = 0;

        app.Iteration += OnApplicationOnIteration;
        var btn = $"{Glyphs.LeftBracket} Ok {Glyphs.RightBracket}";

        win.IsModalChanged += (s, a) =>
                      {
                          if (!a.Value)
                          {
                              return;
                          }
                          var dlg = new Dialog { Width = 18, Height = 3, Buttons = [new () { Text = "Ok" }] };

                          dlg.IsModalChanged += (s, a) =>
                                        {
                                            AutoInitShutdownAttribute.RunIteration ();

                                            var expected = @$"
┌──────────────────┐
│┌────────────────┐│
││     {btn}     ││
│└────────────────┘│
└──────────────────┘";
                                            _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);
                                        };

                          app.Run (dlg);
                          dlg.Dispose ();
                      };
        app.Run (win);
        app.Iteration -= OnApplicationOnIteration;
        win.Dispose ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            if (++iterations > 2)
            {
                app.RequestStop ();
            }
        }
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (
                    5,
                    @"
┌┌───────────────┐─┐
││               │ │
││    ⟦ Ok ⟧     │ │
│└───────────────┘ │
└──────────────────┘"
                )]
    [InlineData (
                    6,
                    @"
┌┌───────────────┐─┐
││               │ │
││               │ │
││    ⟦ Ok ⟧     │ │
│└───────────────┘ │
└──────────────────┘"
                )]
    [InlineData (
                    7,
                    @"
┌──────────────────┐
│┌───────────────┐ │
││               │ │
││               │ │
││    ⟦ Ok ⟧     │ │
│└───────────────┘ │
└──────────────────┘"
                )]
    [InlineData (
                    8,
                    @"
┌──────────────────┐
│┌───────────────┐ │
││               │ │
││               │ │
││               │ │
││    ⟦ Ok ⟧     │ │
│└───────────────┘ │
└──────────────────┘"
                )]
    [InlineData (
                    9,
                    @"
┌──────────────────┐
│┌───────────────┐ │
││               │ │
││               │ │
││               │ │
││               │ │
││    ⟦ Ok ⟧     │ │
│└───────────────┘ │
└──────────────────┘"
                )]
    public void Dialog_In_Window_Without_Size_One_Button_Aligns (int height, string expected)
    {
        app.Driver?.SetScreenSize (20, height);
        var win = new Window ();

        int iterations = -1;

        // Override CM
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        app.Iteration += OnApplicationOnIteration;

        app.Run (win);
        app.Iteration -= OnApplicationOnIteration;
        win.Dispose ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterations++;

            if (iterations == 0)
            {
                var dlg = new Dialog { Buttons = [new () { Text = "Ok" }], Width = Dim.Percent (85), Height = Dim.Percent (85) };
                app.Run (dlg);
                dlg.Dispose ();
            }
            else if (iterations == 1)
            {
                AutoInitShutdownAttribute.RunIteration ();

                // BUGBUG: This seems wrong; is it a bug in Dim.Percent(85)?? No
                _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);
            }
            else
            {
                app.RequestStop ();
            }
        }
    }

    [Fact (Skip = "Convoluted test that needs to be rewritten")]
    [AutoInitShutdown]
    public void Dialog_Opened_From_Another_Dialog ()
    {
        app.Driver?.SetScreenSize (30, 10);

        // Override CM
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var btn1 = new Button { Text = "press me 1" };
        Button? btn2 = null;
        Button? btn3 = null;
        string? expected = null;

        btn1.Accepting += (s, e) =>
                          {
                              btn2 = new () { Text = "Show Sub" };
                              btn3 = new () { Text = "Close" };
                              btn3.Accepting += (s, e) => app.RequestStop ();

                              btn2.Accepting += (s, e) =>
                                                {
                                                    // Don't test MessageBox in Dialog unit tests!
                                                    var subBtn = new Button { Text = "Ok", IsDefault = true };
                                                    var subDlg = new Dialog { Text = "ya", Width = 20, Height = 5, Buttons = [subBtn] };
                                                    subBtn.Accepting += (s, e) => app.RequestStop (subDlg);
                                                    app.Run (subDlg);
                                                };

                              var dlg = new Dialog
                              {
                                  Buttons = [btn2, btn3],
                                  Width = Dim.Percent (85),
                                  Height = Dim.Percent (85)
                              };

                              app.Run (dlg);
                              dlg.Dispose ();
                          };

        var btn =
            $"{Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} Ok {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}";

        int iterations = -1;

        app.Iteration += OnApplicationOnIteration;

        app.Run<Runnable> ();
        app.Iteration -= OnApplicationOnIteration;
        app.Shutdown ();

        Assert.Equal (9, iterations);

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterations++;

            switch (iterations)
            {
                case 0:
                    app.TopRunnableView!.SetNeedsLayout ();
                    app.TopRunnableView.SetNeedsDraw ();

                    break;

                case 1:
                    Assert.False (btn1.NewKeyDownEvent (Key.Space));

                    break;

                // Now this happens on iteration 3 because Space triggers Run on the new dialog which itself causes another iteration
                // as it starts. Meaning we haven't exited case 1 when we enter case 2 from next Run stack frame.
                case 3:

                    expected = @$"
  ┌───────────────────────┐
  │                       │
  │                       │
  │                       │
  │                       │
  │                       │
  │{Glyphs.LeftBracket} Show Sub {Glyphs.RightBracket} {Glyphs.LeftBracket} Close {Glyphs.RightBracket} │
  └───────────────────────┘";
                    DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);

                    Assert.False (btn2!.NewKeyDownEvent (Key.Space));

                    break;
                case 5:

                    DriverAssert.AssertDriverContentsWithFrameAre (
                                                                   @$"
  ┌───────────────────────┐
  │  ┌──────────────────┐ │
  │  │ya                │ │
  │  │                  │ │
  │  │     {btn}     │ │
  │  └──────────────────┘ │
  │{Glyphs.LeftBracket} Show Sub {Glyphs.RightBracket} {Glyphs.LeftBracket} Close {Glyphs.RightBracket} │
  └───────────────────────┘",
                                                                   output, app.Driver);

                    Assert.False (app.TopRunnableView!.NewKeyDownEvent (Key.Enter));

                    break;
                case 7:

                    DriverAssert.AssertDriverContentsWithFrameAre (expected!, output, app.Driver);

                    Assert.False (btn3!.NewKeyDownEvent (Key.Space));

                    break;
                case 9:
                    DriverAssert.AssertDriverContentsWithFrameAre ("", output, app.Driver);

                    app.RequestStop ();

                    break;
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void FileDialog_FileSystemWatcher ()
    {
        for (var i = 0; i < 8; i++)
        {
            ApplicationImpl.Instance.StopAfterFirstIteration = true;
            var fd = new FileDialog ();
            app.Run (fd);
            fd.Dispose ();
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Location_Default ()
    {
        var d = new Dialog
        {
            Width = Dim.Percent (85),
            Height = Dim.Percent (85)
        };
        app.Begin (d);
        app.Driver?.SetScreenSize (100, 100);
        app.LayoutAndDraw ();

        // Default location is centered, so 100 / 2 - 85 / 2 = 7
        var expected = 7;
        Assert.Equal (new (expected, expected), d.Frame.Location);
        d.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Location_Not_Default ()
    {
        var d = new Dialog { X = 1, Y = 1 };
        app.Begin (d);
        app.Driver?.SetScreenSize (100, 100);

        // Default location is centered, so 100 / 2 - 85 / 2 = 7
        var expected = 1;
        Assert.Equal (new (expected, expected), d.Frame.Location);
        d.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Location_When_Application_Top_Not_Default ()
    {
        // Override CM
        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var expected = 5;
        var d = new Dialog { X = expected, Y = expected, Height = 5, Width = 5 };
        app.Begin (d);
        app.Driver?.SetScreenSize (20, 10);
        app.LayoutAndDraw ();

        // Default location is centered, so 100 / 2 - 85 / 2 = 7
        Assert.Equal (new (expected, expected), d.Frame.Location);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
     ┌───┐
     │   │
     │   │
     │   │
     └───┘",
                                                       output
                                                      );
        d.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Modal_Captures_All_Mouse ()
    {
        var top = new Runnable
        {
            Id = "top"
        };

        var d = new Dialog
        {
            Width = 10,
            Height = 10,
            X = 1,
            Y = 1
        };

        app.Driver?.SetScreenSize (20, 20);

        var iterations = 0;

        app.Iteration += OnApplicationOnIteration;

        top.MouseEvent += (s, e) =>
                          {
                              // This should not be called because the dialog is modal
                              Assert.Fail ("Mouse event should not be captured by the top level when a dialog is modal.");
                          };

        app.Run (top);
        top.Dispose ();
        app.Iteration -= OnApplicationOnIteration;
        app.Shutdown ();

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            if (++iterations > 2)
            {
                app.RequestStop ();
            }

            if (iterations == 1)
            {
                app.Run (d);
                d.Dispose ();
            }
            else if (iterations == 2)
            {
                // Mouse click outside of dialog
                app.Mouse.RaiseMouseEvent (new () { Flags = MouseFlags.Button1Clicked, ScreenPosition = new (0, 0) });
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void One_Button_Works ()
    {
        SessionToken? sessionToken = null;

        Button.DefaultShadow = ShadowStyle.None;

        var title = "";
        var btnText = "ok";

        var buttonRow =
            $"{Glyphs.VLine}   {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}   {Glyphs.VLine}";

        int width = buttonRow.Length;
        app.Driver?.SetScreenSize (buttonRow.Length, 10);

        (sessionToken, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btnText }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);
        app.End (sessionToken);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Run_Does_Not_Dispose_Dialog ()
    {
        var top = new Runnable ();

        Dialog dlg = new () { };

        ApplicationImpl.Instance.StopAfterFirstIteration = true;
        app.Run (dlg);

#if DEBUG_IDISPOSABLE
        Assert.False (dlg.WasDisposed);
#endif

        // dlg wasn't disposed yet and it's possible to access to his properties
        Assert.False (dlg.Canceled);
        Assert.NotNull (dlg);

        dlg.Canceled = true;
        Assert.True (dlg.Canceled);

        dlg.Dispose ();
        top.Dispose ();
#if DEBUG_IDISPOSABLE
        Assert.True (dlg.WasDisposed);
#endif
        app.Shutdown ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Size_Default ()
    {
        var d = new Dialog
        {
            Width = Dim.Percent (85),
            Height = Dim.Percent (85)
        };

        app.Begin (d);
        app.Driver?.SetScreenSize (100, 100);
        app.LayoutAndDraw ();

        // Default size is Percent(85) 
        Assert.Equal (new ((int)(100 * .85), (int)(100 * .85)), d.Frame.Size);
        d.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Size_Not_Default ()
    {
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;
        var d = new Dialog { Width = 50, Height = 50 };

        app.Begin (d);
        app.Driver?.SetScreenSize (100, 100);

        // Default size is Percent(85) 
        Assert.Equal (new (50, 50), d.Frame.Size);
        d.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Zero_Buttons_Works ()
    {
        SessionToken? sessionToken = null;

        var title = "1234";

        var buttonRow = $"{Glyphs.VLine}        {Glyphs.VLine}";
        int width = buttonRow.Length;
        app.Driver?.SetScreenSize (buttonRow.Length, 3);

        (sessionToken, Dialog dlg) = BeginButtonTestDialog (title, width, Alignment.Center);

        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output, app.Driver);

        app.End (sessionToken);
        dlg.Dispose ();
    }

    private (SessionToken, Dialog) BeginButtonTestDialog (
        string title,
        int width,
        Alignment align,
        params Button [] btns
    )
    {
        // Override CM
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var dlg = new Dialog
        {
            Title = title,
            X = 0,
            Y = 0,
            Width = width,
            Height = 1,
            ButtonAlignment = align,
            Buttons = btns
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border!.Thickness = new (1, 0, 1, 0);

        SessionToken sessionToken = app.Begin (dlg);

        dlg.SetNeedsDraw ();
        dlg.SetNeedsLayout ();

        AutoInitShutdownAttribute.RunIteration ();

        return (sessionToken, dlg);
    }
#endif

}
