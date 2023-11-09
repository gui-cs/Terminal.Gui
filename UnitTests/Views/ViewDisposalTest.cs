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
#nullable enable
		Dictionary<Type, object? []?> special_params = new Dictionary<Type, object? []?> ();
#nullable restore
		[Fact]
		[Fact]
		[Fact]
		[AutoInitShutdown]
		[Fact]
		public void TestViewsDisposeCorrectly ()
		{
			var reference = DoTest ();
			for (var i = 0; i < 10 && reference.IsAlive; i++) {
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
			}
				string all = "\nView (Container)";
				foreach (var v in ((View)reference.Target).Subviews) {
					all += ",\n";
					all += v.GetType ().Name;
				}
				Assert.Fail ($"Some Views didnt get Garbage Collected: {all}");
			}
		}
		}
			}
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
			Toplevel top = new ();
			//Application.Init ();
			var state = Application.Begin (top);
			var views = GetViews ();
			Container.Add (new View ());
			foreach (var view in views) {
				View instance;
				} else
					instance = (View)Activator.CreateInstance (view);
					instance = (View)Activator.CreateInstance (view);
				else
					instance = (View)Activator.CreateInstance (view);
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
			Application.End (state);
			WeakReference reference = new (Container);
			Container.Dispose ();
			top.Dispose ();
			return reference;
		}

		/// <summary>
		/// Get all types derived from <see cref="View"/> using reflection
		/// </summary>
		/// <returns></returns>
		List<Type> GetViews ()
			foreach (var type in Assembly.GetAssembly (typeof (View)).GetTypes ().Where (T => {
				return ((!T.IsAbstract) && T.IsPublic && T.IsClass && T.IsAssignableTo (typeof (View)) && !T.IsGenericType && !(T == typeof (View)));
			})) {
				return ((!T.IsAbstract) && T.IsPublic && T.IsClass && T.IsAssignableTo (typeof (View)) && !T.IsGenericType && !(T == typeof (View)));})) {
			foreach (var type in Assembly.GetAssembly (typeof (View)).GetTypes ().Where (T => {
				return ((!T.IsAbstract) && T.IsPublic && T.IsClass && T.IsAssignableTo (typeof (View)) && !T.IsGenericType && !(T == typeof (View)));})) {
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
