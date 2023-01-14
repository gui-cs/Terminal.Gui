using System;
using System.ComponentModel;
using System.Linq;
using Terminal.Gui;
using Terminal.Gui.Graphs;

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
				X = Pos.Right (textField) + 1
			};
			cbHorizontal.Toggled += (s) => SetupSplitContainer ();

			cbBorder = new CheckBox ("Border") {
				X = Pos.Right (cbHorizontal) + 1
			};
			cbBorder.Toggled += (s) => SetupSplitContainer ();

			cbTitles = new CheckBox ("Titles") {
				X = Pos.Right (cbBorder) + 1
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

			var root = CreateSplitContainer (startHorizontal ?
					Terminal.Gui.Graphs.Orientation.Horizontal :
					Terminal.Gui.Graphs.Orientation.Vertical, false);

			root.Panel1.Add (CreateTextView (1));
			root.Panel1Title = titles ? "Panel 1" : string.Empty;

			root.Panel2.Add (CreateTextView (2));
			root.Panel2Title = titles ? "Panel 2" : string.Empty;

			root.IntegratedBorder = border ? BorderStyle.Rounded : BorderStyle.None;


			workArea.Add (root);

			if (numberOfPanels == 1) {
				root.Panel2.Visible = false;
			}

			if (numberOfPanels > 2) {

				AddMorePanels (root, 2, numberOfPanels);
			}

			if (loaded) {
				workArea.LayoutSubviews ();
			}
		}

		private View CreateTextView (int number)
		{
			return new TextView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = number.ToString ().Repeat (1000),
				AllowsTab = false,
				WordWrap = true,
			};
		}

		private void AddMorePanels (SplitContainer to, int done, int numberOfPanels)
		{
			if (done == numberOfPanels) {
				return;
			}

			View toSplit;

			if (!(to.Panel1 is SplitContainer)) {

				// we can split Panel1
				var tv = (TextView)to.Panel1.Subviews.Single ();

				var newContainer = CreateSplitContainer (to.Orientation, true);

				to.Remove (to.Panel1);
				to.Add (newContainer);
				to.Panel1 = newContainer;

				newContainer.Panel1.Add (tv);
				newContainer.Panel2.Add (CreateTextView (++done));

				AddMorePanels (to, done, numberOfPanels);
			} else
			if (!(to.Panel2 is SplitContainer)) {
				// we can split Panel2
				var tv = (TextView)to.Panel2.Subviews.Single ();

				var newContainer = CreateSplitContainer (to.Orientation, true);

				to.Remove (to.Panel2);
				to.Add (newContainer);
				to.Panel2 = newContainer;

				newContainer.Panel1.Add (tv);
				newContainer.Panel2.Add (CreateTextView (++done));

				AddMorePanels (to, done, numberOfPanels);
			} else {
				// Both Panel1 and Panel2 are already SplitContainer	

				// So split one of the children
				if(done % 2 == 0) {
					AddMorePanels ((SplitContainer)to.Panel1, done, numberOfPanels);
				} else {

					AddMorePanels ((SplitContainer)to.Panel2, done, numberOfPanels);
				}
			}
		}

		private SplitContainer CreateSplitContainer (Orientation orientation, bool flip)
		{
			var toReturn = new SplitContainer {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				// flip the orientation
				Orientation = orientation
			};

			if (flip) {
				toReturn.Orientation = toReturn.Orientation == Orientation.Vertical ?
					Orientation.Horizontal :
					Orientation.Vertical;
			}

			return toReturn;
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