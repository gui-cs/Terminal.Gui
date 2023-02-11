using System;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using System.Linq;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Tile View as Frame", Description: "Experiments with Tile View")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class TileViewExperiment : Scenario {

		class NewFrameView : FrameView
		{
			
		}

		class TileFrameView : TileView
		{
			
		}

		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();
		}

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Quit", "", () => Application.RequestStop()),
			}) });

			Application.Top.Add (menu);

			var frame = new TileFrameView () {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			frame.Border.BorderStyle = BorderStyle.Double;
			Application.Top.Add (frame);

		}
		
	}
}