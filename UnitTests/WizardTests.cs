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

			//	var btnBackText = "Back";
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
	}
}