using Terminal.Gui;
using System;
using Terminal.Gui.Graphs;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Split Container", Description: "Demonstrates the SplitContainer functionality")]
	[ScenarioCategory ("Controls")]
	public class SplitContainerExample : Scenario {

		private SplitContainer splitContainer;


		private MenuItem miVertical;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Window's.
			Win.Title = this.GetName ();
			Win.Y = 1;

			splitContainer = new SplitContainer {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				SplitterDistance = Pos.Percent(50),
			};


			Label lbl1;
			splitContainer.Panel1.Add (new Label ("Hello"));
			splitContainer.Panel1.Add (lbl1 = new Label ("Type Something:"){Y=2});
			splitContainer.Panel1.Add (new TextField (){Width = Dim.Fill(),Y=2,X=Pos.Right(lbl1)+1});
			
			Label lbl2;
			splitContainer.Panel2.Add (new Label ("World"));
			splitContainer.Panel2.Add (lbl2 = new Label ("Type Here Too:"){Y=2});
			splitContainer.Panel2.Add (new TextField (){Width = Dim.Fill(),Y=2,X=Pos.Right(lbl2)+1});

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
				}})
			});
			Application.Top.Add (menu);
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