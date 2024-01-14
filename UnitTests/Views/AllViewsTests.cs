using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;
public class AllViewsTests {
	readonly ITestOutputHelper _output;

	public AllViewsTests (ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void AllViews_Center_Properly ()
	{
		// See https://github.com/gui-cs/Terminal.Gui/issues/3156

		foreach (var type in GetAllViewClasses ()) {
			Application.Init (new FakeDriver ());
			var view = CreateViewFromType (type, type.GetConstructor (Array.Empty<Type> ()));
			if (view == null) {
				_output.WriteLine ($"Ignoring {type} - It's a Generic");
				Application.Shutdown ();
				continue;
			}
			view.X = Pos.Center ();
			view.Y = Pos.Center ();

			// Ensure the view has positive dimensions
			view.Width = 10;
			view.Height = 10;

			var frame = new View () {
				X = 0,
				Y = 0,
				Width = 50,
				Height = 50,
			};
			frame.Add (view);
			frame.BeginInit ();
			frame.EndInit ();
			frame.LayoutSubviews ();

			// What's the natural width/height?
			var expectedX = (frame.Frame.Width - view.Frame.Width) / 2;
			var expectedY = (frame.Frame.Height - view.Frame.Height) / 2;

			Assert.True (view.Frame.Left == expectedX, $"{view} did not center horizontally. Expected: {expectedX}. Actual: {view.Frame.Left}");
			Assert.True (view.Frame.Top == expectedY, $"{view} did not center vertically. Expected: {expectedY}. Actual: {view.Frame.Top}");
			Application.Shutdown ();
		}
	}

	[Fact]
	public void AllViews_Tests_All_Constructors ()
	{
		Application.Init (new FakeDriver ());

		foreach (var type in GetAllViewClasses ()) {
			Assert.True (Test_All_Constructors_Of_Type (type));
		}

		Application.Shutdown ();
	}

	[Fact]
	public void AllViews_Enter_Leave_Events ()
	{
		foreach (var type in GetAllViewClasses ()) {
			_output.WriteLine ($"Testing {type.Name}");

			Application.Init (new FakeDriver ());

			var top = Application.Top;
			var vType = CreateViewFromType (type, type.GetConstructor (Array.Empty<Type> ()));
			if (vType == null) {
				_output.WriteLine ($"Ignoring {type} - It's a Generic");
				Application.Shutdown ();
				continue;
			}
			vType.X = 0;
			vType.Y = 0;
			vType.Width = 10;
			vType.Height = 1;

			var view = new View () {
				X = 0,
				Y = 1,
				Width = 10,
				Height = 1,
				CanFocus = true
			};
			var vTypeEnter = 0;
			var vTypeLeave = 0;
			var viewEnter = 0;
			var viewLeave = 0;

			vType.Enter += (s, e) => vTypeEnter++;
			vType.Leave += (s, e) => vTypeLeave++;
			view.Enter += (s, e) => viewEnter++;
			view.Leave += (s, e) => viewLeave++;

			top.Add (vType, view);
			Application.Begin (top);

			if (!vType.CanFocus || (vType is Toplevel && ((Toplevel)vType).Modal)) {
				Application.Shutdown ();
				continue;
			}

			if (vType is TextView) {
				top.NewKeyDownEvent (new (KeyCode.Tab | KeyCode.CtrlMask));
			} else if (vType is DatePicker) {
				for (int i = 0; i < 4; i++) {
					top.NewKeyDownEvent (new (KeyCode.Tab | KeyCode.CtrlMask));
				}
			} else {
				top.NewKeyDownEvent (new (KeyCode.Tab));
			}
			top.NewKeyDownEvent (new (KeyCode.Tab));

			Assert.Equal (2, vTypeEnter);
			Assert.Equal (1, vTypeLeave);
			Assert.Equal (1, viewEnter);
			Assert.Equal (1, viewLeave);

			Application.Shutdown ();
		}
	}

	//[Fact]
	//public void AllViews_HotKey_Works ()
	//{
	//	foreach (var type in GetAllViewClasses ()) {
	//		_output.WriteLine ($"Testing {type.Name}");
	//		var view = GetTypeInitializer (type, type.GetConstructor (Array.Empty<Type> ()));
	//		view.HotKeySpecifier = (Rune)'^';
	//		view.Text = "^text";
	//		Assert.Equal(Key.T, view.HotKey);
	//	}
	//}

	public bool Test_All_Constructors_Of_Type (Type type)
	{
		foreach (var ctor in type.GetConstructors ()) {
			var view = CreateViewFromType (type, ctor);
			if (view != null) {
				Assert.True (type.FullName == view.GetType ().FullName);
			}
		}

		return true;
	}

	private static View CreateViewFromType (Type type, ConstructorInfo ctor)
	{
		View viewType = null;

		if (type.IsGenericType && type.IsTypeDefinition) {
			List<Type> gTypes = new List<Type> ();

			foreach (var args in type.GetGenericArguments ()) {
				gTypes.Add (typeof (object));
			}
			type = type.MakeGenericType (gTypes.ToArray ());

			Assert.IsType (type, (View)Activator.CreateInstance (type));

		} else {
			ParameterInfo [] paramsInfo = ctor.GetParameters ();
			Type paramType;
			List<object> pTypes = new List<object> ();

			if (type.IsGenericType) {
				foreach (var args in type.GetGenericArguments ()) {
					paramType = args.GetType ();
					if (args.Name == "T") {
						pTypes.Add (typeof (object));
					} else {
						AddArguments (paramType, pTypes);
					}
				}
			}

			foreach (var p in paramsInfo) {
				paramType = p.ParameterType;
				if (p.HasDefaultValue) {
					pTypes.Add (p.DefaultValue);
				} else {
					AddArguments (paramType, pTypes);
				}

			}

			if (type.IsGenericType && !type.IsTypeDefinition) {
				viewType = (View)Activator.CreateInstance (type);
				Assert.IsType (type, viewType);
			} else {
				viewType = (View)ctor.Invoke (pTypes.ToArray ());
				Assert.IsType (type, viewType);
			}
		}

		return viewType;
	}

	// BUGBUG: This is a hack. We should figure out how to dynamically
	// create the right type of argument for the constructor.
	private static void AddArguments (Type paramType, List<object> pTypes)
	{
		if (paramType == typeof (Rect)) {
			pTypes.Add (Rect.Empty);
		} else if (paramType == typeof (string)) {
			pTypes.Add (string.Empty);
		} else if (paramType == typeof (int)) {
			pTypes.Add (0);
		} else if (paramType == typeof (bool)) {
			pTypes.Add (true);
		} else if (paramType.Name == "IList") {
			pTypes.Add (new List<object> ());
		} else if (paramType.Name == "View") {
			var top = new Toplevel ();
			var view = new View ();
			top.Add (view);
			pTypes.Add (view);
		} else if (paramType.Name == "View[]") {
			pTypes.Add (new View [] { });
		} else if (paramType.Name == "Stream") {
			pTypes.Add (new MemoryStream ());
		} else if (paramType.Name == "String") {
			pTypes.Add (string.Empty);
		} else if (paramType.Name == "TreeView`1[T]") {
			pTypes.Add (string.Empty);
		} else {
			pTypes.Add (null);
		}
	}

	public static List<Type> GetAllViewClasses ()
	{
		return typeof (View).Assembly.GetTypes ()
			.Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsPublic && myType.IsSubclassOf (typeof (View)))
			.ToList ();
	}
}
