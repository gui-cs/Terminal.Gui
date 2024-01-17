using System;
using Xunit;
using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace Terminal.Gui.ViewsTests;

public class MenuTests {
	readonly ITestOutputHelper _output;

	public MenuTests (ITestOutputHelper output)
	{
		_output = output;
	}

	// TODO: Create more low-level unit tests for Menu and MenuItem

	[Fact]
	public void Menu_Constructors_Defaults ()
	{
		Assert.Throws<ArgumentNullException> (() => new Menu (null, 0, 0, null));

		var menu = new Menu (new MenuBar (), 0, 0, new MenuBarItem ());
		Assert.Empty (menu.Title);
		Assert.Empty (menu.Text);

	}

	[Fact]
	public void MenuItem_Constructors_Defaults ()
	{
		var menuItem = new MenuItem ();
		Assert.Equal ("", menuItem.Title);
		Assert.Equal ("", menuItem.Help);
		Assert.Null (menuItem.Action);
		Assert.Null (menuItem.CanExecute);
		Assert.Null (menuItem.Parent);
		Assert.Equal (KeyCode.Null, menuItem.Shortcut);

		menuItem = new MenuItem ("Test", "Help", Run, () => { return true; }, new MenuItem (), KeyCode.F1);
		Assert.Equal ("Test", menuItem.Title);
		Assert.Equal ("Help", menuItem.Help);
		Assert.Equal (Run, menuItem.Action);
		Assert.NotNull (menuItem.CanExecute);
		Assert.NotNull (menuItem.Parent);
		Assert.Equal (KeyCode.F1, menuItem.Shortcut);

		void Run () { }
	}
}
