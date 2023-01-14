using System;
using System.ComponentModel;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Split Container Nesting", Description: "Nest SplitContainers")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class SplitContainerNesting : Scenario {

		private View workArea;
		private TextField textField;
		private CheckBox cbHorizontal;
		private CheckBox cbBorder;
		private CheckBox cbTitles;

		bool loaded = false;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Windows.
			Win.Title = this.GetName ();
			Win.Y = 1;

			var lblPanels = new Label ("Number Of Panels:");
			textField = new TextField {
				X = Pos.Right (lblPanels),
				Width = 10,
				Text = "2",
			};

			textField.TextChanged += (s) => SetupSplitContainer ();


			cbHorizontal = new CheckBox ("Horizontal") {
				X = Pos.Right (textField) +1
			};
			cbHorizontal.Toggled += (s) => SetupSplitContainer ();

			cbBorder = new CheckBox ("Border") {
				X = Pos.Right (cbHorizontal)+1
			};
			cbBorder.Toggled += (s) => SetupSplitContainer ();
			
			cbTitles = new CheckBox ("Titles") {
				X = Pos.Right (cbBorder)+1
			};
			cbTitles.Toggled += (s) => SetupSplitContainer ();

			workArea = new View {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Quit", "", () => Quit()),
			}) });

			Win.Add (lblPanels);
			Win.Add (textField);
			Win.Add (cbHorizontal);
			Win.Add (cbBorder);
			Win.Add (cbTitles);
			Win.Add (workArea);

			SetupSplitContainer ();

			Application.Top.Add (menu);

			Win.Loaded += () => loaded = true;
		}

		private void SetupSplitContainer ()
		{
			int numberOfPanels = GetNumberOfPanels ();

			bool titles = cbTitles.Checked;
			bool border = cbBorder.Checked;
			bool startHorizontal = cbHorizontal.Checked;

			workArea.RemoveAll ();

			if (numberOfPanels <= 0) {
				return;
			}

			var root = new SplitContainer {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Orientation = startHorizontal ?
					Terminal.Gui.Graphs.Orientation.Horizontal :
					Terminal.Gui.Graphs.Orientation.Vertical,
			};
			root.Panel1.Add (new TextView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = new string ('1', 10000),
				AllowsTab = false,
			});
			root.Panel1Title = titles ? "Panel 1" : string.Empty;

			root.Panel2.Add (new TextView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = new string ('2', 10000),
				AllowsTab = false,
			});
			root.Panel2Title = titles ? "Panel 2" : string.Empty;

			root.IntegratedBorder = border ? BorderStyle.Rounded : BorderStyle.None;


			workArea.Add (root);

			if (numberOfPanels == 1) {
				root.Panel2.Visible = false;
			}

			if (numberOfPanels > 2) {

				// TODO: Add more
			}

			if (loaded) {
				workArea.LayoutSubviews ();
			}
		}

		private int GetNumberOfPanels ()
		{
			if (int.TryParse (textField.Text.ToString (), out var panels) && panels >= 0) {

				return panels;
			} else {
				return 0;
			}
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}