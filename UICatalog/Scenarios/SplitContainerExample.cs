using Terminal.Gui;
using Terminal.Gui.Graphs;
using NStack;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Split Container", Description: "Demonstrates the SplitContainer functionality")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class SplitContainerExample : Scenario {

		private SplitContainer splitContainer;

		private SplitContainer nestedSplitContainer;
		private MenuItem miVertical;
		private MenuItem miShowBoth;
		private MenuItem miShowPanel1;
		private MenuItem miShowPanel2;
		private MenuItem miShowNeither;

		private MenuItem miSplitContainer1Border;
		private MenuItem minestedSplitContainerBorder;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Windows.
			Win.Title = this.GetName ();
			Win.Y = 1;

			Win.Add (new Label ("This is a SplitContainer with a minimum panel size of 4. Drag the splitter to resize:"));

			splitContainer = new SplitContainer {
				Y = 2,
				X = 2,
				Width = Dim.Fill () - 2,
				Height = Dim.Fill () - 1,
				SplitterDistance = Pos.Percent (50),
			};
			nestedSplitContainer = new SplitContainer(){
				Width = Dim.Fill(),
				Height = Dim.Fill(),
				Orientation = Orientation.Horizontal
			};

			splitContainer.Panel1MinSize = 4;
			splitContainer.Panel2MinSize = 4;

			Label lbl1;
			splitContainer.Panel1Title = "Hello";
			splitContainer.Panel1.Add (lbl1 = new Label ("Type Something:") { Y = 0 });
			splitContainer.Panel1.Add (new TextField () { Width = Dim.Fill (), Y = 0, X = Pos.Right (lbl1) + 1 });

			Label lbl2;
			splitContainer.Panel2Title = "World";
			splitContainer.Panel2.Add(nestedSplitContainer);

			nestedSplitContainer.Panel1.Add (new TextView ()
			 {
				Width = Dim.Fill(),
				Height = Dim.Fill(),
				Text = GenerateLotsOfText(),
				AllowsTab = false,
				WordWrap = true,
			 });

			nestedSplitContainer.IntegratedBorder = BorderStyle.None;
			
			nestedSplitContainer.Panel2.Add (lbl2 = new Label ("Type Here Too:") { Y = 0 });
			nestedSplitContainer.Panel2.Add (new TextField () { Width = Dim.Fill (), Y = 0, X = Pos.Right (lbl2) + 1 });
			nestedSplitContainer.Panel2.Add (new Label ("Here is a Text box:") { Y = 1 });
			nestedSplitContainer.Panel2.Add (new TextView () { Y = 2, Width = Dim.Fill (), Height = Dim.Fill (), AllowsTab = false });

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
				miSplitContainer1Border = new MenuItem ("_Outer Panel Border", "", () => ToggleBorder(miSplitContainer1Border, splitContainer))
				{
					Checked = splitContainer.IntegratedBorder == BorderStyle.Single,
					CheckType = MenuItemCheckStyle.Checked
				},
				minestedSplitContainerBorder = new MenuItem ("_Inner Panel Border", "", () => ToggleBorder(minestedSplitContainerBorder,nestedSplitContainer))
				{
					Checked = nestedSplitContainer.IntegratedBorder == BorderStyle.Single,
					CheckType = MenuItemCheckStyle.Checked
				},
				new MenuBarItem ("_Show", new MenuItem [] {
						miShowBoth = new MenuItem ("Both", "",()=>{
							splitContainer.Panel1.Visible = true;
							splitContainer.Panel2.Visible = true;
							UpdateShowMenuCheckedStates();
						}),
						miShowPanel1 = new MenuItem ("Panel 1", "", () => {
							splitContainer.Panel1.Visible = true;
							splitContainer.Panel2.Visible = false;
							UpdateShowMenuCheckedStates();
						}),
						miShowPanel2 = new MenuItem ("Panel 2", "", () => {
							splitContainer.Panel1.Visible = false;
							splitContainer.Panel2.Visible = true;
							UpdateShowMenuCheckedStates();
						}),
						miShowNeither = new MenuItem ("Neither", "",()=>{
							splitContainer.Panel1.Visible = false;
							splitContainer.Panel2.Visible = false;
							UpdateShowMenuCheckedStates();
						}),
					})
				}),
			});

			UpdateShowMenuCheckedStates ();

			Application.Top.Add (menu);
		}

		private ustring GenerateLotsOfText ()
		{
			return "Hello There ".Repeat(100);
		}

		private void UpdateShowMenuCheckedStates ()
		{
			miShowBoth.Checked = (splitContainer.Panel1.Visible) && (splitContainer.Panel2.Visible);
			miShowBoth.CheckType = MenuItemCheckStyle.Checked;

			miShowPanel1.Checked = splitContainer.Panel1.Visible && !splitContainer.Panel2.Visible;
			miShowPanel1.CheckType = MenuItemCheckStyle.Checked;

			miShowPanel2.Checked = !splitContainer.Panel1.Visible && splitContainer.Panel2.Visible;
			miShowPanel2.CheckType = MenuItemCheckStyle.Checked;

			miShowNeither.Checked = (!splitContainer.Panel1.Visible) && (!splitContainer.Panel2.Visible);
			miShowNeither.CheckType = MenuItemCheckStyle.Checked;
		}

		public void ToggleOrientation ()
		{
			miVertical.Checked = !miVertical.Checked;
			splitContainer.Orientation = miVertical.Checked ? Orientation.Vertical : Orientation.Horizontal;
			nestedSplitContainer.Orientation = miVertical.Checked ? Orientation.Horizontal : Orientation.Vertical;
		}

		private void ToggleBorder (MenuItem menuItem, SplitContainer splitContainer)
		{
			menuItem.Checked = !menuItem.Checked;
			
			if(menuItem.Checked) {
				splitContainer.IntegratedBorder = BorderStyle.Single;
			} else {
				splitContainer.IntegratedBorder = BorderStyle.None;
			}
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}