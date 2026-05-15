// Copilot

namespace ApplicationTests;

[Collection ("Application Tests")]
public class StatusLineTests
{
    [Fact]
    public void StatusLine_SetText_WritesOscTitle ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.StatusLine.SetText ("Ready", 2);

        Assert.True (app.StatusLine.IsSupported);
        Assert.Equal ("Ready", app.StatusLine.Text);
        Assert.Contains (EscSeqUtils.OSC_SetWindowTitle ("Ready", 2), app.Driver!.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void StatusLine_SetText_BeforeInit_FlushesAfterInit ()
    {
        using IApplication app = Application.Create ();

        app.StatusLine.SetText ("Pending", 2);
        app.Init (DriverRegistry.Names.ANSI);

        Assert.Contains (EscSeqUtils.OSC_SetWindowTitle ("Pending", 2), app.Driver!.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void StatusLine_SetText_LegacyConsole_DoesNotWriteOsc ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.IsLegacyConsole = true;

        app.StatusLine.SetText ("Legacy", 2);

        Assert.False (app.StatusLine.IsSupported);
        Assert.DoesNotContain (EscSeqUtils.OSC_SetWindowTitle ("Legacy", 2), app.Driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void StatusLine_AppModel_ConstrainsScreenToSingleRow ()
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.StatusLine;
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver!.SetScreenSize (30, 10);

        Assert.Equal (30, app.Screen.Width);
        Assert.Equal (1, app.Screen.Height);
        Assert.Equal (1, app.Driver.GetOutputBuffer ().Rows);
    }

    [Fact]
    public void StatusLine_AppModel_RendersTopRowToOscTitle ()
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.StatusLine;
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);

        using Runnable runnable = new () { Width = Dim.Fill (), Height = 1 };
        Label label = new () { Text = "Status OK", X = 0, Y = 0 };
        runnable.Add (label);

        app.StopAfterFirstIteration = true;
        app.Run (runnable);

        string output = app.Driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_SetWindowTitle ("Status OK", 2), output, StringComparison.Ordinal);
        Assert.DoesNotContain (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll, output, StringComparison.Ordinal);
    }
}
