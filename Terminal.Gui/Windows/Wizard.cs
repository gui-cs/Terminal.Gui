using System;
using System.Collections.Generic;
using NStack;

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
				this.ColorScheme = Colors.Menu;
		
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
						helpTextView.X = Pos.Right (Controls) ;
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
		}

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

			backBtn = new Button ("_Back") { AutoSize = true };
			AddButton (backBtn);

			nextfinishBtn = new Button ("_Next...") { AutoSize = true };
			nextfinishBtn.IsDefault = true;
			AddButton (nextfinishBtn);

			backBtn.Clicked += () => {
				var args = new WizardStepEventArgs ();
				MovingBack?.Invoke (args);
				if (!args.Cancel) {
					if (currentStep > 0) {
						CurrentStep--;
					}
				}
			};

			nextfinishBtn.Clicked += () => {
				if (currentStep == steps.Count - 1) {
					var args = new WizardStepEventArgs ();
					Finished?.Invoke (args);
					if (!args.Cancel) {
						Application.RequestStop (this);
					}
				} else {
					var args = new WizardStepEventArgs ();
					MovingNext?.Invoke (args);
					if (!args.Cancel) {
						CurrentStep++;
					}
				}
			};

			Loaded += () => {
				foreach (var step in steps) {
					step.Y = 0;
				}
				if (steps.Count > 0) {

					CurrentStep = 0;
				}
			};

		}

		private List<WizardStep> steps = new List<WizardStep> ();
		private int currentStep = 0;

		/// <summary>
		/// Adds a step to the wizard. The Next and Back buttons navigate through the added steps in the
		/// order they were added.
		/// </summary>
		/// <param name="newStep"></param>
		/// <remarks>The "Next..." button of the last step added will read "Finish" (unless changed from default).</remarks>
		public void AddStep (WizardStep newStep)
		{
			steps.Add (newStep);
			this.Add (newStep);
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
				base.Title = $"{wizardTitle}{(steps.Count > 0 ? " - " + steps [currentStep].Title : string.Empty)}";
			}
		}
		private ustring wizardTitle = ustring.Empty;

		/// <summary>	
		/// <see cref="EventArgs"/> for <see cref="WizardStep"/> transition events.
		/// </summary>
		public class WizardStepEventArgs : EventArgs {
			/// <summary>
			/// Set to true to cancel the transition to the next step.
			/// </summary>
			public bool Cancel { get; set; }

			/// <summary>
			/// Initializes a new instance of <see cref="WizardStepEventArgs"/>
			/// </summary>
			public WizardStepEventArgs ()
			{
				Cancel = false;
			}
		}

		/// <summary>
		/// This event is raised when the Back button in the <see cref="Wizard"/> is clicked. The Back button is always
		/// the first button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any.
		/// </summary>
		public event Action<WizardStepEventArgs> MovingBack;

		/// <summary>
		/// This event is raised when the Next/Finish button in the <see cref="Wizard"/> is clicked. The Next/Finish button is always
		/// the last button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any. This event is only
		/// raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow 
		/// (otherwise the <see cref="Finished"/> event is raised).
		/// </summary>
		public event Action<WizardStepEventArgs> MovingNext;

		/// <summary>
		/// This event is raised when the Next/Finish button in the <see cref="Wizard"/> is clicked. The Next/Finish button is always
		/// the last button in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any. This event is only
		/// raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow 
		/// (otherwise the <see cref="Finished"/> event is raised).
		/// </summary>
		public event Action<WizardStepEventArgs> Finished;

		/// <summary>
		/// This event is raised when the current step )<see cref="CurrentStep"/>) in the <see cref="Wizard"/> changes.
		/// </summary>
		public event Action<CurrentStepChangedEventArgs> CurrentStepChanged;

		/// <summary>
		/// <see cref="EventArgs"/> for <see cref="WizardStep"/> events.
		/// </summary>
		public class CurrentStepChangedEventArgs : EventArgs {
			/// <summary>
			/// The new current <see cref="WizardStep"/>.
			/// </summary>
			public int CurrentStepIndex { get; }

			/// <summary>
			/// Initializes a new instance of <see cref="CurrentStepChangedEventArgs"/>
			/// </summary>
			/// <param name="currentStepIndex">The new current <see cref="WizardStep"/>.</param>
			public CurrentStepChangedEventArgs (int currentStepIndex)
			{
				CurrentStepIndex = currentStepIndex;
			}
		}

		/// <summary>
		/// Gets or sets the currently active <see cref="WizardStep"/>.
		/// </summary>
		public int CurrentStep {
			get => currentStep;
			set {
				currentStep = value;
				OnCurrentStepChanged ();
			}
		}

		/// <summary>
		/// Called when the current <see cref="WizardStep"/> has changed (<see cref="CurrentStep"/>).
		/// </summary>
		public virtual void OnCurrentStepChanged ()
		{
			CurrentStepChanged?.Invoke (new CurrentStepChangedEventArgs (currentStep));
			// Hide all but the first step
			foreach (WizardStep step in steps) {
				step.Visible = (steps [currentStep] == step);
			}

			// TODO: Add support for "Wizard Title - Step Title"
			base.Title = $"{wizardTitle}{(steps.Count > 0 ? " - " + steps [currentStep].Title : string.Empty)}";

			backBtn.Text = steps [currentStep].BackButtonText != ustring.Empty ? steps [currentStep].BackButtonText : "_Back";
			if (currentStep == 0) {
				backBtn.Visible = false;
			} else {
				backBtn.Visible = true;
			}

			if (currentStep == steps.Count - 1) {
				nextfinishBtn.Text = steps [currentStep].NextButtonText != ustring.Empty ? steps [currentStep].NextButtonText : "Fi_nish";
			} else {
				nextfinishBtn.Text = steps [currentStep].NextButtonText != ustring.Empty ? steps [currentStep].NextButtonText : "_Next...";
			}
		}
	}
}