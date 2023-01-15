using System;
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
		private CheckBox cbUseLabels;

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

			cbUseLabels = new CheckBox ("Use Labels") {
				X = Pos.Right (cbTitles) + 1
			};
			cbUseLabels.Toggled += (s) => SetupSplitContainer ();

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
			Win.Add (cbUseLabels);
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
					Terminal.Gui.Graphs.Orientation.Vertical);

			root.Panel1.Add (CreateContentControl (1));
			root.Panel2.Add (CreateContentControl (2));
			

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

		private View CreateContentControl (int number)
		{
			return cbUseLabels.Checked ?
				CreateLabelView (number) :
				CreateTextView (number);
		}

		private View CreateLabelView (int number)
		{
			return new Label {
				Width = Dim.Fill (),
				Height = 1,
				Text = number.ToString ().Repeat (1000),
				CanFocus = true,
			};
		}
		private View CreateTextView (int number)
		{
			return new TextView {
				Width = Dim.Fill (),
				Height = Dim.Fill(),
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
				Split(to,true);
			}

			if (!(to.Panel2 is SplitContainer)) {
				Split(to,false);				
			}

			if (to.Panel1 is SplitContainer && to.Panel2 is SplitContainer) {

				AddMorePanels ((SplitContainer)to.Panel1);
				AddMorePanels ((SplitContainer)to.Panel2);
			}

		}
		private void Split(SplitContainer to, bool left)
		{
			if (panelsCreated == panelsToCreate) {
				return;
			}

			SplitContainer newContainer;

			if (left) {
				to.TrySplitPanel1 (out newContainer);

			}
			else {
				to.TrySplitPanel2 (out newContainer);
			}

			panelsCreated++;

			// During splitting the old Title will have been migrated to Panel1 so we only need
			// to set the Title on Panel2 (the one that gets our new TextView)
			newContainer.Panel2Title = cbTitles.Checked ? $"Panel {panelsCreated}" : string.Empty;

			// Flip orientation
			newContainer.Orientation = newContainer.Orientation == Orientation.Vertical ?
				Orientation.Horizontal :
				Orientation.Vertical;
			
			newContainer.Panel2.Add (CreateContentControl(panelsCreated));
		}

		private SplitContainer CreateSplitContainer (int titleNumber, Orientation orientation)
		{
			var toReturn = new SplitContainer {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				// flip the orientation
				Orientation = orientation
			};

			toReturn.Panel1Title = cbTitles.Checked ? $"Panel {titleNumber}" : string.Empty;
			toReturn.Panel2Title = cbTitles.Checked ? $"Panel {titleNumber + 1}" : string.Empty;

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