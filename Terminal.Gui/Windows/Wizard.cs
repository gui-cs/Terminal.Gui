using System;
using System.Collections.Generic;
using NStack;
using Terminal.Gui.Resources;

namespace Terminal.Gui {
	/// <summary>
	/// Provides a step-based "wizard" UI. The Wizard supports multiple steps. Each step (<see cref="WizardStep"/>) can host 
	/// arbitrary <see cref="View"/>s, much like a <see cref="Dialog"/>. Each step also has a pane for help text. Along the
	/// bottom of the Wizard view are customizable buttons enabling the user to navigate forward and backward through the Wizard. 
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class Wizard : Dialog {

		/// <summary>
		/// One step for the Wizard. The <see cref="WizardStep"/> view hosts two sub-views: 1) add <see cref="View"/>s to <see cref="WizardStep.Controls"/>, 
		/// 2) use <see cref="WizardStep.HelpText"/> to set the contents of the <see cref="TextView"/> that shows on the
		/// right side. Use <see cref="WizardStep.showControls"/> and <see cref="WizardStep.showHelp"/> to 
		/// control wether the control or help pane are shown. 
		/// </summary>
		/// <remarks>
		/// If <see cref="Button"/>s are added, do not set <see cref="Button.IsDefault"/> to true as this will conflict
		/// with the Next button of the Wizard.
		/// </remarks>
		public class WizardStep : View {
			/// <summary>
			/// The title of the <see cref="WizardStep"/>.
			/// </summary>
			public ustring Title { get => title; set => title = value; }
			// TODO: Update Wizard title when step title is changed if step is current - this will require step to slueth it's parent 
			private ustring title;

			// The controlPane is a separate view, so when devs add controls to the Step and help is visible, Y = Pos.AnchorEnd()
			// will work as expected.
			private View controlPane = new FrameView ();

			/// <summary>
			/// THe pane that holds the controls for the <see cref="WizardStep"/>. Use <see cref="WizardStep.Controls"/> `Add(View`) to add 
			/// controls. Note that the Controls view is sized to take 70% of the Wizard's width and the <see cref="WizardStep.HelpText"/> 
			/// takes the other 30%. This can be adjusted by setting `Width` from `Dim.Percent(70)` to 
			/// another value. If <see cref="WizardStep.ShowHelp"/> is set to `false` the control pane will fill the entire 
			/// Wizard.
			/// </summary>
			public View Controls { get => controlPane; }

			/// <summary>
			/// Sets or gets help text for the <see cref="WizardStep"/>.If <see cref="WizardStep.ShowHelp"/> is set to 
			/// `false` the control pane will fill the entire wizard.
			/// </summary>
			/// <remarks>The help text is displayed using a read-only <see cref="TextView"/>.</remarks>
			public ustring HelpText { get => helpTextView.Text; set => helpTextView.Text = value; }
			private TextView helpTextView = new TextView ();

			/// <summary>
			/// Sets or gets the text for the back button. The back button will only be visible on 
			/// steps after the first step.
			/// </summary>
			/// <remarks>The default text is "Back"</remarks>
			public ustring BackButtonText { get; set; } = ustring.Empty;
			// TODO: Update button text of Wizard button when step's button text is changed if step is current - this will require step to slueth it's parent 

			/// <summary>
			/// Sets or gets the text for the next/finish button.
			/// </summary>
			/// <remarks>The default text is "Next..." if the Pane is not the last pane. Otherwise it is "Finish"</remarks>
			public ustring NextButtonText { get; set; } = ustring.Empty;
			// TODO: Update button text of Wizard button when step's button text is changed if step is current - this will require step to slueth it's parent 

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
				this.ColorScheme = Colors.Dialog;

				Y = 0;
				Height = Dim.Fill (1); // for button frame
				Width = Dim.Fill ();

				Controls.ColorScheme = Colors.Dialog;
				Controls.Border.BorderStyle = BorderStyle.None;
				Controls.Border.Padding = new Thickness (0);
				Controls.Border.BorderThickness = new Thickness (0);
				this.Add (Controls);

				helpTextView.ColorScheme = Colors.Menu;
				helpTextView.Y = 0;
				helpTextView.ReadOnly = true;
				helpTextView.WordWrap = true;
				this.Add (helpTextView);
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
				this.Add (scrollBar);
			}

			/// <summary>
			/// If true (the default) the help will be visible. If false, the help will not be shown and the control pane will
			/// fill the wizard step.
			/// </summary>
			public bool ShowHelp {
				get => showHelp;
				set {
					showHelp = value;
					ShowHide ();
				}
			}
			private bool showHelp = true;

			/// <summary>
			/// If true (the default) the <see cref="Controls"/> View will be visible. If false, the controls will not be shown and the help will
			/// fill the wizard step.
			/// </summary>
			public bool ShowControls {
				get => showControls;
				set {
					showControls = value;
					ShowHide ();
				}
			}
			private bool showControls = true;

			/// <summary>
			/// Does the work to show and hide the controls, help, and buttons as appropriate
			/// </summary>
			private void ShowHide ()
			{
				Controls.Height = Dim.Fill (1);
				helpTextView.Height = Dim.Fill (1);
				helpTextView.Width = Dim.Fill ();

				if (showControls) {
					if (showHelp) {
						Controls.Width = Dim.Percent (70);
						helpTextView.X = Pos.Right (Controls);
						helpTextView.Width = Dim.Fill ();

					} else {
						Controls.Width = Dim.Percent (100);
					}
				} else {
					if (showHelp) {
						helpTextView.X = 0;
					} else {
						// Error - no pane shown
					}

				}
				Controls.Visible = showControls;
				helpTextView.Visible = showHelp;
			}
		} // WizardStep

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
		/// <param name="title">Title for the Wizard.</param>
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

			// Add a horiz separator
			var separator = new LineView (Graphs.Orientation.Horizontal) {
				Y = Pos.AnchorEnd (2)
			};
			Add (separator);

			// BUGBUG: Space is to work around https://github.com/migueldeicaza/gui.cs/issues/1812
			backBtn = new Button (Strings.wzBack) { AutoSize = true };
			AddButton (backBtn);

			nextfinishBtn = new Button (Strings.wzFinish) { AutoSize = true };
			nextfinishBtn.IsDefault = true;
			AddButton (nextfinishBtn);

			backBtn.Clicked += BackBtn_Clicked;
			nextfinishBtn.Clicked += NextfinishBtn_Clicked;

			Loaded += Wizard_Loaded;
			Closing += Wizard_Closing;
		}

		private bool finishedPressed = false;

		private void Wizard_Closing (ToplevelClosingEventArgs obj)
		{
			if (!finishedPressed) {
				var args = new WizardButtonEventArgs ();
				Cancelled?.Invoke (args);
			}
		}

		private void Wizard_Loaded ()
		{
			foreach (var step in steps) {
				step.Y = 0;
			}
			if (steps.Count > 0) {
				CurrentStep = steps.First.Value;
			}
		}

		private void NextfinishBtn_Clicked ()
		{
			if (CurrentStep == steps.Last.Value) {
				var args = new WizardButtonEventArgs ();
				Finished?.Invoke (args);
				if (!args.Cancel) {
					finishedPressed = true;
					Application.RequestStop (this);
				}
			} else {
				var args = new WizardButtonEventArgs ();
				MovingNext?.Invoke (args);
				if (!args.Cancel) {
					var current = steps.Find (CurrentStep);
					if (current != null && current.Next != null) {
						GotoStep (current.Next.Value);
					}
				}
			}
		}

		private void BackBtn_Clicked ()
		{
			var args = new WizardButtonEventArgs ();
			MovingBack?.Invoke (args);
			if (!args.Cancel) {
				var current = steps.Find (CurrentStep);
				if (current != null && current.Previous != null) {
					GotoStep (current.Previous.Value);
				}
			}
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
			steps.AddLast (newStep);
			this.Add (newStep);
			SetNeedsLayout ();
		}

		/// <summary>
		/// The title of the Wizard, shown at the top of the Wizard with " - currentStep.Title" appended.
		/// </summary>
		public new ustring Title {
			get {
				// The base (Dialog) Title holds the full title ("Wizard Title - Step Title")
				return base.Title;
			}
			set {
				wizardTitle = value;
				base.Title = $"{wizardTitle}{(steps.Count > 0 ? " - " + currentStep.Title : string.Empty)}";
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
		/// This event is raised when the Back button in the <see cref="Wizard"/> is clicked. The Back button is always
		/// the first button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any.
		/// </summary>
		public event Action<WizardButtonEventArgs> MovingBack;

		/// <summary>
		/// This event is raised when the Next/Finish button in the <see cref="Wizard"/> is clicked. The Next/Finish button is always
		/// the last button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any. This event is only
		/// raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow 
		/// (otherwise the <see cref="Finished"/> event is raised).
		/// </summary>
		public event Action<WizardButtonEventArgs> MovingNext;

		/// <summary>
		/// This event is raised when the Next/Finish button in the <see cref="Wizard"/> is clicked. The Next/Finish button is always
		/// the last button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any. This event is only
		/// raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow 
		/// (otherwise the <see cref="Finished"/> event is raised).
		/// </summary>
		public event Action<WizardButtonEventArgs> Finished;


		/// <summary>
		/// This event is raised when the user has cancelled the <see cref="Wizard"/> (with Ctrl-Q or ESC).
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
				GotoStep (value);
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
		public bool GotoStep (WizardStep newStep)
		{
			if (OnStepChanging (currentStep, newStep)) {
				return false;
			}

			// Hide all but the new step
			foreach (WizardStep step in steps) {
				step.Visible = (step == newStep);
			}

			base.Title = $"{wizardTitle}{(steps.Count > 0 ? " - " + newStep.Title : string.Empty)}";

			// Configure the Back button
			backBtn.Text = newStep.BackButtonText != ustring.Empty ? newStep.BackButtonText : Strings.wzBack; // "_Back";
			backBtn.Visible = (newStep != steps.First.Value);

			// Configure the Next/Finished button
			if (newStep == steps.Last.Value) {
				nextfinishBtn.Text = newStep.NextButtonText != ustring.Empty ? newStep.NextButtonText : Strings.wzFinish; // "Fi_nish";
			} else {
				nextfinishBtn.Text = newStep.NextButtonText != ustring.Empty ? newStep.NextButtonText : Strings.wzNext; // "_Next...";
			}

			// Set focus to the nav buttons
			if (backBtn.HasFocus) {
				backBtn.SetFocus ();
			} else {
				nextfinishBtn.SetFocus ();
			}

			var oldStep = currentStep; 
			currentStep = newStep;

			LayoutSubviews ();
			Redraw (this.Bounds);

			if (OnStepChanged (oldStep, currentStep)) {
				// For correctness we do this, but it's meaningless because there's nothing to cancel
				return false;
			}

			return true;
		}
	}
}