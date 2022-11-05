using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.Core {
	/// <summary>
	/// These tests focus on Application.RunState and the various ways it can be changed.
	/// </summary>
	public class RunStateTests {
		public RunStateTests ()
		{
#if DEBUG_IDISPOSABLE
			Responder.Instances.Clear ();
			Application.RunState.Instances.Clear ();
#endif
		}

		[Fact]
		public void New_Creates_RunState ()
		{
			var rs = new Application.RunState (null);
			Assert.Null (rs.Toplevel);
			
			var top = new Toplevel ();
			rs = new Application.RunState (top);
			Assert.Equal (top, rs.Toplevel);
		}

		[Fact]
		public void Dispose_Cleans_Up_RunState ()
		{
			var rs = new Application.RunState (null);
			Assert.NotNull (rs);

			// Should not throw because Toplevel was null
			rs.Dispose ();
			Assert.True (rs.WasDisposed);

			var top = new Toplevel ();
			rs = new Application.RunState (top);
			Assert.NotNull (rs);

			// Should throw because Toplevel was not cleaned up
			Assert.Throws<InvalidOperationException> (() => rs.Dispose ());

			rs.Toplevel.Dispose ();
			rs.Toplevel = null;
			rs.Dispose ();
			Assert.True (rs.WasDisposed);
			Assert.True (top.WasDisposed);
		}

		void Init ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			Assert.NotNull (Application.Driver);
			Assert.NotNull (Application.MainLoop);
			Assert.NotNull (SynchronizationContext.Current);
		}

		void Shutdown ()
		{
			Application.Shutdown ();
			// Validate there are no outstanding RunState-based instances left
			foreach (var inst in Application.RunState.Instances) {
				Assert.True (inst.WasDisposed);
			}
		}

		[Fact]
		public void Begin_End_Cleans_Up_RunState ()
		{
			// Setup Mock driver
			Init ();

			// Test null Toplevel
			Assert.Throws<ArgumentNullException> (() => Application.Begin (null));

			var top = new Toplevel ();
			var rs = Application.Begin (top);
			Assert.NotNull (rs);
			Assert.Equal (top, Application.Current);
			Application.End (rs);

			Assert.Null (Application.Current);
			Assert.NotNull (Application.Top);
			Assert.NotNull (Application.MainLoop);
			Assert.NotNull (Application.Driver);

			Shutdown ();

			Assert.True (rs.WasDisposed);

			Assert.Null (Application.Top);
			Assert.Null (Application.MainLoop);
			Assert.Null (Application.Driver);
		}
	}
}
