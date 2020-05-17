using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	/// <summary>
	/// Base class for each demo/scenario. To define a new sceanrio simply
	/// 
	/// 1) declare a class derived from Scenario,
	/// 2) Set Name and Description as appropriate using [ScenarioMetadata] attribute
	/// 3) Set one or more categories with the [ScenarioCategory] attribute
	/// 4) Implement Run.
	/// 
	/// The Main program uses reflection to find all sceanarios and adds them to the
	/// ListViews. Press ENTER to run the selected sceanrio. Press ESC to exit it.
	/// </summary>
	public class Scenario {
		/// <summary>
		/// The Top level for the Scenario. 
		/// </summary>
		public Toplevel Top { get; set; }

		/// <summary>
		/// </summary>
		public Window Win { get; set; }


		public Scenario ()
		{
			Top = new Toplevel (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows));
			Win = new Window ($"ESC to Close - Scenario: {GetName ()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			Win.OnKeyPress += (KeyEvent ke) => {
				if (ke.Key == Key.F1) {
					RequestStop ();
				}
			}; 
			Top.Add (Win);
		}

		[System.AttributeUsage (System.AttributeTargets.Class)]
		public class ScenarioMetadata : System.Attribute {
			/// <summary>
			/// Scenario Name
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// Scenario Description
			/// </summary>
			public string Description { get; set; }

			public ScenarioMetadata (string Name, string Description)
			{
				this.Name = Name;
				this.Description = Description;
			}

			/// <summary>
			/// Static helper function to get the Scenario Name given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static string GetName (Type t) => ((ScenarioMetadata)System.Attribute.GetCustomAttributes (t) [0]).Name;

			/// <summary>
			/// Static helper function to get the Scenario Description given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static string GetDescription (Type t) => ((ScenarioMetadata)System.Attribute.GetCustomAttributes (t) [0]).Description;
		}

		/// <summary>
		/// Helper to get the Scenario Name
		/// </summary>
		/// <returns></returns>
		public string GetName () => ScenarioMetadata.GetName (this.GetType ());

		/// <summary>
		/// Helper to get the Scenario Descripiton
		/// </summary>
		/// <returns></returns>
		public string GetDescription () => ScenarioMetadata.GetDescription (this.GetType ());

		[System.AttributeUsage (System.AttributeTargets.Class, AllowMultiple = true)]
		public class ScenarioCategory : System.Attribute {
			/// <summary>
			/// Category Name
			/// </summary>
			public string Name { get; set; }

			public ScenarioCategory (string Name) => this.Name = Name;

			/// <summary>
			/// Static helper function to get the Scenario Name given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static string GetName (Type t) => ((ScenarioCategory)System.Attribute.GetCustomAttributes (t) [0]).Name;

			/// <summary>
			/// Static helper function to get the Scenario Categories given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static List<string> GetCategories (Type t) => System.Attribute.GetCustomAttributes (t)
				.ToList ()
				.Where (a => a is ScenarioCategory)
				.Select (a => ((ScenarioCategory)a).Name)
				.ToList ();
		}

		/// <summary>
		/// Helper function to get the Categories of a Scenario
		/// </summary>
		/// <returns></returns>
		public List<string> GetCategories () => ScenarioCategory.GetCategories (this.GetType ());

		public override string ToString () => $"{GetName (),-30}{GetDescription ()}";

		public virtual void RequestStop ()
		{
			Application.RequestStop ();
		}

		/// <summary>
		/// Runs the scenario. Override to start the scearnio using a Top level different than `Top`.
		/// </summary>
		public virtual void Run ()
		{
			Application.Run (Top);
		}

		/// <summary>
		/// Override this to implement the Scenario setup logic (create controls, etc...). 
		/// </summary>
		public virtual void Setup ()
		{
		}

		/// <summary>
		/// Returns a list of all Categories set by all of the scenarios defined in the project.
		/// </summary>
			public static List<string> GetAllCategories ()
		{
			List<string> categories = new List<string> () { "All" };
			foreach (Type type in typeof (Scenario).Assembly.GetTypes ()
			    .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf (typeof (Scenario)))) {
				List<System.Attribute> attrs = System.Attribute.GetCustomAttributes (type).ToList ();
				categories = categories.Union (attrs.Where (a => a is ScenarioCategory).Select (a => ((ScenarioCategory)a).Name)).ToList ();
			}
			return categories;
		}

		/// <summary>
		/// Returns an instance of each Scenario defined in the project. 
		/// https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
		/// </summary>
		public static List<Type> GetDerivedClassesCollection ()
		{
			List<Type> objects = new List<Type> ();
			foreach (Type type in typeof (Scenario).Assembly.GetTypes ()
			    .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf (typeof (Scenario)))) {
				objects.Add (type);
			}
			return objects;
		}
	}
}
