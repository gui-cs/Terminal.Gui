using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Generic", "Generic sample - A template for creating new Scenarios")]
[ScenarioCategory ("Controls")]
public sealed class MyScenario : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"
        };

        int leftMargin = 0;
        var just = Justification.Centered;

        var button = new Button { X = Pos.Justify(just), Y = Pos.Center (), Text = "Press me!" };
        //button.Margin.Thickness = new Thickness (leftMargin, 0, 0, 0);
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed the button!", "Ok");
        appWindow.Add (button);

        button = new Button { X = Pos.Justify (just), Y = Pos.Center (), Text = "Two" };
        button.Margin.Thickness = new Thickness (leftMargin, 0, 0, 0);
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Two!", "Ok");
        appWindow.Add (button);

        button = new Button { X = Pos.Justify (just), Y = Pos.Center (), Text = "Three" };
        button.Margin.Thickness = new Thickness (leftMargin, 0, 0, 0);
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (button);

        button = new Button { X = Pos.Justify (just), Y = Pos.Center (), Text = "Four" };
        button.Margin.Thickness = new Thickness (leftMargin, 0, 0, 0);
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (button);

        button = new Button { X = Pos.Justify (just), Y = Pos.Center (), Text = "Five" };
        button.Margin.Thickness = new Thickness (leftMargin, 0, 0, 0);
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (button);

        button = new Button { X = Pos.Justify (just), Y = Pos.Center (), Text = "Six" };
        button.Margin.Thickness = new Thickness (leftMargin, 0, 0, 0);
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (button);

        button = new Button { X = Pos.Justify (just), Y = Pos.Center (), Text = "Seven" };
        button.Margin.Thickness = new Thickness (leftMargin, 0, 0, 0);
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (button);

        button = new Button { X = Pos.Justify (just), Y = Pos.Center (), Text = "Eight" };
        button.Margin.Thickness = new Thickness (leftMargin, 0, 0, 0);
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (button);

        just = Justification.FirstLeftRestRight;
        var checkbox = new CheckBox { X = 5, Y = Pos.Justify (just), Text = "Check boxes!" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed the checkbox!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (just), Text = "CheckTwo" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Two!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (just), Text = "CheckThree" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (just), Text = "CheckFour" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (just), Text = "CheckFive" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (just), Text = "CheckSix" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (just), Text = "CheckSeven" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (just), Text = "CheckEight" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
