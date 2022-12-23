using Terminal.Gui;
using System;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Split Container", Description: "Demonstrates the SplitContainer functionality")]
	[ScenarioCategory ("Controls")]
	public class SplitContainerExample : Scenario {

		private SplitContainer splitContainer;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Window's.
			Win.Title = this.GetName ();

			splitContainer = new SplitContainer {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				SplitterDistance = Pos.Percent(50),
			};


			splitContainer.Panel1.Add (new Label ("Hello"));
			splitContainer.Panel2.Add (new Label ("World"));

			Win.Add (splitContainer);
		}
	}
}