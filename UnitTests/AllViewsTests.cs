using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using System.IO;

namespace Terminal.Gui.Views {
	public class AllViewsTests {
		[Fact]
		public void AllViews_Tests_All_Constructors ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			foreach (var type in GetAllViewClassesCollection ()) {
				Assert.True (Constructors_FullTest (type));
			}

			Application.Shutdown ();
		}

		public bool Constructors_FullTest (Type type)
		{
			foreach (var ctor in type.GetConstructors ()) {
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
						Assert.IsType (type, (View)Activator.CreateInstance (type));
					} else {
						Assert.IsType (type, ctor.Invoke (pTypes.ToArray ()));
					}
				}
			}

			return true;
		}

		private static void AddArguments (Type paramType, List<object> pTypes)
		{
			if (paramType == typeof (Rect)) {
				pTypes.Add (Rect.Empty);
			} else if (paramType == typeof (NStack.ustring)) {
				pTypes.Add (NStack.ustring.Empty);
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

		List<Type> GetAllViewClassesCollection ()
		{
			List<Type> types = new List<Type> ();
			foreach (Type type in typeof (View).Assembly.GetTypes ()
			 .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsPublic && myType.IsSubclassOf (typeof (View)))) {
				types.Add (type);
			}
			return types;
		}
	}
}
