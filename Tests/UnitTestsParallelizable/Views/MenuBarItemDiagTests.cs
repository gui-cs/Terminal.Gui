// Temporary diagnostic test to understand MenuBarItem failures
using Terminal.Gui.Tests;
using Terminal.Gui.Tracing;
using Xunit.Abstractions;

namespace ViewsTests;

public class MenuBarItemDiagTests
{
    private readonly ITestOutputHelper _output;
    public MenuBarItemDiagTests (ITestOutputHelper output) => _output = output;

    [Fact]
    public void Diag_Command_Activate_Activates_PopoverMenu ()
    {
        using (TestLogging.Verbose (_output, TraceCategory.Command))
        {
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

            MenuBarItem? menuBarItem = new ();
            menuBarItem.EnableForDesign ();
            hostView.Add (menuBarItem);

            ((View)runnable).Add (hostView);
            app.Begin (runnable);

            _output.WriteLine ($"=== Before Activate ===");
            _output.WriteLine ($"PopoverMenuOpen: {menuBarItem.PopoverMenuOpen}");
            _output.WriteLine ($"IsRegistered: {app.Popovers?.IsRegistered (menuBarItem.PopoverMenu)}");
            _output.WriteLine ($"PopoverMenu.App: {menuBarItem.PopoverMenu?.App}");
            _output.WriteLine ($"PopoverMenu.IsInitialized: {menuBarItem.PopoverMenu?.IsInitialized}");

            menuBarItem.InvokeCommand (Command.Activate);

            _output.WriteLine ($"=== After Activate ===");
            _output.WriteLine ($"PopoverMenuOpen: {menuBarItem.PopoverMenuOpen}");
            _output.WriteLine ($"IsRegistered: {app.Popovers?.IsRegistered (menuBarItem.PopoverMenu)}");

            Assert.True (menuBarItem.PopoverMenuOpen, "PopoverMenuOpen should be true");
            Assert.True (menuBarItem.PopoverMenu?.Visible, "PopoverMenu.Visible should be true");
        }
    }

    [Fact]
    public void Diag_PopoverMenu_Is_Registered_By_Init ()
    {
        using (TestLogging.Verbose (_output, TraceCategory.Command))
        {
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

            MenuBarItem? menuBarItem = new ();
            menuBarItem.EnableForDesign ();
            hostView.Add (menuBarItem);

            ((View)runnable).Add (hostView);

            _output.WriteLine ($"=== Before Begin ===");
            _output.WriteLine ($"MenuBarItem.App: {menuBarItem.App}");
            _output.WriteLine ($"PopoverMenu.App: {menuBarItem.PopoverMenu?.App}");
            _output.WriteLine ($"PopoverMenu.IsInitialized: {menuBarItem.PopoverMenu?.IsInitialized}");
            _output.WriteLine ($"IsRegistered: {app.Popovers?.IsRegistered (menuBarItem.PopoverMenu)}");

            app.Begin (runnable);

            _output.WriteLine ($"=== After Begin ===");
            _output.WriteLine ($"MenuBarItem.App: {menuBarItem.App}");
            _output.WriteLine ($"PopoverMenu.App: {menuBarItem.PopoverMenu?.App}");
            _output.WriteLine ($"PopoverMenu.IsInitialized: {menuBarItem.PopoverMenu?.IsInitialized}");
            _output.WriteLine ($"IsRegistered: {app.Popovers?.IsRegistered (menuBarItem.PopoverMenu)}");

            Assert.True (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));
        }
    }

    [Fact]
    public void Diag_Command_Activate_Focuses ()
    {
        using (TestLogging.Verbose (_output, TraceCategory.Command))
        {
            VirtualTimeProvider time = new ();
            using IApplication app = Application.Create (time);
            app.Init (DriverRegistry.Names.ANSI);
            IRunnable runnable = new Runnable ();

            View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

            MenuBar menuBar = new () { Id = "menuBar" };
            menuBar.EnableForDesign (ref hostView);
            hostView.Add (menuBar);

            ((View)runnable).Add (hostView);
            app.Begin (runnable);

            MenuBarItem menuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
            PopoverMenu? popoverMenu = menuBarItem.PopoverMenu;
            Menu? menu = popoverMenu?.Root;
            MenuItem? menuItem = menu?.SubViews.OfType<MenuItem> ().First ();

            _output.WriteLine ($"=== Before Activate ===");

            menuBar.InvokeCommand (Command.Activate);

            _output.WriteLine ($"=== After Activate ===");
            _output.WriteLine ($"menuBar.HasFocus: {menuBar.HasFocus}");
            _output.WriteLine ($"menuBarItem.HasFocus: {menuBarItem.HasFocus}");
            _output.WriteLine ($"PopoverMenuOpen: {menuBarItem.PopoverMenuOpen}");
            _output.WriteLine ($"popoverMenu.HasFocus: {popoverMenu?.HasFocus}");
            _output.WriteLine ($"popoverMenu.Visible: {popoverMenu?.Visible}");
            _output.WriteLine ($"popoverMenu.IsOpen: {popoverMenu?.IsOpen}");
            _output.WriteLine ($"menu: {menu?.ToIdentifyingString ()}");
            _output.WriteLine ($"menu.HasFocus: {menu?.HasFocus}");
            _output.WriteLine ($"menu.Visible: {menu?.Visible}");
            _output.WriteLine ($"menuItem: {menuItem?.ToIdentifyingString ()}");
            _output.WriteLine ($"menuItem.HasFocus: {menuItem?.HasFocus}");
            _output.WriteLine ($"Focused: {app.Navigation?.GetFocused ()?.ToIdentifyingString ()}");

            // Same assertions as the original test (line 121-129)
            Assert.True (menuBar.HasFocus, "line 121: menuBar.HasFocus");
            Assert.True (menuBarItem.HasFocus, "line 122: menuBarItem.HasFocus");
            Assert.True (popoverMenu?.HasFocus, "line 123: popoverMenu.HasFocus");
            Assert.True (menu?.HasFocus, "line 124: menu.HasFocus");
            Assert.True (menuItem?.HasFocus, "line 125: menuItem.HasFocus");
            Assert.Equal (menu, popoverMenu?.Focused);
            Assert.Equal (menuItem, menu?.Focused);
            Assert.Equal (menuItem, app.Navigation?.GetFocused ());

            menuBar.Dispose ();
        }
    }

    [Fact]
    public void Diag_PopoverMenu_Is_Registered_By_Set ()
    {
        using (TestLogging.Verbose (_output, TraceCategory.Command))
        {
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

            MenuBarItem? menuBarItem = new ();

            ((View)runnable).Add (hostView);
            app.Begin (runnable);

            _output.WriteLine ($"=== Before hostView.Add ===");
            _output.WriteLine ($"IsRegistered: {app.Popovers?.IsRegistered (menuBarItem.PopoverMenu)}");

            hostView.Add (menuBarItem);

            _output.WriteLine ($"=== After hostView.Add, Before EnableForDesign ===");
            _output.WriteLine ($"IsRegistered: {app.Popovers?.IsRegistered (menuBarItem.PopoverMenu)}");
            _output.WriteLine ($"MenuBarItem.App: {menuBarItem.App}");

            menuBarItem.EnableForDesign ();

            _output.WriteLine ($"=== After EnableForDesign ===");
            _output.WriteLine ($"IsRegistered: {app.Popovers?.IsRegistered (menuBarItem.PopoverMenu)}");
            _output.WriteLine ($"PopoverMenu.App: {menuBarItem.PopoverMenu?.App}");

            Assert.True (app.Popovers?.IsRegistered (menuBarItem.PopoverMenu));
        }
    }
}
