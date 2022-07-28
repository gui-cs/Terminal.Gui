﻿using System;
using System.Collections.Generic;
using System.Linq;
using NStack;
using Terminal.Gui.Resources;

namespace Terminal.Gui {

	/// <summary>
	/// Provides navigation and a user interface (UI) to collect related data across multiple steps. Each step (<see cref="WizardStep"/>) can host 
	/// arbitrary <see cref="View"/>s, much like a <see cref="Dialog"/>. Each step also has a pane for help text. Along the
	/// bottom of the Wizard view are customizable buttons enabling the user to navigate forward and backward through the Wizard. 
	/// </summary>
	/// <remarks>
	/// The Wizard can be displayed either as a modal (pop-up) <see cref="Window"/> (like <see cref="Dialog"/>) or as an embedded <see cref="View"/>. 
	/// 
	/// By default, <see cref="Wizard.Modal"/> is <c>true</c>. In this case launch the Wizard with <c>Application.Run(wizard)</c>. 
	/// 
	/// See <see cref="Wizard.Modal"/> for more details.
	/// </remarks>
	/// <example>
	/// <code>
	/// using Terminal.Gui;
	/// using NStack;
	/// 
	/// Application.Init();
	/// 
	/// var wizard = new Wizard ($"Setup Wizard");
	/// 
	/// // Add 1st step
	/// var firstStep = new Wizard.WizardStep ("End User License Agreement");
	/// wizard.AddStep(firstStep);
	/// firstStep.NextButtonText = "Accept!";
	/// firstStep.HelpText = "This is the End User License Agreement.";
	/// 
	/// // Add 2nd step
	/// var secondStep = new Wizard.WizardStep ("Second Step");
	/// wizard.AddStep(secondStep);
	/// secondStep.HelpText = "This is the help text for the Second Step.";
	/// var lbl = new Label ("Name:") { AutoSize = true };
	/// secondStep.Add(lbl);
	/// 
	/// var name = new TextField () { X = Pos.Right (lbl) + 1, Width = Dim.Fill () - 1 };
	/// secondStep.Add(name);
	/// 
	/// wizard.Finished += (args) =>
	/// {
	///     MessageBox.Query("Wizard", $"Finished. The Name entered is '{name.Text}'", "Ok");
	///     Application.RequestStop();
	/// };
	/// 
	/// Application.Top.Add (wizard);
	/// Application.Run ();
	/// Application.Shutdown ();
	/// </code>
	/// </example>
	public class Wizard : Dialog {
		/// <summary>
		/// Represents a basic step that is displayed in a <see cref="Wizard"/>. The <see cref="WizardStep"/> view is divided horizontally in two. On the left is the
		/// content view where <see cref="View"/>s can be added,  On the right is the help for the step.
		/// Set <see cref="WizardStep.HelpText"/> to set the help text. If the help text is empty the help pane will not
		/// be shown. 
		/// 
		/// If there are no Views added to the WizardStep the <see cref="HelpText"/> (if not empty) will fill the wizard step. 
		/// </summary>
		/// <remarks>
		/// If <see cref="Button"/>s are added, do not set <see cref="Button.IsDefault"/> to true as this will conflict
		/// with the Next button of the Wizard.
		/// 
		/// Subscribe to the <see cref="View.VisibleChanged"/> event to be notified when the step is active; see also: <see cref="Wizard.StepChanged"/>.
		/// 
		/// To enable or disable a step from being shown to the user, set <see cref="View.Enabled"/>.
		/// 
		/// </remarks>
		public class WizardStep : FrameView {
			/// <summary>
			/// The title of the <see cref="WizardStep"/>. 
			/// </summary>
			/// <remarks>The Title is only displayed when the <see cref="Wizard"/> is used as a modal pop-up (see <see cref="Wizard.Modal"/>.</remarks>
			public new ustring Title {
				get => title;
				set {
					if (!OnTitleChanging (title, value)) {
						var old = title;
						title = value;
						OnTitleChanged (old, title);
					}
					base.Title = value;
					SetNeedsDisplay ();
				}
			}

			private ustring title = ustring.Empty;

			/// <summary>
			/// An <see cref="EventArgs"/> which allows passing a cancelable new <see cref="Title"/> value event.
			/// </summary>
			public class TitleEventArgs : EventArgs {
				/// <summary>
				/// The new Window Title.
				/// </summary>
				public ustring NewTitle { get; set; }

				/// <summary>
				/// The old Window Title.
				/// </summary>
				public ustring OldTitle { get; set; }

				/// <summary>
				/// Flag which allows cancelling the Title change.
				/// </summary>
				public bool Cancel { get; set; }

				/// <summary>
				/// Initializes a new instance of <see cref="TitleEventArgs"/>
				/// </summary>
				/// <param name="oldTitle">The <see cref="Title"/> that is/has been replaced.</param>
				/// <param name="newTitle">The new <see cref="Title"/> to be replaced.</param>
				public TitleEventArgs (ustring oldTitle, ustring newTitle)
				{
					OldTitle = oldTitle;
					NewTitle = newTitle;
				}
			}

			/// <summary>
			/// Called before the <see cref="Title"/> changes. Invokes the <see cref="TitleChanging"/> event, which can be cancelled.
			/// </summary>
			/// <param name="oldTitle">The <see cref="Title"/> that is/has been replaced.</param>
			/// <param name="newTitle">The new <see cref="Title"/> to be replaced.</param>
			/// <returns><c>true</c> if an event handler cancelled the Title change.</returns>
			public virtual bool OnTitleChanging (ustring oldTitle, ustring newTitle)
			{
				var args = new TitleEventArgs (oldTitle, newTitle);
				TitleChanging?.Invoke (args);
				return args.Cancel;
			}

			/// <summary>
			/// Event fired when the <see cref="Title"/> is changing. Set <see cref="TitleEventArgs.Cancel"/> to 
			/// <c>true</c> to cancel the Title change.
			/// </summary>
			public event Action<TitleEventArgs> TitleChanging;

			/// <summary>
			/// Called when the <see cref="Title"/> has been changed. Invokes the <see cref="TitleChanged"/> event.
			/// </summary>
			/// <param name="oldTitle">The <see cref="Title"/> that is/has been replaced.</param>
			/// <param name="newTitle">The new <see cref="Title"/> to be replaced.</param>
			public virtual void OnTitleChanged (ustring oldTitle, ustring newTitle)
			{
				var args = new TitleEventArgs (oldTitle, newTitle);
				TitleChanged?.Invoke (args);
			}

			/// <summary>
			/// Event fired after the <see cref="Title"/> has been changed. 
			/// </summary>
			public event Action<TitleEventArgs> TitleChanged;

			// The contentView works like the ContentView in FrameView.
			private View contentView = new View ();

			/// <summary>
			/// Sets or gets help text for the <see cref="WizardStep"/>.If <see cref="WizardStep.HelpText"/> is empty
			/// the help pane will not be visible and the content will fill the entire WizardStep.
			/// </summary>
			/// <remarks>The help text is displayed using a read-only <see cref="TextView"/>.</remarks>
			public ustring HelpText {
				get => helpTextView.Text;
				set {
					helpTextView.Text = value;
					ShowHide ();
					SetNeedsDisplay ();
				}
			}
			private TextView helpTextView = new TextView ();

			/// <summary>
			/// Sets or gets the text for the back button. The back button will only be visible on 
			/// steps after the first step.
			/// </summary>
			/// <remarks>The default text is "Back"</remarks>
			public ustring BackButtonText { get; set; } = ustring.Empty;

			/// <summary>
			/// Sets or gets the text for the next/finish button.
			/// </summary>
			/// <remarks>The default text is "Next..." if the Pane is not the last pane. Otherwise it is "Finish"</remarks>
			public ustring NextButtonText { get; set; } = ustring.Empty;

			/// <summary>
			/// Initializes a new instance of the <see cref="Wizard"/> class using <see cref="LayoutStyle.Computed"/> positioning.
			/// </summary>
			/// <param name="title">Title for the Step. Will be appended to the containing Wizard's title as 
			/// "Wizard Title - Wizard Step Title" when this step is active.</param>
			/// <remarks>
			/// </remarks>
			public WizardStep (ustring title)
			{
				this.Title = title; // this.Title holds just the "Wizard Title"; base.Title holds "Wizard Title - Step Title"
				this.Border.BorderStyle = BorderStyle.Rounded;

				base.Add (contentView);

				helpTextView.ColorScheme = new ColorScheme () {  
					Normal = new Attribute(Color.Gray, Color.DarkGray)
				};
				helpTextView.ReadOnly = true;
				helpTextView.WordWrap = true;
				base.Add (helpTextView);

				ShowHide ();

				var scrollBar = new ScrollBarView (helpTextView, true);

				scrollBar.ChangedPosition += () => {
					helpTextView.TopRow = scrollBar.Position;
					if (helpTextView.TopRow != scrollBar.Position) {
						scrollBar.Position = helpTextView.TopRow;
					}
					helpTextView.SetNeedsDisplay ();
				};

				scrollBar.OtherScrollBarView.ChangedPosition += () => {
					helpTextView.LeftColumn = scrollBar.OtherScrollBarView.Position;
					if (helpTextView.LeftColumn != scrollBar.OtherScrollBarView.Position) {
						scrollBar.OtherScrollBarView.Position = helpTextView.LeftColumn;
					}
					helpTextView.SetNeedsDisplay ();
				};

				scrollBar.VisibleChanged += () => {
					if (scrollBar.Visible && helpTextView.RightOffset == 0) {
						helpTextView.RightOffset = 1;
					} else if (!scrollBar.Visible && helpTextView.RightOffset == 1) {
						helpTextView.RightOffset = 0;
					}
				};

				scrollBar.OtherScrollBarView.VisibleChanged += () => {
					if (scrollBar.OtherScrollBarView.Visible && helpTextView.BottomOffset == 0) {
						helpTextView.BottomOffset = 1;
					} else if (!scrollBar.OtherScrollBarView.Visible && helpTextView.BottomOffset == 1) {
						helpTextView.BottomOffset = 0;
					}
				};

				helpTextView.DrawContent += (e) => {
					scrollBar.Size = helpTextView.Lines;
					scrollBar.Position = helpTextView.TopRow;
					if (scrollBar.OtherScrollBarView != null) {
						scrollBar.OtherScrollBarView.Size = helpTextView.Maxlength;
						scrollBar.OtherScrollBarView.Position = helpTextView.LeftColumn;
					}
					scrollBar.LayoutSubviews ();
					scrollBar.Refresh ();
				};
				base.Add (scrollBar);
			}

			/// <summary>
			/// Does the work to show and hide the contentView and helpView as appropriate
			/// </summary>
			internal void ShowHide ()
			{
				contentView.Height = Dim.Fill ();
				helpTextView.Height = Dim.Fill ();
				helpTextView.Width = Dim.Fill ();

				if (contentView.InternalSubviews?.Count > 0) {
					if (helpTextView.Text.Length > 0) {
						contentView.Width = Dim.Percent (70);
						helpTextView.X = Pos.Right (contentView);
						helpTextView.Width = Dim.Fill ();

					} else {
						contentView.Width = Dim.Percent (100);
					}
				} else {
					if (helpTextView.Text.Length > 0) {
						helpTextView.X = 0;
					} else {
						// Error - no pane shown
					}

				}
				contentView.Visible = contentView.InternalSubviews?.Count > 0;
				helpTextView.Visible = helpTextView.Text.Length > 0;
			}

			/// <summary>
			/// Add the specified <see cref="View"/> to the <see cref="WizardStep"/>. 
			/// </summary>
			/// <param name="view"><see cref="View"/> to add to this container</param>
			public override void Add (View view)
			{
				contentView.Add (view);
				if (view.CanFocus)
					CanFocus = true;
				ShowHide ();
			}

			/// <summary>
			///   Removes a <see cref="View"/> from <see cref="WizardStep"/>.
			/// </summary>
			/// <remarks>
			/// </remarks>
			public override void Remove (View view)
			{
				if (view == null)
					return;

				SetNeedsDisplay ();
				var touched = view.Frame;
				contentView.Remove (view);

				if (contentView.InternalSubviews.Count < 1)
					this.CanFocus = false;
				ShowHide ();
			}

			/// <summary>
			///   Removes all <see cref="View"/>s from the <see cref="WizardStep"/>.
			/// </summary>
			/// <remarks>
			/// </remarks>
			public override void RemoveAll ()
			{
				contentView.RemoveAll ();
				ShowHide ();
			}

		} // end of WizardStep class

		/// <summary>
		/// Initializes a new instance of the <see cref="Wizard"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <remarks>
		/// The Wizard will be vertically and horizontally centered in the container.
		/// After initialization use <c>X</c>, <c>Y</c>, <c>Width</c>, and <c>Height</c> change size and position.
		/// </remarks>
		public Wizard () : this (ustring.Empty)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Wizard"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="title">Sets the <see cref="Title"/> for the Wizard.</param>
		/// <remarks>
		/// The Wizard will be vertically and horizontally centered in the container.
		/// After initialization use <c>X</c>, <c>Y</c>, <c>Width</c>, and <c>Height</c> change size and position.
		/// </remarks>
		public Wizard (ustring title) : base (title)
		{
			wizardTitle = title;
			// Using Justify causes the Back and Next buttons to be hard justified against
			// the left and right edge
			ButtonAlignment = ButtonAlignments.Justify;
			this.Border.BorderStyle = BorderStyle.Double;
			this.Border.Padding = new Thickness (0);

			//// Add a horiz separator
			//var separator = new LineView (Graphs.Orientation.Horizontal) {
			//	Y = Pos.AnchorEnd (2)
			//};
			//Add (separator);

			// BUGBUG: Space is to work around https://github.com/gui-cs/Terminal.Gui/issues/1812
			backBtn = new Button (Strings.wzBack) { AutoSize = true };
			AddButton (backBtn);

			nextfinishBtn = new Button (Strings.wzFinish) { AutoSize = true };
			nextfinishBtn.IsDefault = true;
			AddButton (nextfinishBtn);

			backBtn.Clicked += BackBtn_Clicked;
			nextfinishBtn.Clicked += NextfinishBtn_Clicked;

			Loaded += Wizard_Loaded;
			Closing += Wizard_Closing;

			if (Modal) {
				ClearKeybinding (Command.QuitToplevel);
				AddKeyBinding (Key.Esc, Command.QuitToplevel);
			}

		}

		private void Wizard_Loaded ()
		{
			CurrentStep = GetFirstStep (); // gets the first step if CurrentStep == null
		}

		private bool finishedPressed = false;

		private void Wizard_Closing (ToplevelClosingEventArgs obj)
		{
			if (!finishedPressed) {
				var args = new WizardButtonEventArgs ();
				Cancelled?.Invoke (args);
			}
		}

		private void NextfinishBtn_Clicked ()
		{
			if (CurrentStep == GetLastStep ()) {
				var args = new WizardButtonEventArgs ();
				Finished?.Invoke (args);
				if (!args.Cancel) {
					finishedPressed = true;
					if (IsCurrentTop) {
						Application.RequestStop (this);
					} else {
						// Wizard was created as a non-modal (just added to another View). 
						// Do nothing
					}
				}
			} else {
				var args = new WizardButtonEventArgs ();
				MovingNext?.Invoke (args);
				if (!args.Cancel) {
					GoNext ();
				}
			}
		}

		/// <summary>
		/// <see cref="Wizard"/> is derived from <see cref="Dialog"/> and Dialog causes <c>Esc</c> to call
		/// <see cref="Application.RequestStop(Toplevel)"/>, closing the Dialog. Wizard overrides <see cref="Responder.ProcessKey(KeyEvent)"/>
		/// to instead fire the <see cref="Cancelled"/> event when Wizard is being used as a non-modal (see <see cref="Wizard.Modal"/>.
		/// See <see cref="Responder.ProcessKey(KeyEvent)"/> for more.
		/// </summary>
		/// <param name="kb"></param>
		/// <returns></returns>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (!Modal) {
				switch (kb.Key) {
				case Key.Esc:
					var args = new WizardButtonEventArgs ();
					Cancelled?.Invoke (args);
					return false;
				}
			}
			return base.ProcessKey (kb);
		}

		/// <summary>
		/// Causes the wizad to move to the next enabled step (or last step if <see cref="CurrentStep"/> is not set). 
		/// If there is no previous step, does nothing.
		/// </summary>
		public void GoNext ()
		{
			var nextStep = GetNextStep ();
			if (nextStep != null) {
				GoToStep (nextStep);
			}
		}

		/// <summary>
		/// Returns the next enabled <see cref="WizardStep"/> after the current step. Takes into account steps which
		/// are disabled. If <see cref="CurrentStep"/> is <c>null</c> returns the first enabled step.
		/// </summary>
		/// <returns>The next step after the current step, if there is one; otherwise returns <c>null</c>, which 
		/// indicates either there are no enabled steps or the current step is the last enabled step.</returns>
		public WizardStep GetNextStep ()
		{
			LinkedListNode<WizardStep> step = null;
			if (CurrentStep == null) {
				// Get first step, assume it is next
				step = steps.First;
			} else {
				// Get the step after current
				step = steps.Find (CurrentStep);
				if (step != null) {
					step = step.Next;
				}
			}

			// step now points to the potential next step
			while (step != null) {
				if (step.Value.Enabled) {
					return step.Value;
				}
				step = step.Next;
			}
			return null;
		}

		private void BackBtn_Clicked ()
		{
			var args = new WizardButtonEventArgs ();
			MovingBack?.Invoke (args);
			if (!args.Cancel) {
				GoBack ();
			}
		}

		/// <summary>
		/// Causes the wizad to move to the previous enabled step (or first step if <see cref="CurrentStep"/> is not set). 
		/// If there is no previous step, does nothing.
		/// </summary>
		public void GoBack ()
		{
			var previous = GetPreviousStep ();
			if (previous != null) {
				GoToStep (previous);
			}
		}

		/// <summary>
		/// Returns the first enabled <see cref="WizardStep"/> before the current step. Takes into account steps which
		/// are disabled. If <see cref="CurrentStep"/> is <c>null</c> returns the last enabled step.
		/// </summary>
		/// <returns>The first step ahead of the current step, if there is one; otherwise returns <c>null</c>, which 
		/// indicates either there are no enabled steps or the current step is the first enabled step.</returns>
		public WizardStep GetPreviousStep ()
		{
			LinkedListNode<WizardStep> step = null;
			if (CurrentStep == null) {
				// Get last step, assume it is previous
				step = steps.Last;
			} else {
				// Get the step before current
				step = steps.Find (CurrentStep);
				if (step != null) {
					step = step.Previous;
				}
			}

			// step now points to the potential previous step
			while (step != null) {
				if (step.Value.Enabled) {
					return step.Value;
				}
				step = step.Previous;
			}
			return null;
		}

		/// <summary>
		/// Returns the first enabled step in the Wizard
		/// </summary>
		/// <returns>The last enabled step</returns>
		public WizardStep GetFirstStep ()
		{
			return steps.FirstOrDefault (s => s.Enabled);
		}

		/// <summary>
		/// Returns the last enabled step in the Wizard
		/// </summary>
		/// <returns>The last enabled step</returns>
		public WizardStep GetLastStep ()
		{
			return steps.LastOrDefault (s => s.Enabled);
		}

		private LinkedList<WizardStep> steps = new LinkedList<WizardStep> ();
		private WizardStep currentStep = null;

		/// <summary>
		/// If the <see cref="CurrentStep"/> is not the first step in the wizard, this button causes
		/// the <see cref="MovingBack"/> event to be fired and the wizard moves to the previous step. 
		/// </summary>
		/// <remarks>
		/// Use the <see cref="MovingBack"></see> event to be notified when the user attempts to go back.
		/// </remarks>
		public Button BackButton { get => backBtn; }
		private Button backBtn;

		/// <summary>
		/// If the <see cref="CurrentStep"/> is the last step in the wizard, this button causes
		/// the <see cref="Finished"/> event to be fired and the wizard to close. If the step is not the last step,
		/// the <see cref="MovingNext"/> event will be fired and the wizard will move next step. 
		/// </summary>
		/// <remarks>
		/// Use the <see cref="MovingNext"></see> and <see cref="Finished"></see> events to be notified 
		/// when the user attempts go to the next step or finish the wizard.
		/// </remarks>
		public Button NextFinishButton { get => nextfinishBtn; }
		private Button nextfinishBtn;

		/// <summary>
		/// Adds a step to the wizard. The Next and Back buttons navigate through the added steps in the
		/// order they were added.
		/// </summary>
		/// <param name="newStep"></param>
		/// <remarks>The "Next..." button of the last step added will read "Finish" (unless changed from default).</remarks>
		public void AddStep (WizardStep newStep)
		{
			SizeStep (newStep);

			newStep.EnabledChanged += UpdateButtonsAndTitle;
			newStep.TitleChanged += (args) => UpdateButtonsAndTitle ();
			steps.AddLast (newStep);
			this.Add (newStep);
			UpdateButtonsAndTitle ();
		}

		/// <summary>
		/// The title of the Wizard, shown at the top of the Wizard with " - currentStep.Title" appended.
		/// </summary>
		/// <remarks>
		/// The Title is only displayed when the <see cref="Wizard"/> <see cref="Wizard.Modal"/> is set to <c>false</c>.
		/// </remarks>
		public new ustring Title {
			get {
				// The base (Dialog) Title holds the full title ("Wizard Title - Step Title")
				return base.Title;
			}
			set {
				wizardTitle = value;
				base.Title = $"{wizardTitle}{(steps.Count > 0 && currentStep != null ? " - " + currentStep.Title : string.Empty)}";
			}
		}
		private ustring wizardTitle = ustring.Empty;

		/// <summary>	
		/// <see cref="EventArgs"/> for <see cref="WizardStep"/> transition events.
		/// </summary>
		public class WizardButtonEventArgs : EventArgs {
			/// <summary>
			/// Set to true to cancel the transition to the next step.
			/// </summary>
			public bool Cancel { get; set; }

			/// <summary>
			/// Initializes a new instance of <see cref="WizardButtonEventArgs"/>
			/// </summary>
			public WizardButtonEventArgs ()
			{
				Cancel = false;
			}
		}

		/// <summary>
		/// Raised when the Back button in the <see cref="Wizard"/> is clicked. The Back button is always
		/// the first button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any.
		/// </summary>
		public event Action<WizardButtonEventArgs> MovingBack;

		/// <summary>
		/// Raised when the Next/Finish button in the <see cref="Wizard"/> is clicked (or the user presses Enter). 
		/// The Next/Finish button is always the last button in the array of Buttons passed to the <see cref="Wizard"/> constructor, 
		/// if any. This event is only raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow 
		/// (otherwise the <see cref="Finished"/> event is raised).
		/// </summary>
		public event Action<WizardButtonEventArgs> MovingNext;

		/// <summary>
		/// Raised when the Next/Finish button in the <see cref="Wizard"/> is clicked. The Next/Finish button is always
		/// the last button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any. This event is only
		/// raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow 
		/// (otherwise the <see cref="Finished"/> event is raised).
		/// </summary>
		public event Action<WizardButtonEventArgs> Finished;

		/// <summary>
		/// Raised when the user has cancelled the <see cref="Wizard"/> by pressin the Esc key. 
		/// To prevent a modal (<see cref="Wizard.Modal"/> is <c>true</c>) Wizard from
		/// closing, cancel the event by setting <see cref="WizardButtonEventArgs.Cancel"/> to 
		/// <c>true</c> before returning from the event handler.
		/// </summary>
		public event Action<WizardButtonEventArgs> Cancelled;

		/// <summary>
		/// <see cref="EventArgs"/> for <see cref="WizardStep"/> events.
		/// </summary>
		public class StepChangeEventArgs : EventArgs {
			/// <summary>
			/// The current (or previous) <see cref="WizardStep"/>.
			/// </summary>
			public WizardStep OldStep { get; }

			/// <summary>
			/// The <see cref="WizardStep"/> the <see cref="Wizard"/> is changing to or has changed to.
			/// </summary>
			public WizardStep NewStep { get; }

			/// <summary>
			/// Event handlers can set to true before returning to cancel the step transition.
			/// </summary>
			public bool Cancel { get; set; }

			/// <summary>
			/// Initializes a new instance of <see cref="StepChangeEventArgs"/>
			/// </summary>
			/// <param name="oldStep">The current <see cref="WizardStep"/>.</param>
			/// <param name="newStep">The new <see cref="WizardStep"/>.</param>
			public StepChangeEventArgs (WizardStep oldStep, WizardStep newStep)
			{
				OldStep = oldStep;
				NewStep = newStep;
				Cancel = false;
			}
		}

		/// <summary>
		/// This event is raised when the current <see cref="CurrentStep"/>) is about to change. Use <see cref="StepChangeEventArgs.Cancel"/> 
		/// to abort the transition.
		/// </summary>
		public event Action<StepChangeEventArgs> StepChanging;

		/// <summary>
		/// This event is raised after the <see cref="Wizard"/> has changed the <see cref="CurrentStep"/>. 
		/// </summary>
		public event Action<StepChangeEventArgs> StepChanged;

		/// <summary>
		/// Gets or sets the currently active <see cref="WizardStep"/>.
		/// </summary>
		public WizardStep CurrentStep {
			get => currentStep;
			set {
				GoToStep (value);
			}
		}

		/// <summary>
		/// Called when the <see cref="Wizard"/> is about to transition to another <see cref="WizardStep"/>. Fires the <see cref="StepChanging"/> event. 
		/// </summary>
		/// <param name="oldStep">The step the Wizard is about to change from</param>
		/// <param name="newStep">The step the Wizard is about to change to</param>
		/// <returns>True if the change is to be cancelled.</returns>
		public virtual bool OnStepChanging (WizardStep oldStep, WizardStep newStep)
		{
			var args = new StepChangeEventArgs (oldStep, newStep);
			StepChanging?.Invoke (args);
			return args.Cancel;
		}

		/// <summary>
		/// Called when the <see cref="Wizard"/> has completed transition to a new <see cref="WizardStep"/>. Fires the <see cref="StepChanged"/> event. 
		/// </summary>
		/// <param name="oldStep">The step the Wizard changed from</param>
		/// <param name="newStep">The step the Wizard has changed to</param>
		/// <returns>True if the change is to be cancelled.</returns>
		public virtual bool OnStepChanged (WizardStep oldStep, WizardStep newStep)
		{
			var args = new StepChangeEventArgs (oldStep, newStep);
			StepChanged?.Invoke (args);
			return args.Cancel;
		}

		/// <summary>
		/// Changes to the specified <see cref="WizardStep"/>.
		/// </summary>
		/// <param name="newStep">The step to go to.</param>
		/// <returns>True if the transition to the step succeeded. False if the step was not found or the operation was cancelled.</returns>
		public bool GoToStep (WizardStep newStep)
		{
			if (OnStepChanging (currentStep, newStep) || (newStep != null && !newStep.Enabled)) {
				return false;
			}

			// Hide all but the new step
			foreach (WizardStep step in steps) {
				step.Visible = (step == newStep);
				step.ShowHide ();
			}

			var oldStep = currentStep;
			currentStep = newStep;

			UpdateButtonsAndTitle ();

			// Set focus to the nav buttons
			if (backBtn.HasFocus) {
				backBtn.SetFocus ();
			} else {
				nextfinishBtn.SetFocus ();
			}

			if (OnStepChanged (oldStep, currentStep)) {
				// For correctness we do this, but it's meaningless because there's nothing to cancel
				return false;
			}

			return true;
		}

		private void UpdateButtonsAndTitle ()
		{
			if (CurrentStep == null) return;

			base.Title = $"{wizardTitle}{(steps.Count > 0 ? " - " + CurrentStep.Title : string.Empty)}";

			// Configure the Back button
			backBtn.Text = CurrentStep.BackButtonText != ustring.Empty ? CurrentStep.BackButtonText : Strings.wzBack; // "_Back";
			backBtn.Visible = (CurrentStep != GetFirstStep ());

			// Configure the Next/Finished button
			if (CurrentStep == GetLastStep ()) {
				nextfinishBtn.Text = CurrentStep.NextButtonText != ustring.Empty ? CurrentStep.NextButtonText : Strings.wzFinish; // "Fi_nish";
			} else {
				nextfinishBtn.Text = CurrentStep.NextButtonText != ustring.Empty ? CurrentStep.NextButtonText : Strings.wzNext; // "_Next...";
			}

			SizeStep (CurrentStep);

			SetNeedsLayout ();
			LayoutSubviews ();
			Redraw (Bounds);
		}

		private void SizeStep (WizardStep step)
		{
			if (Modal) {
				// If we're modal, then we expand the WizardStep so that the top and side 
				// borders and not visible. The bottom border is the separator above the buttons.
				step.X = step.Y = -1;
				step.Height = Dim.Fill (1); // for button frame
				step.Width = Dim.Fill (-1);
			} else {
				// If we're not a modal, then we show the border around the WizardStep
				step.X = step.Y = 0;
				step.Height = Dim.Fill (1); // for button frame
				step.Width = Dim.Fill (0);
			}
		}

		/// <summary>
		/// Determines whether the <see cref="Wizard"/> is displayed as modal pop-up or not.
		/// 
		/// The default is <c>true</c>. The Wizard will be shown with a frame with <see cref="Title"/> and will behave like
		/// any <see cref="Toplevel"/> window.
		/// 
		/// If set to <c>false</c> the Wizard will have no frame and will behave like any embedded <see cref="View"/>.
		/// 
		/// To use Wizard as an embedded View 
		/// <list type="number">
		/// <item><description>Set <see cref="Modal"/> to <c>false</c>.</description></item>
		/// <item><description>Add the Wizard to a containing view with <see cref="View.Add(View)"/>.</description></item>
		/// </list>
		/// 
		/// If a non-Modal Wizard is added to the application after <see cref="Application.Run(Func{Exception, bool})"/> has been called
		/// the first step must be explicitly set by setting <see cref="CurrentStep"/> to <see cref="GetNextStep()"/>:
		/// <code>
		///    wizard.CurrentStep = wizard.GetNextStep();
		/// </code>
		/// </summary>
		public new bool Modal {
			get => base.Modal;
			set {
				base.Modal = value;
				foreach (var step in steps) {
					SizeStep (step);
				}
				if (base.Modal) {
					ColorScheme = Colors.Dialog;
					Border.BorderStyle = BorderStyle.Rounded;
					Border.Effect3D = true;
					Border.DrawMarginFrame = true;
				} else {
					if (SuperView != null) {
						ColorScheme = SuperView.ColorScheme;
					} else {
						ColorScheme = Colors.Base;
					}
					CanFocus = true;
					Border.Effect3D = false;
					Border.BorderStyle = BorderStyle.None;
					Border.DrawMarginFrame = false;
				}
			}
		}
	}
}