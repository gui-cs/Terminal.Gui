using Xunit.Abstractions;
using static Terminal.Gui.Application;

namespace Terminal.Gui.DialogTests;

public class DialogTests
{
    private readonly ITestOutputHelper _output;
    public DialogTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void Add_Button_Works ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;

        var title = "1234";
        var btn1Text = "yes";
        var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

        // We test with one button first, but do this to get the width right for 2
        int width = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}".Length;
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
            Buttons = [new() { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border.Thickness = new (1, 0, 1, 0);
        runstate = Begin (dlg);
        var buttonRow = $"{CM.Glyphs.VLine}    {btn1}     {CM.Glyphs.VLine}";
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

        // Now add a second button
        buttonRow = $"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });
        var first = false;
        RunIteration (ref runstate, ref first);
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        dlg = new ()
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.Fill,
            Buttons = [new() { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border.Thickness = new (1, 0, 1, 0);
        runstate = Begin (dlg);
        buttonRow = $"{CM.Glyphs.VLine}{btn1}         {CM.Glyphs.VLine}";
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

        // Now add a second button
        buttonRow = $"{CM.Glyphs.VLine}{btn1}   {btn2}{CM.Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });
        first = false;
        RunIteration (ref runstate, ref first);
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        dlg = new ()
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.End,
            Buttons = [new() { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border.Thickness = new (1, 0, 1, 0);
        runstate = Begin (dlg);
        buttonRow = $"{CM.Glyphs.VLine}{new (' ', width - btn1.Length - 2)}{btn1}{CM.Glyphs.VLine}";
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

        // Now add a second button
        buttonRow = $"{CM.Glyphs.VLine}  {btn1} {btn2}{CM.Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });
        first = false;
        RunIteration (ref runstate, ref first);
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        dlg = new ()
        {
            Title = title,
            Width = width,
            Height = 1,
            ButtonAlignment = Alignment.Start,
            Buttons = [new() { Text = btn1Text }]
        };

        // Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
        dlg.Border.Thickness = new (1, 0, 1, 0);
        runstate = Begin (dlg);
        buttonRow = $"{CM.Glyphs.VLine}{btn1}{new (' ', width - btn1.Length - 2)}{CM.Glyphs.VLine}";
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

        // Now add a second button
        buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2}  {CM.Glyphs.VLine}";
        dlg.AddButton (new () { Text = btn2Text });
        first = false;
        RunIteration (ref runstate, ref first);
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ]|"
        var btn1Text = "yes";
        var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
        var btn3Text = "maybe";
        var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
        var btn4Text = "never";
        var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

        var buttonRow = $"{CM.Glyphs.VLine} {btn1} {btn2} {btn3} {btn4} {CM.Glyphs.VLine}";
        int width = buttonRow.Length;
        d.SetBufferSize (buttonRow.Length, 3);

        // Default - Center
        (runstate, Dialog dlg) = RunButtonTestDialog (
                                                      title,
                                                      width,
                                                      Alignment.Center,
                                                      new Button { Text = btn1Text },
                                                      new Button { Text = btn2Text },
                                                      new Button { Text = btn3Text },
                                                      new Button { Text = btn4Text }
                                                     );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{CM.Glyphs.VLine}{btn1}  {btn2}  {btn3} {btn4}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Fill,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        buttonRow = $"{CM.Glyphs.VLine}  {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.End,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}  {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Start,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_On_Too_Small_Width ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ][ never ]|"
        var btn1Text = "yes";
        var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
        var btn3Text = "maybe";
        var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
        var btn4Text = "never";
        var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";
        var buttonRow = string.Empty;

        var width = 30;
        d.SetBufferSize (width, 1);

        // Default - Center
        buttonRow =
            $"{CM.Glyphs.VLine} yes {CM.Glyphs.RightBracket}{btn2}{btn3}{CM.Glyphs.LeftBracket} never{CM.Glyphs.VLine}";

        (runstate, Dialog dlg) = RunButtonTestDialog (
                                                      title,
                                                      width,
                                                      Alignment.Center,
                                                      new Button { Text = btn1Text },
                                                      new Button { Text = btn2Text },
                                                      new Button { Text = btn3Text },
                                                      new Button { Text = btn4Text }
                                                     );
        Assert.Equal (new (width, 1), dlg.Frame.Size);
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        buttonRow =
            $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} yes {CM.Glyphs.LeftBracket} no {CM.Glyphs.LeftBracket} maybe {CM.Glyphs.LeftBracket} never {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Fill,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        buttonRow = $"{CM.Glyphs.VLine}es {CM.Glyphs.RightBracket}{btn2}{btn3}{btn4}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.End,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        buttonRow = $"{CM.Glyphs.VLine}{btn1}{btn2}{btn3}{CM.Glyphs.LeftBracket} neve{CM.Glyphs.VLine}";

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Start,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_WideOdd ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ]|"
        var btn1Text = "really long button 1";
        var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
        var btn2Text = "really long button 2";
        var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
        var btn3Text = "really long button 3";
        var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
        var btn4Text = "really long button 44"; // 44 is intentional to make length different than rest
        var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

        // Note extra spaces to make dialog even wider
        //                         123456                          1234567
        var buttonRow = $"{CM.Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {CM.Glyphs.VLine}";
        int width = buttonRow.Length;
        d.SetBufferSize (buttonRow.Length, 1);

        // Default - Center
        (runstate, Dialog dlg) = RunButtonTestDialog (
                                                      title,
                                                      width,
                                                      Alignment.Center,
                                                      new Button { Text = btn1Text },
                                                      new Button { Text = btn2Text },
                                                      new Button { Text = btn3Text },
                                                      new Button { Text = btn4Text }
                                                     );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{CM.Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Fill,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        buttonRow = $"{CM.Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.End,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Start,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Four_Wider ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ]|"
        var btn1Text = "yes";
        var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
        var btn3Text = "你你你你你"; // This is a wide char
        var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";

        // Requires a Nerd Font
        var btn4Text = "\uE36E\uE36F\uE370\uE371\uE372\uE373";
        var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

        // Note extra spaces to make dialog even wider
        //                         123456                           123456
        var buttonRow = $"{CM.Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {CM.Glyphs.VLine}";
        int width = buttonRow.GetColumns ();
        d.SetBufferSize (width, 3);

        // Default - Center
        (runstate, Dialog dlg) = RunButtonTestDialog (
                                                      title,
                                                      width,
                                                      Alignment.Center,
                                                      new Button { Text = btn1Text },
                                                      new Button { Text = btn2Text },
                                                      new Button { Text = btn3Text },
                                                      new Button { Text = btn4Text }
                                                     );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        buttonRow = $"{CM.Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Fill,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        buttonRow = $"{CM.Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.End,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.GetColumns ());

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Start,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text },
                                               new Button { Text = btn4Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_One ()
    {
        var d = (FakeDriver)Driver;
        RunState runstate = null;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ ok ]|"
        var btnText = "ok";

        var buttonRow =
            $"{CM.Glyphs.VLine}  {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}  {CM.Glyphs.VLine}";
        int width = buttonRow.Length;

        d.SetBufferSize (width, 1);

        (runstate, Dialog dlg) = RunButtonTestDialog (
                                                      title,
                                                      width,
                                                      Alignment.Center,
                                                      new Button { Text = btnText }
                                                     );

        // Center
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify 
        buttonRow =
            $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}    {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Fill,
                                               new Button { Text = btnText }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        buttonRow =
            $"{CM.Glyphs.VLine}    {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.End,
                                               new Button { Text = btnText }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        buttonRow =
            $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}    {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Start,
                                               new Button { Text = btnText }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Wider
        buttonRow =
            $"{CM.Glyphs.VLine}   {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}   {CM.Glyphs.VLine}";
        width = buttonRow.Length;

        d.SetBufferSize (width, 1);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Center,
                                               new Button { Text = btnText }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        buttonRow =
            $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}      {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Fill,
                                               new Button { Text = btnText }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        buttonRow =
            $"{CM.Glyphs.VLine}      {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.End,
                                               new Button { Text = btnText }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        buttonRow =
            $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}      {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Start,
                                               new Button { Text = btnText }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Three ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ][ maybe ]|"
        var btn1Text = "yes";
        var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
        var btn3Text = "maybe";
        var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";

        var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {btn3} {CM.Glyphs.VLine}";
        int width = buttonRow.Length;

        d.SetBufferSize (buttonRow.Length, 3);

        (runstate, Dialog dlg) = RunButtonTestDialog (
                                                      title,
                                                      width,
                                                      Alignment.Center,
                                                      new Button { Text = btn1Text },
                                                      new Button { Text = btn2Text },
                                                      new Button { Text = btn3Text }
                                                     );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        buttonRow = $@"{CM.Glyphs.VLine}{btn1}  {btn2}  {btn3}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Fill,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        buttonRow = $@"{CM.Glyphs.VLine}  {btn1} {btn2} {btn3}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.End,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        buttonRow = $@"{CM.Glyphs.VLine}{btn1} {btn2} {btn3}  {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Start,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text },
                                               new Button { Text = btn3Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Two ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ]|"
        var btn1Text = "yes";
        var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

        var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
        int width = buttonRow.Length;

        d.SetBufferSize (buttonRow.Length, 3);

        (runstate, Dialog dlg) = RunButtonTestDialog (
                                                      title,
                                                      width,
                                                      Alignment.Center,
                                                      new Button { Text = btn1Text },
                                                      new Button { Text = btn2Text }
                                                     );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        buttonRow = $@"{CM.Glyphs.VLine}{btn1}   {btn2}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Fill,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        buttonRow = $@"{CM.Glyphs.VLine}  {btn1} {btn2}{CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.End,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        buttonRow = $@"{CM.Glyphs.VLine}{btn1} {btn2}  {CM.Glyphs.VLine}";
        Assert.Equal (width, buttonRow.Length);

        (runstate, dlg) = RunButtonTestDialog (
                                               title,
                                               width,
                                               Alignment.Start,
                                               new Button { Text = btn1Text },
                                               new Button { Text = btn2Text }
                                              );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ButtonAlignment_Two_Hidden ()
    {
        RunState runstate = null;
        var firstIteration = false;

        var d = (FakeDriver)Driver;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var title = "1234";

        // E.g "|[ yes ][ no ]|"
        var btn1Text = "yes";
        var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
        var btn2Text = "no";
        var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

        var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
        int width = buttonRow.Length;

        d.SetBufferSize (buttonRow.Length, 3);

        Dialog dlg = null;
        Button button1, button2;

        // Default (Center)
        button1 = new() { Text = btn1Text };
        button2 = new() { Text = btn2Text };
        (runstate, dlg) = RunButtonTestDialog (title, width, Alignment.Center, button1, button2);
        button1.Visible = false;
        RunIteration (ref runstate, ref firstIteration);
        buttonRow = $@"{CM.Glyphs.VLine}         {btn2} {CM.Glyphs.VLine}";
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Justify
        Assert.Equal (width, buttonRow.Length);
        button1 = new() { Text = btn1Text };
        button2 = new() { Text = btn2Text };
        (runstate, dlg) = RunButtonTestDialog (title, width, Alignment.Fill, button1, button2);
        button1.Visible = false;
        RunIteration (ref runstate, ref firstIteration);
        buttonRow = $@"{CM.Glyphs.VLine}          {btn2}{CM.Glyphs.VLine}";
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Right
        Assert.Equal (width, buttonRow.Length);
        button1 = new() { Text = btn1Text };
        button2 = new() { Text = btn2Text };
        (runstate, dlg) = RunButtonTestDialog (title, width, Alignment.End, button1, button2);
        button1.Visible = false;
        RunIteration (ref runstate, ref firstIteration);
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();

        // Left
        Assert.Equal (width, buttonRow.Length);
        button1 = new() { Text = btn1Text };
        button2 = new() { Text = btn2Text };
        (runstate, dlg) = RunButtonTestDialog (title, width, Alignment.Start, button1, button2);
        button1.Visible = false;
        RunIteration (ref runstate, ref firstIteration);
        buttonRow = $@"{CM.Glyphs.VLine}        {btn2}  {CM.Glyphs.VLine}";
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
        dlg.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Dialog_In_Window_With_Size_One_Button_Aligns ()
    {
        ((FakeDriver)Driver).SetBufferSize (20, 5);

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
        var btn = $"{CM.Glyphs.LeftBracket} Ok {CM.Glyphs.RightBracket}";

        win.Loaded += (s, a) =>
                      {
                          var dlg = new Dialog { Width = 18, Height = 3, Buttons = [new () { Text = "Ok" }] };

                          dlg.Loaded += (s, a) =>
                                        {
                                            Refresh ();

                                            var expected = @$"
┌──────────────────┐
│┌────────────────┐│
││     {btn}     ││
│└────────────────┘│
└──────────────────┘";
                                            _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
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
        ((FakeDriver)Driver).SetBufferSize (20, height);
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
                             // BUGBUG: This seems wrong; is it a bug in Dim.Percent(85)?? No
                             _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
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
        ((FakeDriver)Driver).SetBufferSize (30, 10);

        // Override CM
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var btn1 = new Button { Text = "press me 1" };
        Button btn2 = null;
        Button btn3 = null;
        string expected = null;

        btn1.Accept += (s, e) =>
                       {
                           btn2 = new () { Text = "Show Sub" };
                           btn3 = new () { Text = "Close" };
                           btn3.Accept += (s, e) => RequestStop ();

                           btn2.Accept += (s, e) =>
                                          {
                                              // Don't test MessageBox in Dialog unit tests!
                                              var subBtn = new Button { Text = "Ok", IsDefault = true };
                                              var subDlg = new Dialog { Text = "ya", Width = 20, Height = 5, Buttons = [subBtn] };
                                              subBtn.Accept += (s, e) => RequestStop (subDlg);
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
            $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Ok {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

        int iterations = -1;

        Iteration += (s, a) =>
                     {
                         iterations++;

                         if (iterations == 0)
                         {
                             Assert.True (btn1.NewKeyDownEvent (Key.Space));
                         }
                         else if (iterations == 1)
                         {
                             expected = @$"
  ┌───────────────────────┐
  │                       │
  │                       │
  │                       │
  │                       │
  │                       │
  │{CM.Glyphs.LeftBracket} Show Sub {CM.Glyphs.RightBracket} {CM.Glyphs.LeftBracket} Close {CM.Glyphs.RightBracket} │
  └───────────────────────┘";
                             TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

                             Assert.True (btn2.NewKeyDownEvent (Key.Space));
                         }
                         else if (iterations == 2)
                         {
                             TestHelpers.AssertDriverContentsWithFrameAre (
                                                                           @$"
  ┌───────────────────────┐
  │  ┌──────────────────┐ │
  │  │ya                │ │
  │  │                  │ │
  │  │     {btn}     │ │
  │  └──────────────────┘ │
  │{CM.Glyphs.LeftBracket} Show Sub {CM.Glyphs.RightBracket} {CM.Glyphs.LeftBracket} Close {CM.Glyphs.RightBracket} │
  └───────────────────────┘",
                                                                           _output
                                                                          );

                             Assert.True (Top.NewKeyDownEvent (Key.Enter));
                         }
                         else if (iterations == 3)
                         {
                             TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

                             Assert.True (btn3.NewKeyDownEvent (Key.Space));
                         }
                         else if (iterations == 4)
                         {
                             TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

                             RequestStop ();
                         }
                     };

        Run ().Dispose ();
        Shutdown ();

        Assert.Equal (4, iterations);
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
        ((FakeDriver)Driver).SetBufferSize (100, 100);

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
        ((FakeDriver)Driver).SetBufferSize (100, 100);

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
        ((FakeDriver)Driver).SetBufferSize (20, 10);

        // Default location is centered, so 100 / 2 - 85 / 2 = 7
        Assert.Equal (new (expected, expected), d.Frame.Location);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌───┐
     │   │
     │   │
     │   │
     └───┘",
                                                      _output
                                                     );
        d.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Location_When_Not_Application_Top_Not_Default ()
    {
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Double;

        int iterations = -1;

        // Override CM
        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Iteration += (s, a) =>
                     {
                         iterations++;

                         if (iterations == 0)
                         {
                             var d = new Dialog { X = 5, Y = 5, Height = 3, Width = 5 };
                             RunState rs = Begin (d);

                             Assert.Equal (new (5, 5), d.Frame.Location);

                             TestHelpers.AssertDriverContentsWithFrameAre (
                                                                           @"
╔══════════════════╗
║                  ║
║                  ║
║                  ║
║                  ║
║    ┌───┐         ║
║    │   │         ║
║    └───┘         ║
║                  ║
╚══════════════════╝",
                                                                           _output
                                                                          );
                             End (rs);
                             d.Dispose ();

                             d = new ()
                             {
                                 X = 5, Y = 5,
                                 Width = Dim.Percent (85),
                                 Height = Dim.Percent (85)
                             };
                             rs = Begin (d);

                             // This is because of PostionTopLevels and EnsureVisibleBounds
                             Assert.Equal (new (5, 5), d.Frame.Location);

                             // #3127: Before					
                             Assert.Equal (new (17, 8), d.Frame.Size);

                             TestHelpers.AssertDriverContentsWithFrameAre (
                                                                           @"
╔══════════════════╗
║                  ║
║                  ║
║                  ║
║                  ║
║    ┌──────────────
║    │              
║    │              
║    │              
╚════│              ",
                                                                           _output);

//                             // #3127: After: Because Toplevel is now Width/Height = Dim.Filll
//                             Assert.Equal (new (15, 6), d.Frame.Size);

//                             TestHelpers.AssertDriverContentsWithFrameAre (
//                                                                           @"
//╔══════════════════╗
//║                  ║
//║  ┌─────────────┐ ║
//║  │             │ ║
//║  │             │ ║
//║  │             │ ║
//║  │             │ ║
//║  └─────────────┘ ║
//║                  ║
//╚══════════════════╝",
//                                                                           _output
//                                                                          );
                             End (rs);
                             d.Dispose ();
                         }
                         else if (iterations > 0)
                         {
                             RequestStop ();
                         }
                     };

        ((FakeDriver)Driver).SetBufferSize (20, 10);
        Run (top);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void One_Button_Works ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;

        Button.DefaultShadow = ShadowStyle.None;

        var title = "";
        var btnText = "ok";

        var buttonRow =
            $"{CM.Glyphs.VLine}   {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}   {CM.Glyphs.VLine}";

        int width = buttonRow.Length;
        d.SetBufferSize (buttonRow.Length, 10);

        (runstate, Dialog dlg) = RunButtonTestDialog (
                                                      title,
                                                      width,
                                                      Alignment.Center,
                                                      new Button { Text = btnText }
                                                     );
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
        End (runstate);
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
        ((FakeDriver)Driver).SetBufferSize (100, 100);

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
        ((FakeDriver)Driver).SetBufferSize (100, 100);

        // Default size is Percent(85) 
        Assert.Equal (new (50, 50), d.Frame.Size);
        d.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void Zero_Buttons_Works ()
    {
        RunState runstate = null;

        var d = (FakeDriver)Driver;

        var title = "1234";

        var buttonRow = $"{CM.Glyphs.VLine}        {CM.Glyphs.VLine}";
        int width = buttonRow.Length;
        d.SetBufferSize (buttonRow.Length, 3);

        (runstate, Dialog dlg) = RunButtonTestDialog (title, width, Alignment.Center, null);
        TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

        End (runstate);
        dlg.Dispose ();
    }

    private (RunState, Dialog) RunButtonTestDialog (
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
        dlg.Border.Thickness = new (1, 0, 1, 0);

        return (Begin (dlg), dlg);
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
        Assert.False (Top.WasDisposed);
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

        void Dlg_Ready (object sender, EventArgs e) { RequestStop (); }
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
        Assert.False (Top.WasDisposed);
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
        exception = Record.Exception (() => Assert.True (dlg.Canceled));
        Assert.NotNull (exception);
        Assert.True (Top.WasDisposed);
        Shutdown ();
        Assert.True (dlg2.WasDisposed);
        Assert.Null (Top);
#endif

        return;

        void Dlg_Ready (object sender, EventArgs e)
        {
            ((Dialog)sender).Canceled = true;
            RequestStop ();
        }
    }
}
