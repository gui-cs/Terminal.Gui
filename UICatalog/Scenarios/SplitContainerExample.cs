using Terminal.Gui;
using System;
using Terminal.Gui.Graphs;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Split Container", Description: "Demonstrates the SplitContainer functionality")]
	[ScenarioCategory ("Controls")]
	public class SplitContainerExample : Scenario {

		private SplitContainer splitContainer;


		private MenuItem miVertical;
		private MenuItem miShowBoth;
		private MenuItem miShowPanel1;
		private MenuItem miShowPanel2;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Window's.
			Win.Title = this.GetName ();
			Win.Y = 1;

			Win.Add (new Label ("This is a SplitContainer with a minimum panel size of 2.  Drag the splitter to resize:"));
			Win.Add (new LineView (Orientation.Horizontal) { Y = 1 });

			splitContainer = new SplitContainer {
				Y = 2,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				SplitterDistance = Pos.Percent (50), // TODO: get this to work with drag resizing and percents
				Panel1MinSize = 2,
				Panel2MinSize = 2,
			};


			Label lbl1;
			splitContainer.Panel1.Add (new Label ("Hello"));
			splitContainer.Panel1.Add (lbl1 = new Label ("Type Something:"){Y=2});
			splitContainer.Panel1.Add (new TextField (){Width = Dim.Fill(),Y=2,X=Pos.Right(lbl1)+1});
			
			Label lbl2;
			splitContainer.Panel2.Add (new Label ("World"));
			splitContainer.Panel2.Add (lbl2 = new Label ("Type Here Too:"){Y=2});
			splitContainer.Panel2.Add (new TextField (){Width = Dim.Fill(),Y=2,X=Pos.Right(lbl2)+1});
			splitContainer.Panel2.Add (new Label ("Here is a Text box:") { Y = 4 });
			splitContainer.Panel2.Add (new TextView () { Y = 5, Width = Dim.Fill(), Height = Dim.Fill(), AllowsTab = false});


			Win.Add (splitContainer);


			var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Quit", "", () => Quit()),
			}),
			new MenuBarItem ("_Options", new MenuItem [] {
				miVertical = new MenuItem ("_Vertical", "", () => ToggleOrientation())
				{
					Checked = splitContainer.Orientation == Orientation.Vertical,
					CheckType = MenuItemCheckStyle.Checked
				},
				new MenuBarItem ("_Show", new MenuItem [] {
						miShowBoth = new MenuItem ("Both", "",()=>{
							splitContainer.Panel1Collapsed = false;
							splitContainer.Panel2Collapsed = false;
							UpdateShowMenuCheckedStates();
						}),
						miShowPanel1 = new MenuItem ("Panel1", "", () => {

							splitContainer.Panel2Collapsed = true;
							UpdateShowMenuCheckedStates();
						}),
						miShowPanel2 = new MenuItem ("Panel2", "", () => {
							splitContainer.Panel1Collapsed = true;
							UpdateShowMenuCheckedStates();
						}),							
					})
				}),

			}) ;

			UpdateShowMenuCheckedStates ();
			
			Application.Top.Add (menu);
		}

		private void UpdateShowMenuCheckedStates ()
		{
			miShowBoth.Checked = (!splitContainer.Panel1Collapsed) && (!splitContainer.Panel2Collapsed);
			miShowBoth.CheckType = MenuItemCheckStyle.Checked;

			miShowPanel1.Checked = splitContainer.Panel2Collapsed;
			miShowPanel1.CheckType = MenuItemCheckStyle.Checked;

			miShowPanel2.Checked = splitContainer.Panel1Collapsed;
			miShowPanel2.CheckType = MenuItemCheckStyle.Checked;
		}

		public void ToggleOrientation()
		{

			miVertical.Checked = !miVertical.Checked;
			splitContainer.Orientation =  miVertical.Checked ? Orientation.Vertical : Orientation.Horizontal;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}