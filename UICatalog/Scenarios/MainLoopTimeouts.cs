#pragma warning disable format

#pragma warning restore format
using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "MainLoopTimeouts", Description: "MainLoop Timeouts")]
	[ScenarioCategory ("Tests")]
	public class MainLoopTimeouts : Scenario {
		static readonly List<string> GlobalList = new () { "1" };
		static readonly ListView GlobalListView = new () { Width = Dim.Fill (), Height = Dim.Fill () };

		static Label CounterLabel;
		static Label BlinkingLabel;

		static int Counter = 0;

		static object _listToken = null;
		static object _blinkToken = null;
		static object _countToken = null;

		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();

			var startButton = new Button ("Start");
			var stopButton = new Button ("Stop") { Y = 1 };
			var container = new View () { X = Pos.Center (), Y = Pos.Center (), Width = 8, Height = 8, ColorScheme = Colors.Error };

			CounterLabel = new Label ("0") { X = Pos.X (container), Y = Pos.Y (container) - 2 };
			BlinkingLabel = new Label ("Blink") { X = Pos.X (container), Y = Pos.Bottom (container) + 1 };

			startButton.Clicked += Start;
			stopButton.Clicked += Stop;

			GlobalListView.SetSource (GlobalList);
			container.Add (GlobalListView);

			Application.Top.Add (container, CounterLabel, BlinkingLabel);
			Application.Top.Add (startButton, stopButton);
			Application.Run ();
			Application.Shutdown ();
		}

		public override void Run ()
		{
		}

		private static void Start ()
		{
			_listToken = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (100), Add);
			_blinkToken = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (1000), Blink);
			_countToken = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (1000), Count);
		}

		private static void Stop ()
		{
			Application.MainLoop.RemoveTimeout (_listToken);
			Application.MainLoop.RemoveTimeout (_blinkToken);
			Application.MainLoop.RemoveTimeout (_countToken);
		}

		private static bool Add (MainLoop mainLoop)
		{
			Application.MainLoop.Invoke (() => {
				GlobalList.Add (new Random ().Next (100).ToString ());
				GlobalListView.MoveDown ();
			});

			return true;
		}

		private static bool Blink (MainLoop mainLoop)
		{
			Application.MainLoop.Invoke (() => {
				if (BlinkingLabel.Visible) {
					BlinkingLabel.Visible = false;
					System.Diagnostics.Debug.WriteLine (BlinkingLabel.Visible);
				} else {
					BlinkingLabel.Visible = true;
					System.Diagnostics.Debug.WriteLine (BlinkingLabel.Visible);
				}

			});

			return true;
		}

		private static bool Count (MainLoop mainLoop)
		{
			Application.MainLoop.Invoke (() => {
				Counter++;
				CounterLabel.Text = Counter.ToString ();
			});

			return true;
		}
	}
}