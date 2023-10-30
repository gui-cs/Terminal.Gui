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

		[Fact]
		[AutoInitShutdown]
		public void TestViewsDisposeCorrectly ()
		{
			var reference = DoTest ();
			for (var i = 0; i < 10 && reference.IsAlive; i++) {
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
			}
#if DEBUG_IDISPOSABLE
			if (reference.IsAlive) {
				Assert.True (((View)reference.Target).WasDisposed);
				Assert.Fail ($"Some Views didnt get Garbage Collected: {((View)reference.Target).Subviews}");
			}
#endif
		}

		void getSpecialParams ()
		{
			special_params.Clear ();
			//special_params.Add (typeof (LineView), new object [] { Orientation.Horizontal });
		}

		WeakReference DoTest ()
		{
			getSpecialParams ();
			View Container = new View ();
			Toplevel top = Application.Top;
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
				output.WriteLine ($"Added instance of {view}!");
			}
			top.Add (Container);
			// make sure the application is doing to the views whatever its supposed to do to the views
			for (var i = 0; i < 100; i++) {
				Application.Refresh ();
			}

			top.Remove (Container);
			WeakReference reference = new (Container, true);
			Container.Dispose ();
			return reference;
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
