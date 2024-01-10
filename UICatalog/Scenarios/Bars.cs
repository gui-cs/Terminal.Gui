using Terminal.Gui;

namespace UICatalog.Scenarios;
[ScenarioMetadata (Name: "Bars", Description: "Illustrates Bar views (e.g. StatusBar)")]
[ScenarioCategory ("Controls")]
public class bars : Scenario {
	public override void Init ()
	{
		Application.Init ();
		ConfigurationManager.Themes.Theme = Theme;
		ConfigurationManager.Apply ();
		Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
		Application.Top.Loaded += Top_Initialized;
	}

	// Setting everything up in Initialized handler because we change the
	// QuitKey and it only sticks if changed after init
	void Top_Initialized (object sender, System.EventArgs e)
	{
		Application.QuitKey = Key.Z.WithCtrl;

		var bar = new Bar () {
		};
		var barITem = new BarItem () { Text = $"Quit - {Application.QuitKey}", AutoSize = true };
		barITem.KeyBindings.Add (Application.QuitKey, KeyBindingScope.Application, Command.QuitToplevel);
		bar.Add (barITem);

		barITem = new BarItem () { Text = $"Show/Hide - {Key.F10}", AutoSize = true };
		barITem.KeyBindings.Add (Key.F10, KeyBindingScope.Application, Command.ToggleExpandCollapse);
		bar.Add (barITem);

		bar.Add (new Label () { Text = "FocusLabel", CanFocus = true });

		var button = new Button ("Press me!") {
			AutoSize = true
		};
		button.Clicked += Button_Clicked;

		bar.Add (button);

		button = new Button ("Or me!") {
			AutoSize = true,
		};
		button.Clicked += Button_Clicked;

		bar.Add (button);

		Application.Top.Add (bar);
	}

	void Button_Clicked (object sender, System.EventArgs e) => MessageBox.Query("Hi", $"You clicked {sender}");
}
