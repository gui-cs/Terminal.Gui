using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests {

	public class ViewDisposalTest {

#nullable enable
		Dictionary<Type, object? []?> special_params = new Dictionary<Type, object? []?> ();
#nullable restore

		readonly ITestOutputHelper output;

		public ViewDisposalTest (ITestOutputHelper output)
		{
			{
				this.output = output;
			}
		}

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void TestViewsDisposeCorrectly (bool callShutdown)
		{
			var refs = DoTest (callShutdown);
			//var reference = refs [0];
			for (var i = 0; i < 10 && refs [0].IsAlive; i++) {
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
			}
			foreach (var reference in refs) {
				if (reference.IsAlive) {
#if DEBUG_IDISPOSABLE
					Assert.True (((View)reference.Target).WasDisposed);
#endif
					string alive = "";						// Instead of just checking the subviews of the container, we now iterate through a list
					foreach (var r in refs) {					// of Weakreferences Referencing every View that was tested. This makes more sense because 
						if (r.IsAlive) {					// View.Dispose removes all of its subviews, wich is why View.Subviews is always empty 
							if (r == refs [0]) {				// after View.Dispose has run. Luckily I didnt discover any more bugs or this wouldv'e
								alive += "\n View (Container)";         // been a little bit annoying to find an answer for. Thanks to BDisp for listening to
							}						// me and giving his best to help me fix this thing. If you take a look at the commit log
							alive += ",\n--";				// you will find that he did most of the work. -a-usr
							alive += r.Target.GetType ().Name;
						}							// NOTE: DELETE BEFORE NEXT COMMIT
					}
					Assert.Fail ($"Some Views didnt get Garbage Collected: {alive}");
				}
			}
			if (!callShutdown) {
				Application.Shutdown ();
			}
		}

		void getSpecialParams ()
		{
			special_params.Clear ();
			//special_params.Add (typeof (LineView), new object [] { Orientation.Horizontal });
		}

		List<WeakReference> DoTest (bool callShutdown)
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (driver));
			getSpecialParams ();
			View Container = new View ();
			List<WeakReference> refs = new List<WeakReference> { new WeakReference (Container, true) };
			Container.Add (new View ());
			Toplevel top = new ();
			var state = Application.Begin (top);
			var views = GetViews ();
			foreach (var view in views) {
				View instance;
				//Create instance of view and add to container
				if (special_params.ContainsKey (view)) {
					instance = (View)Activator.CreateInstance (view, special_params [view]);
				} else {
					instance = (View)Activator.CreateInstance (view);
				}

				Assert.NotNull (instance);
				Container.Add (instance);

				refs.Add (new WeakReference (instance, true));
				output.WriteLine ($"Added instance of {view}!");
			}
			top.Add (Container);
			// make sure the application is doing to the views whatever its supposed to do to the views
			for (var i = 0; i < 100; i++) {
				Application.Refresh ();
			}

			top.Remove (Container);
			Application.End (state);
			Assert.True (refs.All (r => r.IsAlive));
#if DEBUG_IDISPOSABLE
			Assert.True (top.WasDisposed);
			Assert.False (Container.WasDisposed);
#endif
			Assert.Null (Application.Top);
			Container.Dispose ();
#if DEBUG_IDISPOSABLE
			Assert.True (Container.WasDisposed);
#endif
			if (callShutdown) {
				Application.Shutdown ();
			}

			return refs;
		}

		/// <summary>
		/// Get all types derived from <see cref="View"/> using reflection
		/// </summary>
		/// <returns></returns>
		List<Type> GetViews ()
		{
			List<Type> valid = new ();
			// Filter all types that can be instantiated, are public, arent generic,  aren't the view type itself, but derive from view
			foreach (var type in Assembly.GetAssembly (typeof (View)).GetTypes ().Where (T => { //body of anonymous check function
				return ((!T.IsAbstract) && T.IsPublic && T.IsClass && T.IsAssignableTo (typeof (View)) && !T.IsGenericType && !(T == typeof (View)));
			})) //end of body of anonymous check function
			{ //body of the foreach loop
				output.WriteLine ($"Found Type {type.Name}");
				Assert.DoesNotContain (type, valid);
				Assert.True (type.IsAssignableTo (typeof (IDisposable)));// Just to be safe
				valid.Add (type);
				output.WriteLine ("	-Added!");
			} //end body of foreach loop

			return valid;
		}
	}
}
