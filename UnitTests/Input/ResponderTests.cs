using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.InputTests;

public class ResponderTests {
	[Fact] [TestRespondersDisposed]
	public void New_Initializes ()
	{
		var r = new Responder ();
		Assert.NotNull (r);
		Assert.Equal ("Terminal.Gui.Responder", r.ToString ());
		Assert.False (r.CanFocus);
		Assert.False (r.HasFocus);
		Assert.True (r.Enabled);
		Assert.True (r.Visible);
		r.Dispose ();
	}

	[Fact] [TestRespondersDisposed]
	public void New_Methods_Return_False ()
	{
		var r = new View ();

		//Assert.False (r.OnKeyDown (new KeyEventArgs () { Key = Key.Unknown }));
		Assert.False (r.OnKeyDown (new KeyEventArgs () { ConsoleDriverKey = KeyCode.Unknown }));
		Assert.False (r.OnKeyUp (new KeyEventArgs () { ConsoleDriverKey = KeyCode.Unknown }));
		Assert.False (r.MouseEvent (new MouseEvent () { Flags = MouseFlags.AllEvents }));
		Assert.False (r.OnMouseEnter (new MouseEvent () { Flags = MouseFlags.AllEvents }));
		Assert.False (r.OnMouseLeave (new MouseEvent () { Flags = MouseFlags.AllEvents }));

		var v = new View ();
		Assert.False (r.OnEnter (v));
		v.Dispose ();

		v = new View ();
		Assert.False (r.OnLeave (v));
		v.Dispose ();

		r.Dispose ();
	}

	[Fact]
	public void KeyPressed_Handled_True_Cancels_KeyPress ()
	{
		var r = new View ();
		var args = new KeyEventArgs () { ConsoleDriverKey = KeyCode.Unknown };

		Assert.False (r.OnKeyDown (args));
		Assert.False (args.Handled);

		r.KeyDown += (s, a) => a.Handled = true;
		Assert.True (r.OnKeyDown (args));
		Assert.True (args.Handled);

		r.Dispose ();
	}

	// Generic lifetime (IDisposable) tests
	[Fact] [TestRespondersDisposed]
	public void Dispose_Works ()
	{

		var r = new Responder ();
#if DEBUG_IDISPOSABLE
		Assert.Single (Responder.Instances);
#endif

		r.Dispose ();
#if DEBUG_IDISPOSABLE
		Assert.Empty (Responder.Instances);
#endif
	}

	public class DerivedView : View {
		public DerivedView () { }

		public override bool OnKeyDown (KeyEventArgs keyEvent)
		{
			return true;
		}
	}

	[Fact] [TestRespondersDisposed]
	public void IsOverridden_False_IfNotOverridden ()
	{
		// MouseEvent IS defined on Responder but NOT overridden
		Assert.False (Responder.IsOverridden (new Responder () { }, "MouseEvent"));

		// MouseEvent is defined on Responder and NOT overrident on View
		Assert.False (Responder.IsOverridden (new View () { Text = "View does not override MouseEvent" }, "MouseEvent"));
		Assert.False (Responder.IsOverridden (new DerivedView () { Text = "DerivedView does not override MouseEvent" }, "MouseEvent"));

		// MouseEvent is NOT defined on DerivedView 
		Assert.False (Responder.IsOverridden (new DerivedView () { Text = "DerivedView does not override MouseEvent" }, "MouseEvent"));

		// OnKeyDown is defined on View and NOT overrident on Button
		Assert.False (Responder.IsOverridden (new Button () { Text = "Button does not override OnKeyDown" }, "OnKeyDown"));

#if DEBUG_IDISPOSABLE
		// HACK: Force clean up of Responders to avoid having to Dispose all the Views created above.
		Responder.Instances.Clear ();
		Assert.Empty (Responder.Instances);
#endif
	}

	[Fact] [TestRespondersDisposed]
	public void IsOverridden_True_IfOverridden ()
	{
		// MouseEvent is defined on Responder IS overriden on ScrollBarView (but not View)
		Assert.True (Responder.IsOverridden (new ScrollBarView () { Text = "ScrollBarView overrides MouseEvent" }, "MouseEvent"));

		//// OnKeyDown is defined on View
		//Assert.True (Responder.IsOverridden (new View () { Text = "View overrides OnKeyDown" }, "OnKeyDown"));

		//// OnKeyDown is defined on DerivedView
		//Assert.True (Responder.IsOverridden (new DerivedView () { Text = "DerivedView overrides OnKeyDown" }, "OnKeyDown"));

		// ScrollBarView overrides both MouseEvent (from Responder) and Redraw (from View)
		Assert.True (Responder.IsOverridden (new ScrollBarView () { Text = "ScrollBarView overrides MouseEvent" }, "MouseEvent"));
		Assert.True (Responder.IsOverridden (new ScrollBarView () { Text = "ScrollBarView overrides OnDrawContent" }, "OnDrawContent"));

		Assert.True (Responder.IsOverridden (new Button () { Text = "Button overrides MouseEvent" }, "MouseEvent"));
#if DEBUG_IDISPOSABLE
		// HACK: Force clean up of Responders to avoid having to Dispose all the Views created above.
		Responder.Instances.Clear ();
		Assert.Empty (Responder.Instances);
#endif
	}
}