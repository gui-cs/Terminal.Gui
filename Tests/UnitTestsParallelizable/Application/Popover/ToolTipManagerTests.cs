using System.ComponentModel;
using JetBrains.Annotations;

namespace ApplicationTests.Popover;

/// <summary>
/// Contains unit tests for the ToolTipManager class.
/// </summary>
[TestSubject (typeof (ToolTipManager))]
public class ToolTipManagerTests
{
    [Fact]
    public void SetToolTip_NullTarget_ThrowsArgumentNullException ()
    {
        ToolTipManager manager = ToolTipManager.Instance;
        ToolTipProvider provider = new (() => new Label () { Text = "Test" });
        
        Assert.Throws<ArgumentNullException> (() => manager.SetToolTip (null!, provider));
    }

    [Fact]
    public void SetToolTip_NullProvider_ThrowsArgumentNullException ()
    {
        ToolTipManager manager = ToolTipManager.Instance;
        View view = new ();
        
        Assert.Throws<ArgumentNullException> (() => manager.SetToolTip (view, null!));
    }

    [Fact]
    public void SetToolTip_ValidParameters_SetsRemovesToolTip ()
    {
        ToolTipManager manager = ToolTipManager.Instance;
        View view = new ();
        ToolTipProvider provider = new (() => new Label () { Text = "Test" });
        
        manager.SetToolTip (view, provider);
        
        Assert.True (manager.Registrations.ContainsKey (view));
        
        manager.RemoveToolTip (view);
        
        Assert.False (manager.Registrations.ContainsKey (view));
    }

    [Fact]
    public void SetToolTipExtension1_SetsToolTip ()
    {
        View view = new ();
        
        view.SetToolTip("Test");
        
        Assert.True (ToolTipManager.Instance.Registrations.ContainsKey (view));
        
        view.RemoveToolTip ();
        
        Assert.False (ToolTipManager.Instance.Registrations.ContainsKey (view));
    }

    [Fact]
    public void SetToolTipExtension2_SetsToolTip ()
    {
        View view = new ();
        
        view.SetToolTip (() => "Test");
        
        Assert.True (ToolTipManager.Instance.Registrations.ContainsKey (view));
        
        view.RemoveToolTip ();
        
        Assert.False (ToolTipManager.Instance.Registrations.ContainsKey (view));
    }

    [Fact]
    public void SetToolTipExtension3_SetsToolTip ()
    {
        View view = new ();
        
        view.SetToolTip (() => new Label () { Text = "Test" });
        
        Assert.True (ToolTipManager.Instance.Registrations.ContainsKey (view));
        
        view.RemoveToolTip ();
        
        Assert.False (ToolTipManager.Instance.Registrations.ContainsKey (view));
    }

    [Fact]
    public void EnterView_ShowsHidesToolTip()
    {
        using IApplication app = Application.Create ().Init ();

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        View view = new ();
        window.Add (view);
        View toolTipContent = new ();
        
        view.SetToolTip (() => toolTipContent);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        Assert.False (toolTipContent.Visible);
        // Simulate mouse enter event
        CancelEventArgs eventArgs = new ();
        _ = view.NewMouseEnterEvent (eventArgs);
        
        Assert.True (toolTipContent.Visible);
        // Simulate mouse leave event
        view.NewMouseLeaveEvent ();
        
        Assert.False (toolTipContent.Visible);

        window.Dispose ();
    }
}