using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.Views {
	public class SpinnerViewTests {

		readonly ITestOutputHelper output;

		public SpinnerViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}


		[Fact,AutoInitShutdown]
		public void TestSpinnerView_ThrottlesAnimation()
		{
			var view = GetSpinnerView ();

			view.Redraw (view.Bounds);
			
			var expected = "/";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Redraw (view.Bounds);

			expected = "/";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Redraw (view.Bounds);

			expected = "/";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void TestSpinnerView_NoThrottle()
		{
			var view = GetSpinnerView ();
			view.SpinDelayInMilliseconds = 0;

			view.Redraw (view.Bounds);


			var expected = @"─";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Redraw (view.Bounds);


			expected = @"\";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		private SpinnerView GetSpinnerView ()
		{
			var view = new SpinnerView (); 
			
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			Assert.Equal (1, view.Width);
			Assert.Equal (1, view.Height);

			return view;
		}
	}
}