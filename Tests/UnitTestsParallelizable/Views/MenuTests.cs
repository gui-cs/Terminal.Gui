namespace ViewsTests;

public class MenuTests
{
    // Claude - Opus 4.6
    [Fact]
    public void Constructors_Defaults ()
    {
        Menu menu = new ();

        Assert.Empty (menu.Title);
        Assert.Empty (menu.Text);
        Assert.Equal (Orientation.Vertical, menu.Orientation);
        Assert.IsType<DimAuto> (menu.Width);
        Assert.IsType<DimAuto> (menu.Height);
        Assert.Equal (Menu.DefaultBorderStyle, menu.BorderStyle);
        Assert.Contains (Command.Accept, menu.CommandsToBubbleUp);
        Assert.Contains (Command.Activate, menu.CommandsToBubbleUp);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Add_MenuItem_Sets_CanFocus ()
    {
        Menu menu = new ();
        MenuItem item = new () { Title = "Item1" };

        // Before adding, CanFocus is default (false for Shortcut-derived)
        menu.Add (item);

        Assert.True (item.CanFocus);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Add_Line_Grows_To_Fill ()
    {
        Menu menu = new ();
        Line line = new ();
        menu.Add (line);

        // After adding a Line, X and Width should be set for auto-join
        Assert.IsType<PosFunc> (line.X);
        Assert.NotNull (line.Width);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Accept_Bubbles_From_MenuItem ()
    {
        Menu menu = new ();
        MenuItem item = new () { Title = "Item1" };
        menu.Add (item);

        var menuAcceptingFired = false;

        menu.Accepting += (_, _) => { menuAcceptingFired = true; };

        // Accept on the MenuItem should bubble to the Menu via the Accepting handler in OnSubViewAdded
        item.InvokeCommand (Command.Accept);

        Assert.True (menuAcceptingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Activate_Bubbles_From_MenuItem ()
    {
        Menu menu = new ();
        MenuItem item = new () { Title = "Item1" };
        menu.Add (item);

        var menuActivatingFired = false;

        menu.Activating += (_, _) => { menuActivatingFired = true; };

        // Activate on the MenuItem should bubble via CommandsToBubbleUp
        item.InvokeCommand (Command.Activate);

        Assert.True (menuActivatingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_FocusChange_Updates_SelectedMenuItem ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        Menu menu = new ();
        MenuItem item1 = new () { Title = "Item1" };
        MenuItem item2 = new () { Title = "Item2" };
        menu.Add (item1);
        menu.Add (item2);

        IRunnable runnable = new Runnable ();
        (runnable as View)?.Add (menu);
        app.Begin (runnable);

        item2.SetFocus ();

        Assert.Equal (item2, menu.SelectedMenuItem);

        (runnable as View)?.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_FocusChange_Raises_SelectedMenuItemChanged ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        Menu menu = new ();
        MenuItem item1 = new () { Title = "Item1" };
        MenuItem item2 = new () { Title = "Item2" };
        menu.Add (item1);
        menu.Add (item2);

        IRunnable runnable = new Runnable ();
        (runnable as View)?.Add (menu);
        app.Begin (runnable);

        MenuItem? changedItem = null;

        menu.SelectedMenuItemChanged += (_, selected) => { changedItem = selected; };

        item2.SetFocus ();

        Assert.Equal (item2, changedItem);

        (runnable as View)?.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_OnVisibleChanged_Selects_First_Item ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        Menu menu = new ();
        MenuItem item1 = new () { Title = "Item1" };
        MenuItem item2 = new () { Title = "Item2" };
        menu.Add (item1);
        menu.Add (item2);

        IRunnable runnable = new Runnable ();
        (runnable as View)?.Add (menu);
        app.Begin (runnable);

        // Set Visible to false, then back to true
        menu.Visible = false;
        menu.Visible = true;

        // OnVisibleChanged should select the first MenuItem
        Assert.Equal (item1, menu.SelectedMenuItem);

        (runnable as View)?.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_SuperMenuItem_Set_Correctly ()
    {
        Menu subMenu = new ();
        MenuItem subItem = new () { Title = "SubItem" };
        subMenu.Add (subItem);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };

        Assert.Equal (parentItem, subMenu.SuperMenuItem);

        parentItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Multiple_MenuItems_Accept_Both_Work ()
    {
        Menu menu = new ();

        MenuItem item1 = new () { Title = "Item1" };
        MenuItem item2 = new () { Title = "Item2" };

        menu.Add (item1);
        menu.Add (item2);

        var acceptCount = 0;
        object? lastSender = null;

        // When a MenuItem is inside a Menu, Accept bubbles via the Accepting handler
        // in Menu.OnSubViewAdded which calls Menu.RaiseAccepted.
        // Subscribe to the Menu's Accepting event to verify both items trigger it independently.
        menu.Accepting += (sender, _) =>
                          {
                              acceptCount++;
                              lastSender = sender;
                          };

        // Accept item1
        item1.InvokeCommand (Command.Accept);
        Assert.Equal (1, acceptCount);

        // Accept item2
        item2.InvokeCommand (Command.Accept);
        Assert.Equal (2, acceptCount);

        menu.Dispose ();
    }
}
