using System;
using Terminal.Gui;
using Terminal.Gui.Graphs;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Split View Nesting", Description: "Nest SplitViews")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class SplitViewNesting : Scenario {

		private View workArea;
		private TextField textField;
		private CheckBox cbHorizontal;
		private CheckBox cbBorder;
		private CheckBox cbTitles;
		private CheckBox cbUseLabels;

		bool loaded = false;
		int viewsCreated;
		int viewsToCreate;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Windows.
			Win.Title = this.GetName ();
			Win.Y = 1;

			var lblViews = new Label ("Number Of Views:");
			textField = new TextField {
				X = Pos.Right (lblViews),
				Width = 10,
				Text = "2",
			};

			textField.TextChanged += (s) => SetupSplitView ();


			cbHorizontal = new CheckBox ("Horizontal") {
				X = Pos.Right (textField) + 1
			};
			cbHorizontal.Toggled += (s) => SetupSplitView ();

			cbBorder = new CheckBox ("Border") {
				X = Pos.Right (cbHorizontal) + 1
			};
			cbBorder.Toggled += (s) => SetupSplitView ();

			cbTitles = new CheckBox ("Titles") {
				X = Pos.Right (cbBorder) + 1
			};
			cbTitles.Toggled += (s) => SetupSplitView ();

			cbUseLabels = new CheckBox ("Use Labels") {
				X = Pos.Right (cbTitles) + 1
			};
			cbUseLabels.Toggled += (s) => SetupSplitView ();

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

			Win.Add (lblViews);
			Win.Add (textField);
			Win.Add (cbHorizontal);
			Win.Add (cbBorder);
			Win.Add (cbTitles);
			Win.Add (cbUseLabels);
			Win.Add (workArea);

			SetupSplitView ();

			Application.Top.Add (menu);

			Win.Loaded += () => loaded = true;
		}

		private void SetupSplitView ()
		{
			int numberOfViews = GetNumberOfViews ();

			bool titles = cbTitles.Checked;
			bool border = cbBorder.Checked;
			bool startHorizontal = cbHorizontal.Checked;

			workArea.RemoveAll ();
			
			if (numberOfViews <= 0) {
				return;
			}

			var root = CreateSplitView (1,startHorizontal ?
					Terminal.Gui.Graphs.Orientation.Horizontal :
					Terminal.Gui.Graphs.Orientation.Vertical);

			root.View1.Add (CreateContentControl (1));
			root.View2.Add (CreateContentControl (2));
			

			root.IntegratedBorder = border ? BorderStyle.Rounded : BorderStyle.None;


			workArea.Add (root);

			if (numberOfViews == 1) {
				root.View2.Visible = false;
			}

			if (numberOfViews > 2) {

				viewsCreated = 2;
				viewsToCreate = numberOfViews;
				AddMoreViews (root);
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
				AutoSize = false,
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
				//WordWrap = true,  // TODO: This is very slow (like 10s to render with 45 views)
			};
		}

		private void AddMoreViews (SplitView to)
		{
			if (viewsCreated == viewsToCreate) {
				return;
			}

			if (!(to.View1 is SplitView)) {
				Split(to,true);
			}

			if (!(to.View2 is SplitView)) {
				Split(to,false);				
			}

			if (to.View1 is SplitView && to.View2 is SplitView) {

				AddMoreViews ((SplitView)to.View1);
				AddMoreViews ((SplitView)to.View2);
			}

		}
		private void Split(SplitView to, bool left)
		{
			if (viewsCreated == viewsToCreate) {
				return;
			}

			SplitView newView;

			if (left) {
				to.TrySplitView1 (out newView);

			}
			else {
				to.TrySplitView2 (out newView);
			}

			viewsCreated++;

			// During splitting the old Title will have been migrated to View1 so we only need
			// to set the Title on View2 (the one that gets our new TextView)
			newView.View2Title = cbTitles.Checked ? $"View {viewsCreated}" : string.Empty;

			// Flip orientation
			newView.Orientation = to.Orientation == Orientation.Vertical ?
				Orientation.Horizontal :
				Orientation.Vertical;
			
			newView.View2.Add (CreateContentControl(viewsCreated));
		}

		private SplitView CreateSplitView (int titleNumber, Orientation orientation)
		{
			var toReturn = new SplitView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				// flip the orientation
				Orientation = orientation
			};

			toReturn.View1Title = cbTitles.Checked ? $"View {titleNumber}" : string.Empty;
			toReturn.View2Title = cbTitles.Checked ? $"View {titleNumber + 1}" : string.Empty;

			return toReturn;
		}

		private int GetNumberOfViews ()
		{
			if (int.TryParse (textField.Text.ToString (), out var views) && views >= 0) {

				return views;
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