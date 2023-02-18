using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Snake", Description: "The game of apple eating.")]
	[ScenarioCategory ("Colors")]
	public class Snake : Scenario
	{
		private bool isDisposed;

        public override void Setup ()
		{
			base.Setup ();

            var state = new SnakeState();

            Win.Add(new SnakeView(state){
                Width = Dim.Fill(),
                Height = Dim.Fill()
            });            
        }

        private class SnakeView : View {
            
			public SnakeState State { get; }

            public SnakeView(SnakeState state)
            {
				State = state;
			}
		}
        private class SnakeState : View {


        }
        private enum Direction{
            Up,
            Down,
            Left,
            Right
        }
    }
}