using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	/// <summary>
	/// This Scenario demonstrates how to use Termina.gui's Dim and Pos Layout System. 
	/// [x] - Using Dim.Fill to fill a window
	/// [x] - Using Dim.Fill and Dim.Pos to automatically align controls based on an initial control
	/// [ ] - ...
	/// </summary>
	[ScenarioMetadata (Name: "All Views Tester", Description: "Provides a test UI for all classes derived from View")]
	[ScenarioCategory ("Layout")]
	class AllViewsTester : Scenario {
		private static Window _leftPane;
		private static ListView _classListView;
		private static FrameView _settingsPane;
		private static FrameView _hostPane;

		Dictionary<string, Type> _viewClasses;
		Type _curClass = null;
		View _curView = null;
		StatusItem _currentClassStatusItem;


		// Settings
		CheckBox _computedCheckBox;


		public override void Init (Toplevel top)
		{
			Application.Init ();

			Top = top;
			if (Top == null) {
				Top = Application.Top;
			}

			//Win = new Window ($"CTRL-Q to Close - Scenario: {GetName ()}") {
			//	X = 0,
			//	Y = 0,
			//	Width = Dim.Fill (),
			//	Height = Dim.Fill ()
			//};
			//Top.Add (Win);
		}

		public override void Setup ()
		{
			_currentClassStatusItem = new StatusItem (Key.Unknown, "Class:", null);
			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.ControlQ, "~^Q~ Quit", () => Quit()),
				_currentClassStatusItem,
			});
			Top.Add (statusBar);

			_viewClasses = GetAllViewClassesCollection ()
				.OrderBy (t => t.Name)
				.Select (t => new KeyValuePair<string, Type> (t.Name, t))
				.ToDictionary (t => t.Key, t => t.Value);

			_leftPane = new Window ("Classes") {
				X = 0,
				Y = 0, // for menu
				Width = 15,
				Height = Dim.Fill (),
				CanFocus = false,
				ColorScheme = Colors.TopLevel,
			};

			_classListView = new ListView (_viewClasses.Keys.ToList()) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (), // for status bar
				AllowsMarking = false,
				ColorScheme = Colors.TopLevel,
			};
			_classListView.OpenSelectedItem += (o, a) => {
				Top.SetFocus (_settingsPane);
			};
			_classListView.SelectedChanged += (sender, args) => {
				_curClass = _viewClasses.Values.ToArray()[_classListView.SelectedItem];
				SetCurrentClass ();
			};
			_leftPane.Add (_classListView);

			_settingsPane = new FrameView ("Settings") {
				X = Pos.Right(_leftPane),
				Y = 0, // for menu
				Width = Dim.Fill (),
				Height = 10,
				CanFocus = false,
				ColorScheme = Colors.TopLevel,
			};
			_computedCheckBox = new CheckBox ("Computed Layout", true) { X = 0, Y = 0 };
			_computedCheckBox.Toggled += (sender, previousState) => {
				if (_curView != null) {
					_curView.LayoutStyle = previousState ? LayoutStyle.Absolute : LayoutStyle.Computed;
					_hostPane.LayoutSubviews ();
				}

			};
			_settingsPane.Add (_computedCheckBox);

			_hostPane = new FrameView ("") {
				X = Pos.Right (_leftPane) + 2,
				Y = Pos.Bottom(_settingsPane) + 2, 
				Width = Dim.Fill (2),
				Height = Dim.Fill (3), // + 1 for status bar
				ColorScheme = Colors.Dialog,
			};

			Top.Add (_leftPane, _settingsPane, _hostPane);

			_curClass = _viewClasses.First().Value;			
			SetCurrentClass ();
		}

		List<Type> GetAllViewClassesCollection ()
		{
			List<Type> types = new List<Type> ();
			foreach (Type type in typeof (View).Assembly.GetTypes ()
			 .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsPublic && myType.IsSubclassOf (typeof (View)))) {
				types.Add (type);
			}
			return types;
		}

		void SetCurrentClass ()
		{
			_hostPane.Title = _currentClassStatusItem.Title = $"Class: {_curClass.Name}";

			// Remove existing class, if any
			if (_curView != null) {
				_hostPane.Remove (_curView);
				_hostPane.Clear ();
				_curView = null;
			}

			// Instantiate view
			_curView = (View)Activator.CreateInstance (_curClass);

			_curView.X = Pos.Center ();
			_curView.Y = Pos.Center ();
			_curView.Width = Dim.Fill (5);
			_curView.Height = Dim.Fill (5);

			// Set the colorscheme to make it stand out
			_curView.ColorScheme = Colors.Base;

			// If the view supports a Text property, set it so we have something to look at
			if (_curClass.GetProperty ("Text") != null) {
				try {
					_curView.GetType ().GetProperty ("Text")?.GetSetMethod ()?.Invoke (_curView, new [] { ustring.Make ("Test Text") });
				}
				catch (TargetInvocationException e) {
					MessageBox.ErrorQuery ("Exception", e.InnerException.Message, "Ok");
					_hostPane.Remove (_curView);
					_hostPane.Clear ();
					_curView = null;
				}
			}

			if (_curView == null) return;

			// If the view supports a Title property, set it so we have something to look at
			if (_curClass.GetProperty ("Title") != null) {
				_curView?.GetType ().GetProperty ("Title")?.GetSetMethod ()?.Invoke (_curView, new [] { ustring.Make ("Test Title") });
			}

			// Set Settings
			_computedCheckBox.Checked = _curView.LayoutStyle == LayoutStyle.Computed;


			// Add
			_hostPane.Add (_curView);
			_hostPane.LayoutSubviews ();
			_hostPane.Clear ();
			_hostPane.SetNeedsDisplay ();
		}

		public override void Run ()
		{
			base.Run ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}