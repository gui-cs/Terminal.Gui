using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	/// <summary>
	/// Base class for each demo/scenario. To define a new scenario simply
	/// 
	/// 1) declare a class derived from Scenario,
	/// 2) Set Name and Description as appropriate using [ScenarioMetadata] attribute
	/// 3) Set one or more categories with the [ScenarioCategory] attribute
	/// 4) Implement Setup.
	/// 5) Optionally, implement Run.
	/// 
	/// The Main program uses reflection to find all scenarios and adds them to the
	/// ListViews. Press ENTER to run the selected scenario. Press CTRL-Q to exit it.
	/// </summary>
	public class Scenario : IDisposable {
		private bool _disposedValue;

		/// <summary>
		/// The Top level for the Scenario. This should be set to `Application.Top` in most cases.
		/// </summary>
		public Toplevel Top { get; set; }

		/// <summary>
		/// The Window for the Scenario. This should be set within the `Application.Top` in most cases.
		/// </summary>
		public Window Win { get; set; }

		/// <summary>
		/// Helper that provides the default Window implementation with a frame and 
		/// label showing the name of the Scenario and logic to exit back to 
		/// the Scenario picker UI.
		/// Override Init to provide any `Toplevel` behavior needed.
		/// </summary>
		/// <param name="top"></param>
		public virtual void Init(Toplevel top)
		{
			Top = top;
			Win = new Window ($"CTRL-Q to Close - Scenario: {GetName ()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
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
		/// Helper to get the Scenario Description
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

		/// <summary>
		/// Override this to implement the Scenario setup logic (create controls, etc...). 
		/// </summary>
		public virtual void Setup ()
		{
		}

		/// <summary>
		/// Runs the scenario. Override to start the scenario using a Top level different than `Top`.
		/// </summary>
		public virtual void Run ()
		{
			Application.Run (Top);
		}

		/// <summary>
		/// Stops the scenario. Override to implement shutdown behavior for the Scenario.
		/// </summary>
		public virtual void RequestStop ()
		{
			Application.RequestStop ();
		}

		/// <summary>
		/// Returns a list of all Categories set by all of the scenarios defined in the project.
		/// </summary>
		internal static List<string> GetAllCategories ()
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
		internal static List<Type> GetDerivedClassesCollection ()
		{
			List<Type> objects = new List<Type> ();
			foreach (Type type in typeof (Scenario).Assembly.GetTypes ()
			    .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf (typeof (Scenario)))) {
				objects.Add (type);
			}
			return objects;
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!_disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				_disposedValue = true;
			}
		}

		public void Dispose ()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose (disposing: true);
			GC.SuppressFinalize (this);
		}
	}
}
