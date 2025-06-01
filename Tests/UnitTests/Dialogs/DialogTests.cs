#nullable enable
using UnitTests;
using Xunit.Abstractions;
using static Terminal.Gui.App.Application;

namespace Terminal.Gui.DialogTests;

public class DialogTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void Add_Button_Works ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;

        var title = "1234";
        var btn1Text = "yes";
        var btn1 = $"{Glyphs.LeftBracket} {btn1Text} {Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{Glyphs.LeftBracket} {btn2Text} {Glyphs.RightBracket}";

        // We test with one button first, but do this to get the width right for 2
        int width = $@"{Glyphs.VLine} {btn1} {btn2} {Glyphs.VLine}".Length;
        d.SetBufferSize (width, 1);

        // Override CM
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        // Default (center)
        var dlg = new Dialog
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.Center,
            Buttons = [new () { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border!.Thickness = new (1, 0, 1, 0);
        runState = Begin (dlg);
        var buttonRow = $"{Glyphs.VLine}    {btn1}     {Glyphs.VLine}";

        RunIteration (ref runState);

        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

        // Now add a second button
        buttonRow = $"{Glyphs.VLine} {btn1} {btn2} {Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });

        RunIteration (ref runState);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        dlg = new ()
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.Fill,
            Buttons = [new () { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border!.Thickness = new (1, 0, 1, 0);
        runState = Begin (dlg);

        RunIteration (ref runState);

        buttonRow = $"{Glyphs.VLine}{btn1}         {Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

        // Now add a second button
        buttonRow = $"{Glyphs.VLine}{btn1}   {btn2}{Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });
        RunIteration (ref runState);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        dlg = new ()
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.End,
            Buttons = [new () { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border!.Thickness = new (1, 0, 1, 0);
        runState = Begin (dlg);

        RunIteration (ref runState);

        buttonRow = $"{Glyphs.VLine}{new (' ', width - btn1.Length - 2)}{btn1}{Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

        // Now add a second button
        buttonRow = $"{Glyphs.VLine}  {btn1} {btn2}{Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });

        RunIteration (ref runState);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
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

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border!.Thickness = new (1, 0, 1, 0);
        runState = Begin (dlg);
        RunIteration (ref runState);

        buttonRow = $"{Glyphs.VLine}{btn1}{new (' ', width - btn1.Length - 2)}{Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

        // Now add a second button
        buttonRow = $"{Glyphs.VLine}{btn1} {btn2}  {Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });

        RunIteration (ref runState);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;
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
        d.SetBufferSize (buttonRow.Length, 3);

        // Default - Center
        (runState, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text },
                                                        new Button { Text = btn4Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{Glyphs.VLine}{btn1}  {btn2}  {btn3} {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        buttonRow = $"{Glyphs.VLine}  {btn1} {btn2} {btn3} {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        buttonRow = $"{Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}  {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_On_Too_Small_Width ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;
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
        d.SetBufferSize (width, 1);

        // Default - Center
        buttonRow =
            $"{Glyphs.VLine} yes {Glyphs.RightBracket}{btn2}{btn3}{Glyphs.LeftBracket} never{Glyphs.VLine}";

        (runState, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text },
                                                        new Button { Text = btn4Text }
                                                       );
        Assert.Equal (new (width, 1), dlg.Frame.Size);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} yes {Glyphs.LeftBracket} no {Glyphs.LeftBracket} maybe {Glyphs.LeftBracket} never {Glyphs.RightBracket}{Glyphs.VLine}";

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        buttonRow = $"{Glyphs.VLine}es {Glyphs.RightBracket}{btn2}{btn3}{btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        buttonRow = $"{Glyphs.VLine}{btn1}{btn2}{btn3}{Glyphs.LeftBracket} neve{Glyphs.VLine}";

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_WideOdd ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;
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
        var btn4Text = "really long button 44"; // 44 is intentional to make length different than rest
        var btn4 = $"{Glyphs.LeftBracket} {btn4Text} {Glyphs.RightBracket}";

        // Note extra spaces to make dialog even wider
        //                         123456                          1234567
        var buttonRow = $"{Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {Glyphs.VLine}";
        int width = buttonRow.Length;
        d.SetBufferSize (buttonRow.Length, 1);

        // Default - Center
        (runState, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text },
                                                        new Button { Text = btn4Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        buttonRow = $"{Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        buttonRow = $"{Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_Wider ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;
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
        d.SetBufferSize (width, 3);

        // Default - Center
        (runState, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text },
                                                        new Button { Text = btn4Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        buttonRow = $"{Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        buttonRow = $"{Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text },
                                                 new Button { Text = btn4Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_One ()
    {
        var d = (FakeDriver)Driver!;
        RunState? runState = null;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ ok ]|"
        var btnText = "ok";

        var buttonRow =
            $"{Glyphs.VLine}  {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}  {Glyphs.VLine}";
        int width = buttonRow.Length;

        d.SetBufferSize (width, 1);

        (runState, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btnText }
                                                       );

        // Center
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify 
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}    {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        buttonRow =
            $"{Glyphs.VLine}    {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}    {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Wider
        buttonRow =
            $"{Glyphs.VLine}   {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}   {Glyphs.VLine}";
        width = buttonRow.Length;

        d.SetBufferSize (width, 1);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Center,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}      {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        buttonRow =
            $"{Glyphs.VLine}      {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        buttonRow =
            $"{Glyphs.VLine}{Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}      {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btnText }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Three ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;
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

        d.SetBufferSize (buttonRow.Length, 3);

        (runState, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text },
                                                        new Button { Text = btn3Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        buttonRow = $@"{Glyphs.VLine}{btn1}  {btn2}  {btn3}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        buttonRow = $@"{Glyphs.VLine}  {btn1} {btn2} {btn3}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        buttonRow = $@"{Glyphs.VLine}{btn1} {btn2} {btn3}  {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text },
                                                 new Button { Text = btn3Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Two ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;
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

        d.SetBufferSize (buttonRow.Length, 3);

        (runState, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btn1Text },
                                                        new Button { Text = btn2Text }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        buttonRow = $@"{Glyphs.VLine}{btn1}   {btn2}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Fill,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        buttonRow = $@"{Glyphs.VLine}  {btn1} {btn2}{Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.End,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        buttonRow = $@"{Glyphs.VLine}{btn1} {btn2}  {Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runState, dlg) = BeginButtonTestDialog (
                                                 title,
                                                 width,
                                                 Alignment.Start,
                                                 new Button { Text = btn1Text },
                                                 new Button { Text = btn2Text }
                                                );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Two_Hidden ()
    {
        RunState? runState = null;
        var firstIteration = false;

        var d = (FakeDriver)Driver!;
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

        d.SetBufferSize (buttonRow.Length, 3);

        Dialog dlg = null;
        Button button1, button2;

        // Default (Center)
        button1 = new () { Text = btn1Text };
        button2 = new () { Text = btn2Text };
        (runState, dlg) = BeginButtonTestDialog (title, width, Alignment.Center, button1, button2);
        button1.Visible = false;
        RunIteration (ref runState, firstIteration);
        buttonRow = $@"{Glyphs.VLine}         {btn2} {Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Justify
        Assert.Equal (width, buttonRow.Length);
        button1 = new () { Text = btn1Text };
        button2 = new () { Text = btn2Text };
        (runState, dlg) = BeginButtonTestDialog (title, width, Alignment.Fill, button1, button2);
        button1.Visible = false;
        RunIteration (ref runState, firstIteration);
        buttonRow = $@"{Glyphs.VLine}          {btn2}{Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Right
        Assert.Equal (width, buttonRow.Length);
        button1 = new () { Text = btn1Text };
        button2 = new () { Text = btn2Text };
        (runState, dlg) = BeginButtonTestDialog (title, width, Alignment.End, button1, button2);
        button1.Visible = false;
        RunIteration (ref runState, firstIteration);
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();

        // Left
        Assert.Equal (width, buttonRow.Length);
        button1 = new () { Text = btn1Text };
        button2 = new () { Text = btn2Text };
        (runState, dlg) = BeginButtonTestDialog (title, width, Alignment.Start, button1, button2);
        button1.Visible = false;
        RunIteration (ref runState, firstIteration);
        buttonRow = $@"{Glyphs.VLine}        {btn2}  {Glyphs.VLine}";
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Dialog_In_Window_With_Size_One_Button_Aligns ()
    {
        ((FakeDriver)Driver!).SetBufferSize (20, 5);

        // Override CM
        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var win = new Window ();

        var iterations = 0;

        Iteration += (s, a) =>
                     {
                         if (++iterations > 2)
                         {
                             RequestStop ();
                         }
                     };
        var btn = $"{Glyphs.LeftBracket} Ok {Glyphs.RightBracket}";

        win.Loaded += (s, a) =>
                      {
                          var dlg = new Dialog { Width = 18, Height = 3, Buttons = [new () { Text = "Ok" }] };

                          dlg.Loaded += (s, a) =>
                                        {
                                            LayoutAndDraw ();

                                            var expected = @$"
┌──────────────────┐
│┌────────────────┐│
││     {btn}     ││
│└────────────────┘│
└──────────────────┘";
                                            _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
                                        };

                          Run (dlg);
                          dlg.Dispose ();
                      };
        Run (win);
        win.Dispose ();
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
        ((FakeDriver)Driver!).SetBufferSize (20, height);
        var win = new Window ();

        int iterations = -1;

        // Override CM
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Iteration += (s, a) =>
                     {
                         iterations++;

                         if (iterations == 0)
                         {
                             var dlg = new Dialog
                             {
                                 Buttons = [new () { Text = "Ok" }],
                                 Width = Dim.Percent (85),
                                 Height = Dim.Percent (85)
                             };
                             Run (dlg);
                             dlg.Dispose ();
                         }
                         else if (iterations == 1)
                         {
                             LayoutAndDraw ();

                             // BUGBUG: This seems wrong; is it a bug in Dim.Percent(85)?? No
                             _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
                         }
                         else
                         {
                             RequestStop ();
                         }
                     };

        Run (win);
        win.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Dialog_Opened_From_Another_Dialog ()
    {
        ((FakeDriver)Driver!).SetBufferSize (30, 10);

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
                              btn3.Accepting += (s, e) => RequestStop ();

                              btn2.Accepting += (s, e) =>
                                                {
                                                    // Don't test MessageBox in Dialog unit tests!
                                                    var subBtn = new Button { Text = "Ok", IsDefault = true };
                                                    var subDlg = new Dialog { Text = "ya", Width = 20, Height = 5, Buttons = [subBtn] };
                                                    subBtn.Accepting += (s, e) => RequestStop (subDlg);
                                                    Run (subDlg);
                                                };

                              var dlg = new Dialog
                              {
                                  Buttons = [btn2, btn3],
                                  Width = Dim.Percent (85),
                                  Height = Dim.Percent (85)
                              };

                              Run (dlg);
                              dlg.Dispose ();
                          };

        var btn =
            $"{Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} Ok {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}";

        int iterations = -1;

        Iteration += (s, a) =>
                     {
                         iterations++;

                         switch (iterations)
                         {
                             case 0:
                                 Top!.SetNeedsLayout ();
                                 Top.SetNeedsDraw ();
                                 LayoutAndDraw ();

                                 break;

                             case 1:
                                 Assert.False (btn1.NewKeyDownEvent (Key.Space));

                                 break;
                             case 2:
                                 LayoutAndDraw ();

                                 expected = @$"
  ┌───────────────────────┐
  │                       │
  │                       │
  │                       │
  │                       │
  │                       │
  │{Glyphs.LeftBracket} Show Sub {Glyphs.RightBracket} {Glyphs.LeftBracket} Close {Glyphs.RightBracket} │
  └───────────────────────┘";
                                 DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

                                 Assert.False (btn2!.NewKeyDownEvent (Key.Space));

                                 break;
                             case 3:
                                 LayoutAndDraw ();

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
                                                                                output
                                                                               );

                                 Assert.False (Top!.NewKeyDownEvent (Key.Enter));

                                 break;
                             case 4:
                                 LayoutAndDraw ();

                                 DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

                                 Assert.False (btn3!.NewKeyDownEvent (Key.Space));

                                 break;
                             case 5:
                                 DriverAssert.AssertDriverContentsWithFrameAre ("", output);

                                 RequestStop ();

                                 break;
                         }
                     };

        Run ().Dispose ();
        Shutdown ();

        Assert.Equal (5, iterations);
    }

    [Fact]
    [AutoInitShutdown]
    public void FileDialog_FileSystemWatcher ()
    {
        for (var i = 0; i < 8; i++)
        {
            var fd = new FileDialog ();
            fd.Ready += (s, e) => RequestStop ();
            Run (fd);
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
        Begin (d);
        ((FakeDriver)Driver!).SetBufferSize (100, 100);

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
        Begin (d);
        ((FakeDriver)Driver!).SetBufferSize (100, 100);

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
        Begin (d);
        ((FakeDriver)Driver!).SetBufferSize (20, 10);

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
    public void One_Button_Works ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;

        Button.DefaultShadow = ShadowStyle.None;

        var title = "";
        var btnText = "ok";

        var buttonRow =
            $"{Glyphs.VLine}   {Glyphs.LeftBracket} {btnText} {Glyphs.RightBracket}   {Glyphs.VLine}";

        int width = buttonRow.Length;
        d.SetBufferSize (buttonRow.Length, 10);

        (runState, Dialog dlg) = BeginButtonTestDialog (
                                                        title,
                                                        width,
                                                        Alignment.Center,
                                                        new Button { Text = btnText }
                                                       );
        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);
        End (runState);
        dlg.Dispose ();
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

        Begin (d);
        ((FakeDriver)Driver!).SetBufferSize (100, 100);

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

        Begin (d);
        ((FakeDriver)Driver!).SetBufferSize (100, 100);

        // Default size is Percent(85) 
        Assert.Equal (new (50, 50), d.Frame.Size);
        d.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void Zero_Buttons_Works ()
    {
        RunState? runState = null;

        var d = (FakeDriver)Driver!;

        var title = "1234";

        var buttonRow = $"{Glyphs.VLine}        {Glyphs.VLine}";
        int width = buttonRow.Length;
        d.SetBufferSize (buttonRow.Length, 3);

        (runState, Dialog dlg) = BeginButtonTestDialog (title, width, Alignment.Center, null);

        DriverAssert.AssertDriverContentsWithFrameAre ($"{buttonRow}", output);

        End (runState);
        dlg.Dispose ();
    }

    private (RunState, Dialog) BeginButtonTestDialog (
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

        RunState runState = Begin (dlg);

        dlg.SetNeedsDraw ();
        dlg.SetNeedsLayout ();
        dlg.Layout ();
        dlg.Draw ();

        return (runState, dlg);
    }

    [Fact]
    [AutoInitShutdown]
    public void Run_Does_Not_Dispose_Dialog ()
    {
        var top = new Toplevel ();

        Dialog dlg = new ();

        dlg.Ready += Dlg_Ready;

        Run (dlg);

#if DEBUG_IDISPOSABLE
        Assert.False (dlg.WasDisposed);
        Assert.False (Top!.WasDisposed);
        Assert.NotEqual (top, Top);
        Assert.Equal (dlg, Top);
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
        Assert.True (Top.WasDisposed);
        Assert.NotNull (Top);
#endif
        Shutdown ();
        Assert.Null (Top);

        return;

        void Dlg_Ready (object? sender, EventArgs e) { RequestStop (); }
    }

    [Fact]
    [AutoInitShutdown]
    public void Can_Access_Cancel_Property_After_Run ()
    {
        Dialog dlg = new ();

        dlg.Ready += Dlg_Ready;

        Run (dlg);

#if DEBUG_IDISPOSABLE
        Assert.False (dlg.WasDisposed);
        Assert.False (Top!.WasDisposed);
        Assert.Equal (dlg, Top);
#endif

        Assert.True (dlg.Canceled);

        // Run it again is possible because it isn't disposed yet
        Run (dlg);

        // Run another view without dispose the prior will throw an assertion
#if DEBUG_IDISPOSABLE
        Dialog dlg2 = new ();
        dlg2.Ready += Dlg_Ready;
        Exception exception = Record.Exception (() => Run (dlg2));
        Assert.NotNull (exception);

        dlg.Dispose ();

        // Now it's possible to tun dlg2 without throw
        Run (dlg2);

        Assert.True (dlg.WasDisposed);
        Assert.False (Top.WasDisposed);
        Assert.Equal (dlg2, Top);
        Assert.False (dlg2.WasDisposed);

        dlg2.Dispose ();

        // Now an assertion will throw accessing the Canceled property
        exception = Record.Exception (() => Assert.True (dlg.Canceled))!;
        Assert.NotNull (exception);
        Assert.True (Top.WasDisposed);
        Shutdown ();
        Assert.True (dlg2.WasDisposed);
        Assert.Null (Top);
#endif

        return;

        void Dlg_Ready (object? sender, EventArgs e)
        {
            ((Dialog)sender!).Canceled = true;
            RequestStop ();
        }
    }


    [Fact]
    [AutoInitShutdown]
    public void Modal_Captures_All_Mouse ()
    {
        Toplevel top = new Toplevel ()
        {
            Id = "top",
        };

        var d = new Dialog
        {
            Width = 10,
            Height = 10,
            X = 1,
            Y = 1
        };

        ((FakeDriver)Driver!).SetBufferSize (20, 20);

        int iterations = 0;
        Iteration += (s, a) =>
                     {
                         if (++iterations > 2)
                         {
                             RequestStop ();
                         }

                         if (iterations == 1)
                         {
                             Application.Run (d);
                             d.Dispose ();
                         }
                         else if (iterations == 2)
                         {
                             // Mouse click outside of dialog
                             Application.RaiseMouseEvent (new MouseEventArgs () { Flags = MouseFlags.Button1Clicked, ScreenPosition = new Point (0, 0) });
                         }


                     };

        top.MouseEvent += (s, e) =>
                          {
                              // This should not be called because the dialog is modal
                              Assert.False (true, "Mouse event should not be captured by the top level when a dialog is modal.");
                          };

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }
}
