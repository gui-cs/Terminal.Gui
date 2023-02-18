using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Snake", Description: "The game of apple eating.")]
	[ScenarioCategory ("Colors")]
	public class Snake : Scenario {
		private bool isDisposed;

		public override void Setup ()
		{
			base.Setup ();

			var state = new SnakeState ();

			state.Reset (100, 20);

			var snakeView = new SnakeView (state) {
				Width = state.Width,
				Height = state.Height
			};

			Win.Add (snakeView);
			
			Task.Run (() => {
				while (!isDisposed) {

					state.AdvanceState ();

					// When updating from a Thread/Task always use Invoke
					Application.MainLoop.Invoke (() => {
						snakeView.SetNeedsDisplay ();
					});

					Task.Delay (100).Wait ();
				}
			});
		}

		private class SnakeView : View {

			public SnakeState State { get; }

			public SnakeView (SnakeState state)
			{
				State = state;
				CanFocus = true;
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				var canvas = new LineCanvas ();

				canvas.AddLine (new Point (0, 0), State.Width-1, Orientation.Horizontal, BorderStyle.Double);
				canvas.AddLine (new Point (0, 0), State.Height-1, Orientation.Vertical, BorderStyle.Double);
				canvas.AddLine (new Point (0, State.Height-1), State.Width-1, Orientation.Horizontal, BorderStyle.Double);
				canvas.AddLine (new Point (State.Width-1, 0), State.Height - 1, Orientation.Vertical, BorderStyle.Double);

				canvas.Draw (this, Bounds);

				AddRune (State.Head.X, State.Head.Y, 'S');
				AddRune (State.Apple.X, State.Apple.Y, 'A');
			}
			public override bool OnKeyDown (KeyEvent keyEvent)
			{
				if(keyEvent.Key == Key.CursorUp) {
					State.PlannedDirection = Direction.Up;
					return true;
				}
				if (keyEvent.Key == Key.CursorDown) {
					State.PlannedDirection = Direction.Down;
					return true;
				}
				if (keyEvent.Key == Key.CursorLeft) {
					State.PlannedDirection = Direction.Left;
					return true;
				}
				if (keyEvent.Key == Key.CursorRight) {
					State.PlannedDirection = Direction.Right;
					return true;
				}

				return false;
			}
		}
		private class SnakeState {

			public int Width { get; private set; }
			public int Height { get; private set; }

			/// <summary>
			/// Position of the snakes head
			/// </summary>
			public Point Head { get; private set; }

			/// <summary>
			/// Current position of the Apple that the snake has to eat.
			/// </summary>
			public Point Apple { get; private set; }

			public Direction CurrentDirection { get; private set; }
			public Direction PlannedDirection { get; set; }

			internal void AdvanceState ()
			{
				UpdateDirection ();

				var newHead = GetNewHeadPoint ();

				Head = newHead;

				if(IsDeath(newHead)) {
					GameOver ();
				}

			}

			private void UpdateDirection ()
			{
				if(!AreOpposites(CurrentDirection,PlannedDirection)) {
					CurrentDirection = PlannedDirection;
				}
			}

			private bool AreOpposites (Direction a, Direction b)
			{
				switch(a) {
					case Direction.Left: return b == Direction.Right;
					case Direction.Right: return b == Direction.Left;
					case Direction.Up: return b == Direction.Down;
					case Direction.Down: return b == Direction.Up;
				}

				return false;
			}

			private void GameOver ()
			{
				Reset (Width, Height);
			}

			private Point GetNewHeadPoint ()
			{
				switch (CurrentDirection) {
				case Direction.Left:
					return new Point (Head.X - 1, Head.Y);

				case Direction.Right:
					return new Point (Head.X + 1, Head.Y);

				case Direction.Up:
					return new Point (Head.X, Head.Y -1);

				case Direction.Down:
					return new Point (Head.X, Head.Y + 1);
				}

				throw new Exception ("Unknown direction");
			}

			/// <summary>
			/// Restarts the game with the given canvas size
			/// </summary>
			/// <param name="width"></param>
			/// <param name="height"></param>
			internal void Reset (int width, int height)
			{
				if (width < 5 || height < 5) {
					return;
				}

				Width = width;
				Height = height;


				Head = new Point(width/2,height/2);
				Apple = GetNewRandomApplePoint ();
			}

			private Point GetNewRandomApplePoint ()
			{
				Random r = new Random();

				for(int i = 0;i<1000;i++) {
					var x = r.Next (0, Width);
					var y = r.Next (0, Height);

					var p = new Point (x, y);

					if (p == Head) {
						continue;
					}

					if (IsDeath (p)) {
						continue;
					}

					return p;
				}

				// Game is won or we are unable to generate a valid apple
				// point after 1000 attempts.  Maybe screen size is very small
				// or something.  Either way restart the game.
				Reset(Width, Height);
				return Apple;
			}

			private bool IsDeath (Point p)
			{
				if(p.X <= 0 || p.X >= Width-1) {
					return true;
				}

				if (p.Y <= 0 || p.Y >= Height-1) {
					return true;
				}

				//TODO: if over snakes body

				return false;
			}
		}
		private enum Direction {
			Up,
			Down,
			Left,
			Right
		}
	}
}