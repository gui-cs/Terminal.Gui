using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
		int panelsCreated;
		int panelsToCreate;

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

			var root = CreateSplitContainer (1,startHorizontal ?
					Terminal.Gui.Graphs.Orientation.Horizontal :
					Terminal.Gui.Graphs.Orientation.Vertical, false);

			root.Panel1.Add (CreateTextView (1));
			root.Panel2.Add (CreateTextView (2));
			

			root.IntegratedBorder = border ? BorderStyle.Rounded : BorderStyle.None;


			workArea.Add (root);

			if (numberOfPanels == 1) {
				root.Panel2.Visible = false;
			}

			if (numberOfPanels > 2) {

				panelsCreated = 2;
				panelsToCreate = numberOfPanels;
				AddMorePanels (root);
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
				//WordWrap = true,  // TODO: This is very slow (like 10s to render with 45 panels)
			};
		}

		private void AddMorePanels (SplitContainer to)
		{
			if (panelsCreated == panelsToCreate) {
				return;
			}

			if (!(to.Panel1 is SplitContainer)) {
				SplitLeft (to);
			}

			if (!(to.Panel2 is SplitContainer)) {
				SplitRight (to);				
			}

			if (to.Panel1 is SplitContainer && to.Panel2 is SplitContainer) {

				AddMorePanels ((SplitContainer)to.Panel1);
				AddMorePanels ((SplitContainer)to.Panel2);
			}

		}
		private void SplitLeft(SplitContainer to)
		{
			if (panelsCreated == panelsToCreate) {
				return;
			}

			// we can split Panel1
			var tv = (TextView)to.Panel1.Subviews.Single ();

			panelsCreated++;

			var newContainer = CreateSplitContainer (panelsCreated, to.Orientation, true);

			to.Remove (to.Panel1);
			to.Add (newContainer);
			to.Panel1 = newContainer;

			newContainer.Panel1.Add (tv);
			newContainer.Panel2.Add (CreateTextView (panelsCreated));
		}
		private void SplitRight(SplitContainer to)
		{
			if (panelsCreated == panelsToCreate) {
				return;
			}

			// we can split Panel2
			var tv = (TextView)to.Panel2.Subviews.Single ();
			panelsCreated++;

			var newContainer = CreateSplitContainer (panelsCreated, to.Orientation, true);

			to.Remove (to.Panel2);
			to.Add (newContainer);
			to.Panel2 = newContainer;

			newContainer.Panel2.Add (tv);
			newContainer.Panel1.Add (CreateTextView (panelsCreated));
		}

		private SplitContainer CreateSplitContainer (int titleNumber, Orientation orientation, bool flip)
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
			toReturn.Panel1Title = cbTitles.Checked ? $"Panel {titleNumber}" : string.Empty;
			toReturn.Panel2Title = cbTitles.Checked ? $"Panel {titleNumber+1}" : string.Empty;

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