using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Designer {
#if false
	class Surface : Window {
		public Surface () : base ("Designer")
		{
		}
	}

	class MainClass {
		public static void Main (string [] args)
		{
			Application.Init ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => { Application.RequestStop (); })
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				}),
			});

			var login = new Label ("Login: ") { X = 3, Y = 6 };
			var password = new Label ("Password: ") {
				X = Pos.Left (login),
				Y = Pos.Bottom (login) + 1
			};

			var surface = new Surface () {
				X = 0,
				Y = 1,
				Width = Dim.Percent (80),
				Height = Dim.Fill ()
			};

			surface.Add (login, password);
			Application.Top.Add (menu, surface);
			Application.Run ();
		}
	}
#elif false
	class MainClass {
		public static void Main (string [] args)
		{
			Application.Init ();

			Window window = new Window ("Repaint Issue") { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
			RadioGroup radioGroup = new RadioGroup (1, 1, new [] { "Short", "Longer Text  --> Will not be repainted <--", "Short" });

			Button replaceButtonLonger = new Button (1, 10, "Replace Texts above Longer") {
				Clicked = () => { radioGroup.RadioLabels = new string [] { "Longer than before", "Shorter Text", "Longer than before" }; }
			};

			Button replaceButtonSmaller = new Button (35, 10, "Replace Texts above Smaller") {
				Clicked = () => { radioGroup.RadioLabels = new string [] { "Short", "Longer Text  --> Will not be repainted <--", "Short" }; }
			};

			window.Add (radioGroup, replaceButtonLonger, replaceButtonSmaller);
			Application.Top.Add (window);
			Application.Run ();
		}
	}
#elif false
	class MainClass {
		public static void Main (string [] args)
		{

			string[] radioLabels = { "First", "Second" };
			Application.Init();

			Window window = new Window("Redraw issue when setting coordinates of label") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

			Label radioLabel = new Label("Radio selection: ") { X = 1, Y = 1 };
			Label otherLabel = new Label("Other label: ") { X = Pos.Left(radioLabel), Y = Pos.Top(radioLabel) + radioLabels.Length };

			RadioGroup radioGroup = new RadioGroup(radioLabels) { X = Pos.Right(radioLabel), Y = Pos.Top(radioLabel) };
			RadioGroup radioGroup2 = new RadioGroup(new[] { "Option 1 of the second radio group", "Option 2 of the second radio group" }) { X = Pos.Right(radioLabel), Y = Pos.Top(otherLabel) };

			Button replaceButton = new Button(1, 10, "Add radio labels") {
				Clicked = () =>
				{
					radioGroup.RadioLabels = new[] { "First", "Second", "Third                             <- Third ->", "Fourth                            <- Fourth ->" };
					otherLabel.Y = Pos.Top(radioLabel) + radioGroup.RadioLabels.Length;
					//Application.Refresh(); // Even this won't redraw the app correctly, only a terminal resize will re-render the view.
					//typeof(Application).GetMethod("TerminalResized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
				}
			};

			window.Add(radioLabel, otherLabel, radioGroup, radioGroup2, replaceButton);
			Application.Top.Add(window);
			Application.Run();
		}
	}
#elif false
	class MainClass {
		static TaskScheduler syncContextTaskScheduler;

		public static void Main (string [] args)
		{
			Application.Init ();

			Window window = new Window ("When awaiting a method it is awaited until the cursor moves") { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

			Button button = new Button (1, 1, "Load Items");
			ListView itemsList = new ListView ();
			itemsList.X = Pos.X (button);
			itemsList.Y = Pos.Y (button) + 4;
			button.Clicked += async () => {
				Application.MainLoop.Invoke (async () => {
					itemsList.Clear ();
					//When the button is 'clicked' the following is executed
					Debug.WriteLine ($"Clicked the button");
					var items = await LoadItemsAsync ();

					//However the following line is not executed
					//until the button is clicked again or
					//until the cursor is moved to the next view/control 
					Debug.WriteLine ($"Got {items.Count} items)");
					itemsList.SetSource (items);

					//Without calling this the UI is not updated
					//this.LayoutSubviews ();
				});
			};

			window.Add (itemsList, button);
			Application.Top.Add (window);
			Application.Run ();
		}

		private static Task<List<string>> LoadItemsAsync ()
		{
			try {
				// Do something that takes lot of times.
				List<string> items = new List<string> () { "One", "Two", "Three" };
				return Task.FromResult (items);
			} catch (TaskCanceledException ex) {
				Debug.WriteLine (ex.Message);
				return Task.FromResult (new List<string> ());
			}
		}
	}
#elif false
	class MainClass {
		static TaskScheduler syncContextTaskScheduler;

		public static void Main (string [] args)
		{
			Application.Init ();

			Window window = new Window ("When awaiting a method it is awaited until the cursor moves") { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

			Button button = new Button (1, 1, "Load Items");
			ListView itemsList = new ListView ();
			itemsList.X = Pos.X (button);
			itemsList.Y = Pos.Y (button) + 4;
			button.Clicked += async () => {
				itemsList.Clear ();
				//When the button is 'clicked' the following is executed
				Debug.WriteLine ($"Clicked the button");
				using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ()) {
					if (button.Text == "Cancel") {
						button.Text = "Load Items";
						cancellationTokenSource.Cancel ();
					} else
						button.Text = "Cancel";
					syncContextTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext ();
					await Task<List<string>>.Run (() => LoadItemsAsync (cancellationTokenSource.Token).Result).ContinueWith (task => {
						//However the following line is not executed
						//until the button is clicked again or
						//until the cursor is moved to the next view/control
						var items = task.Result;
						Debug.WriteLine ($"Got {items.Count} items)");
						itemsList.SetSource (items);

						//Without calling this the UI is not updated
						//this.LayoutSubviews ();
						button.Text = "Load Items";
					}, CancellationToken.None,
						TaskContinuationOptions.OnlyOnRanToCompletion,
						syncContextTaskScheduler);
				}
			};

			window.Add (itemsList, button);
			Application.Top.Add (window);
			Application.Run ();
		}

		private static Task<List<string>> LoadItemsAsync (CancellationToken cancellationToken)
		{
			try {
				// Do something that takes lot of times.
				//Task.Delay (3000);
				Thread.Sleep (800);
				if (cancellationToken.IsCancellationRequested)
					throw new TaskCanceledException ("Task was cancelled");

				List<string> items = new List<string> () { "One", "Two", "Three" };
				cancellationToken.ThrowIfCancellationRequested ();
				return Task.FromResult (items);
			} catch (TaskCanceledException ex) {
				Debug.WriteLine (ex.Message);
				return Task.FromResult (new List<string> ());
			}
		}
	}
#elif true
	class MainClass {
		public static void Main (string [] args)
		{
			Application.Init ();

			Window window = new Window ("Button repainting issue when changing the text length to one smaller than before.") { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
			string strLonger = "Button with a long text";
			Button button = new Button (1, 5, strLonger);
			button.Clicked += () => {
				//When the button is 'clicked' the following is executed
				Debug.WriteLine ($"Clicked the button with text '{button.Text}'");
				if (button.Text != strLonger) {
					button.Text = strLonger;
				} else
					button.Text = "Small text";
			};

			window.Add (button);
			Application.Top.Add (window);
			Application.Run ();
		}
	}

#endif
}
