using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;
using Xunit.Abstractions;
using NStack;

namespace Terminal.Gui.Views {

	public class WizardTests {
		readonly ITestOutputHelper output;

		public WizardTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		private void RunButtonTestWizard (string title, int width, int height)
		{
			var wizard = new Wizard (title) { Width = width, Height = height };
			Application.End (Application.Begin (wizard));
		}

		// =========== WizardStep Tests
		[Fact, AutoInitShutdown]
		public void WizardStep_Title ()
		{
			// Verify default title

			// Verify set actually changes property

			// Verify set changes Wizard title (TODO: NOT YET IMPLEMENTED)
		}

		[Fact, AutoInitShutdown]
		public void WizardStep_ButtonText ()
		{
			// Verify default button text

			// Verify set actually changes property

			// Verify set actually changes buttons for the current step
		}

		// =========== Wizard Tests
		[Fact, AutoInitShutdown]
		public void DefaultConstructor_SizedProperly ()
		{
			var d = ((FakeDriver)Application.Driver);

			var wizard = new Wizard ();
			Assert.NotEqual (0, wizard.Width);
			Assert.NotEqual (0, wizard.Height);
		}

		[Fact, AutoInitShutdown]
		// Verify a zero-step wizard doesn't crash and shows a blank wizard
		// and that the title is correct
		public void ZeroStepWizard_Shows ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";
			var stepTitle = "";

			int width = 30;
			int height = 6;
			d.SetBufferSize (width, height);

			var btnBackText = "Back";
			var btnBack = $"{d.LeftBracket} {btnBackText} {d.RightBracket}";
			var btnNextText = "Finish";
			var btnNext = $"{d.LeftBracket}{d.LeftDefaultIndicator} {btnNextText} {d.RightDefaultIndicator}{d.RightBracket}";

			var topRow = $"{d.ULDCorner} {title}{stepTitle} {new String (d.HDLine.ToString () [0], width - title.Length - stepTitle.Length - 4)}{d.URDCorner}";
			var row2 = $"{d.VDLine}{new String (' ', width - 2)}{d.VDLine}";
			var row3 = row2;
			var separatorRow = $"{d.VDLine}{new String (d.HLine.ToString () [0], width - 2)}{d.VDLine}";
			var buttonRow = $"{d.VDLine}{btnBack}{new String (' ', width - btnBack.Length - btnNext.Length - 2)}{btnNext}{d.VDLine}";
			var bottomRow = $"{d.LLDCorner}{new String (d.HDLine.ToString () [0], width - 2)}{d.LRDCorner}";

			var wizard = new Wizard (title) { Width = width, Height = height };
			Application.End (Application.Begin (wizard));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{row2}\n{row3}\n{separatorRow}\n{buttonRow}\n{bottomRow}", output);
		}

		[Fact, AutoInitShutdown]
		// This test verifies that a single step wizard shows the correct buttons
		// and that the title is correct
		public void OneStepWizard_Shows ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";
			var stepTitle = "ABCD";

			int width = 30;
			int height = 7;
			d.SetBufferSize (width, height);

			var btnBackText = "Back";
			var btnBack = string.Empty; // $"{d.LeftBracket} {btnBackText} {d.RightBracket}";
			var btnNextText = "Finish"; // "Next";
			var btnNext = $"{d.LeftBracket}{d.LeftDefaultIndicator} {btnNextText} {d.RightDefaultIndicator}{d.RightBracket}";

			var topRow = $"{d.ULDCorner} {title} - {stepTitle} {new String (d.HDLine.ToString () [0], width - title.Length - stepTitle.Length - 7)}{d.URDCorner}";
			var row2 = $"{d.VDLine}{new String (' ', width - 2)}{d.VDLine}";
			var row3 = row2;
			var row4 = row3;
			var separatorRow = $"{d.VDLine}{new String (d.HLine.ToString () [0], width - 2)}{d.VDLine}";
			var buttonRow = $"{d.VDLine}{btnBack}{new String (' ', width - btnBack.Length - btnNext.Length - 2)}{btnNext}{d.VDLine}";
			var bottomRow = $"{d.LLDCorner}{new String (d.HDLine.ToString () [0], width - 2)}{d.LRDCorner}";

			var wizard = new Wizard (title) { Width = width, Height = height };
			wizard.AddStep (new Wizard.WizardStep (stepTitle));
			//wizard.LayoutSubviews ();
			var firstIteration = false;
			var runstate = Application.Begin (wizard);
			Application.RunMainLoopIteration (ref runstate, true, ref firstIteration);

			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{row2}\n{row3}\n{row4}\n{separatorRow}\n{buttonRow}\n{bottomRow}", output);
			Application.End (runstate);
		}

		[Fact, AutoInitShutdown]
		// This test verifies that the 2nd step in a wizard with 2 steps 
		// shows the correct buttons on both steps
		// and that the title is correct
		public void TwoStepWizard_Next_Shows_SecondStep ()
		{
			// verify step one

			// Next

			// verify step two

			// Back

			// verify step one again
		}

		[Fact, AutoInitShutdown]
		// This test verifies that the 2nd step in a wizard with more than 2 steps 
		// shows the correct buttons on all steps
		// and that the title is correct
		public void ThreeStepWizard_Next_Shows_Steps ()
		{

			// verify step one

			// Next

			// verify step two

			// Back

			// verify step one again
		}

		[Fact, AutoInitShutdown]
		// this test is needed because Wizard overrides Dialog's title behavior ("Title - StepTitle")
		public void Setting_Title_Works ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";
			var stepTitle = " - ABCD";

			int width = 40;
			int height = 4;
			d.SetBufferSize (width, height);

			var btnNextText = "Finish";
			var btnNext = $"{d.LeftBracket}{d.LeftDefaultIndicator} {btnNextText} {d.RightDefaultIndicator}{d.RightBracket}";

			var topRow = $"{d.ULDCorner} {title}{stepTitle} {new String (d.HDLine.ToString () [0], width - title.Length - stepTitle.Length - 4)}{d.URDCorner}";
			var separatorRow = $"{d.VDLine}{new String (d.HLine.ToString () [0], width - 2)}{d.VDLine}";

			// Once this is fixed, revert to commented out line: https://github.com/migueldeicaza/gui.cs/issues/1791
			var buttonRow = $"{d.VDLine}{new String (' ', width - btnNext.Length - 2)}{btnNext}{d.VDLine}";
			//var buttonRow = $"{d.VDLine}{new String (' ', width - btnNext.Length - 2)}{btnNext}{d.VDLine}";
			var bottomRow = $"{d.LLDCorner}{new String (d.HDLine.ToString () [0], width - 2)}{d.LRDCorner}";

			var wizard = new Wizard (title) { Width = width, Height = height };
			wizard.AddStep (new Wizard.WizardStep ("ABCD"));

			Application.End (Application.Begin (wizard));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{separatorRow}\n{buttonRow}\n{bottomRow}", output);
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GetPreviousStep_Correct ()
		{
			var wizard = new Wizard ();

			// If no steps should be null
			Assert.Null (wizard.GetPreviousStep ());

			var step1 = new Wizard.WizardStep ("step1");
			wizard.AddStep (step1);

			// If no current step, should be last step
			Assert.Equal (step1.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());

			wizard.CurrentStep = step1;
			// If there is 1 step it's current step should be null
			Assert.Null (wizard.GetPreviousStep ());

			// If one disabled step should be null
			step1.Enabled = false;
			Assert.Null (wizard.GetPreviousStep ());

			// If two steps and at 2 and step 1 is `Enabled = true`should be step1
			var step2 = new Wizard.WizardStep ("step2");
			wizard.AddStep (step2);
			wizard.CurrentStep = step2;
			step1.Enabled = true;
			Assert.Equal (step1.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());

			// If two steps and at 2 and step 1 is `Enabled = false` should be null
			step1.Enabled = false;
			Assert.Null (wizard.GetPreviousStep ());

			// If three steps with Step2.Enabled = true
			//   At step 1 should be null
			//   At step 2 should be step 1
			//   At step 3 should be step 2
			var step3 = new Wizard.WizardStep ("step3");
			wizard.AddStep (step3);
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Null (wizard.GetPreviousStep ());
			wizard.CurrentStep = step2;
			Assert.Equal (step1.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());
			wizard.CurrentStep = step3;
			Assert.Equal (step2.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());

			// If three steps with Step2.Enabled = false
			//   At step 1 should be null
			//   At step 3 should be step1
			step1.Enabled = true;
			step2.Enabled = false;
			step3.Enabled = true;
			wizard.CurrentStep = step1;
			Assert.Null (wizard.GetPreviousStep ());
			wizard.CurrentStep = step3;
			Assert.Equal (step1.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());

			// If three steps with Step1.Enabled = false & Step2.Enabled = false
			//   At step 3 should be null

			// If no current step, GetPreviousStep provides equivalent to GetLastStep
			wizard.CurrentStep = null;
			step1.Enabled = true;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step3.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());

			step1.Enabled = false;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step3.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());

			step1.Enabled = false;
			step2.Enabled = false;
			step3.Enabled = true;
			Assert.Equal (step3.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());

			step1.Enabled = false;
			step2.Enabled = true;
			step3.Enabled = false;
			Assert.Equal (step2.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());

			step1.Enabled = true;
			step2.Enabled = false;
			step3.Enabled = false;
			Assert.Equal (step1.Title.ToString(), wizard.GetPreviousStep ().Title.ToString());
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GetNextStep_Correct ()
		{
			var wizard = new Wizard ();

			// If no steps should be null
			Assert.Null (wizard.GetNextStep ());

			var step1 = new Wizard.WizardStep ("step1");
			wizard.AddStep (step1);

			// If no current step, should be first step
			Assert.Equal (step1.Title.ToString(), wizard.GetNextStep ().Title.ToString());

			wizard.CurrentStep = step1;
			// If there is 1 step it's current step should be null
			Assert.Null (wizard.GetNextStep ());

			// If one disabled step should be null
			step1.Enabled = false;
			Assert.Null (wizard.GetNextStep ());

			// If two steps and at 1 and step 2 is `Enabled = true`should be step 2
			var step2 = new Wizard.WizardStep ("step2");
			wizard.AddStep (step2);
			Assert.Equal (step2.Title.ToString(), wizard.GetNextStep ().Title.ToString());

			// If two steps and at 1 and step 2 is `Enabled = false` should be null
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = false;
			Assert.Null (wizard.GetNextStep ());

			// If three steps with Step2.Enabled = true
			//   At step 1 should be step 2
			//   At step 2 should be step 3
			//   At step 3 should be null
			var step3 = new Wizard.WizardStep ("step3");
			wizard.AddStep (step3);
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step2.Title.ToString(), wizard.GetNextStep ().Title.ToString());
			wizard.CurrentStep = step2;
			Assert.Equal (step3.Title.ToString(), wizard.GetNextStep ().Title.ToString());
			wizard.CurrentStep = step3;
			Assert.Null (wizard.GetNextStep ());

			// If three steps with Step2.Enabled = false
			//   At step 1 should be step 3
			//   At step 3 should be null
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = false;
			step3.Enabled = true;
			Assert.Equal (step3.Title.ToString(), wizard.GetNextStep ().Title.ToString());
			wizard.CurrentStep = step3;
			Assert.Null (wizard.GetNextStep ());

			// If three steps with Step2.Enabled = false & Step3.Enabled = false
			//   At step 1 should be null
			step1.Enabled = true;
			wizard.CurrentStep = step1;
			step2.Enabled = false;
			step3.Enabled = false;
			Assert.Null (wizard.GetNextStep ());

			// If no current step, GetNextStep provides equivalent to GetFirstStep
			wizard.CurrentStep = null;
			step1.Enabled = true;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step1.Title.ToString(), wizard.GetNextStep ().Title.ToString());

			step1.Enabled = false;
			step2.Enabled = true;
			step3.Enabled = true;
			Assert.Equal (step2.Title.ToString(), wizard.GetNextStep ().Title.ToString());

			step1.Enabled = false;
			step2.Enabled = false;
			step3.Enabled = true;
			Assert.Equal (step3.Title.ToString(), wizard.GetNextStep ().Title.ToString());

			step1.Enabled = false;
			step2.Enabled = true;
			step3.Enabled = false;
			Assert.Equal (step2.Title.ToString(), wizard.GetNextStep ().Title.ToString());

			step1.Enabled = true;
			step2.Enabled = false;
			step3.Enabled = false;
			Assert.Equal (step1.Title.ToString(), wizard.GetNextStep ().Title.ToString());
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GoNext_Works ()
		{
			// If zero steps do nothing

			// If one step do nothing (enabled or disabled)

			// If two steps
			//    If current is 1
			//        If 2 is enabled 2 becomes current
			//        If 2 is disabled 1 stays current
			//    If current is 2 does nothing
		}

		[Fact, AutoInitShutdown]
		public void Navigate_GoBack_Works ()
		{
			// If zero steps do nothing

			// If one step do nothing (enabled or disabled)

			// If two steps
			//    If current is 1 does nothing
			//    If current is 2 does nothing
			//        If 1 is enabled 2 becomes current
			//        If 1 is disabled 1 stays current
		}
	}
}