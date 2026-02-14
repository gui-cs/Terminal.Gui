using Terminal.Gui.Testing;

namespace ViewsTests;

public class MenuBarTests
{
    // Claude - Opus 4.6
    [Fact]
    public void Mouse_Click_Activates_And_Opens ()
    {
        // Arrange - mirror the Menus.cs scenario: MenuBar inside a focusable host view
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        (runnable as View)!.Add (hostView);
        app.Begin (runnable);

        // Focus something else, like in the real scenario
        hostView.SetFocus ();

        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());

        // Act - click on the first MenuBarItem
        MenuBarItem firstItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point itemScreenPos = firstItem.FrameToScreen ().Location;

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (itemScreenPos));

        // Assert
        Assert.True (menuBar.Active, "MenuBar should be Active after click");
        Assert.True (menuBar.IsOpen (), "PopoverMenu should be open after click");

        menuBar.Dispose ();
    }
}
