﻿using Terminal.Gui;
using System;
using Terminal.Gui.Graphs;
using NStack;
using System.Linq;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Split Container", Description: "Demonstrates the SplitContainer functionality")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class SplitContainerExample : Scenario {

		private SplitContainer splitContainer;

		private SplitContainer splitContainer2;
		private MenuItem miVertical;
		private MenuItem miShowBoth;
		private MenuItem miShowPanel1;
		private MenuItem miShowPanel2;
		private MenuItem miShowNeither;

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
				SplitterDistance = Pos.Percent (50), // TODO: get this to work with drag resizing and percents
			};
			splitContainer2 = new SplitContainer(){
				Width = Dim.Fill(),
				Height = Dim.Fill(),
				Orientation = Orientation.Horizontal
			};

			splitContainer.Panels [0].MinSize = 4;
			splitContainer.Panels [1].MinSize = 4;

			Label lbl1;
			splitContainer.Panels [0].Title = "Hello";
			splitContainer.Panels [0].Add (lbl1 = new Label ("Type Something:") { Y = 0 });
			splitContainer.Panels [0].Add (new TextField () { Width = Dim.Fill (), Y = 0, X = Pos.Right (lbl1) + 1 });

			Label lbl2;
			splitContainer.Panels [1].Title = "World";
			splitContainer.Panels[1].Add(splitContainer2);

			splitContainer2.Panels [0].Add (new TextView ()
			 {
				Width = Dim.Fill(),
				Height = Dim.Fill(),
				Text = GenerateLotsOfText(),
				AllowsTab = false,
				WordWrap = true,
			 });

			splitContainer2.Border.BorderStyle = BorderStyle.None;
			splitContainer2.Border.DrawMarginFrame = false;
			
			splitContainer2.Panels [1].Add (lbl2 = new Label ("Type Here Too:") { Y = 0 });
			splitContainer2.Panels [1].Add (new TextField () { Width = Dim.Fill (), Y = 0, X = Pos.Right (lbl2) + 1 });
			splitContainer2.Panels [1].Add (new Label ("Here is a Text box:") { Y = 1 });
			splitContainer2.Panels [1].Add (new TextView () { Y = 2, Width = Dim.Fill (), Height = Dim.Fill (), AllowsTab = false });

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
							splitContainer.Panels [0].Visible = true;
							splitContainer.Panels [1].Visible = true;
							UpdateShowMenuCheckedStates();
						}),
						miShowPanel1 = new MenuItem ("Panel 1", "", () => {
							splitContainer.Panels [0].Visible = true;
							splitContainer.Panels [1].Visible = false;
							UpdateShowMenuCheckedStates();
						}),
						miShowPanel2 = new MenuItem ("Panel 2", "", () => {
							splitContainer.Panels [0].Visible = false;
							splitContainer.Panels [1].Visible = true;
							UpdateShowMenuCheckedStates();
						}),
						miShowNeither = new MenuItem ("Neither", "",()=>{
							splitContainer.Panels [0].Visible = false;
							splitContainer.Panels [1].Visible = false;
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
			miShowBoth.Checked = (splitContainer.Panels [0].Visible) && (splitContainer.Panels [1].Visible);
			miShowBoth.CheckType = MenuItemCheckStyle.Checked;

			miShowPanel1.Checked = splitContainer.Panels [0].Visible && !splitContainer.Panels [1].Visible;
			miShowPanel1.CheckType = MenuItemCheckStyle.Checked;

			miShowPanel2.Checked = !splitContainer.Panels [0].Visible && splitContainer.Panels [1].Visible;
			miShowPanel2.CheckType = MenuItemCheckStyle.Checked;

			miShowNeither.Checked = (!splitContainer.Panels [0].Visible) && (!splitContainer.Panels [1].Visible);
			miShowNeither.CheckType = MenuItemCheckStyle.Checked;
		}

		public void ToggleOrientation ()
		{
			miVertical.Checked = !miVertical.Checked;
			splitContainer.Orientation = miVertical.Checked ? Orientation.Vertical : Orientation.Horizontal;
			splitContainer2.Orientation = miVertical.Checked ? Orientation.Horizontal : Orientation.Vertical;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}