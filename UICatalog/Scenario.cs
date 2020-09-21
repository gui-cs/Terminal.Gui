using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	/// <summary>
	/// <para>Base class for each demo/scenario.</para>
	/// <para>
	///  To define a new scenario:
	///  <list type="number">
	///  <item><description>Create a new <c>.cs</c> file in the <cs>Scenarios</cs> directory that derives from <see cref="Scenario"/>.</description></item>
	///  <item><description>Annotate the <see cref="Scenario"/> derived class with a <see cref="Scenario.ScenarioMetaData"/> attribute specifying the scenario's name and description.</description></item>
	///  <item><description>Add one or more <see cref="Scenario.ScenarioCategory"/> attributes to the class specifying which categories the sceanrio belongs to. If you don't specify a category the sceanrio will show up in "All".</description></item>
	///  <item><description>Implement the <see cref="Setup"/> override which will be called when a user selects the scenario to run.</description></item>
	///  <item><description>Optionally, implement the <see cref="Init(Toplevel)"/> and/or <see cref="Run"/> overrides to provide a custom implementation.</description></item>
	///  </list>
	/// </para>
	/// <para>
	/// The UI Catalog program uses reflection to find all scenarios and adds them to the
	/// ListViews. Press ENTER to run the selected scenario. Press CTRL-Q to exit it.	/
	/// </para>
	/// </summary>
	/// <example>
	/// The example below is provided in the `Scenarios` directory as a generic sample that can be copied and re-named:
	/// <code>
	/// using Terminal.Gui;
	/// 
	/// namespace UICatalog {
	/// 	[ScenarioMetadata (Name: "Generic", Description: "Generic sample - A template for creating new Scenarios")]
	/// 	[ScenarioCategory ("Controls")]
	/// 	class MyScenario : Scenario {
	/// 		public override void Setup ()
	/// 		{
	/// 			// Put your scenario code here, e.g.
	/// 			Win.Add (new Button ("Press me!") {
	/// 				X = Pos.Center (),
	/// 				Y = Pos.Center (),
	/// 				Clicked = () => MessageBox.Query (20, 7, "Hi", "Neat?", "Yes", "No")
	/// 			});
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	public class Scenario : IDisposable {
		private bool _disposedValue;

		/// <summary>
		/// The Top level for the <see cref="Scenario"/>. This should be set to <see cref="Terminal.Gui.Application.Top"/> in most cases.
		/// </summary>
		public Toplevel Top { get; set; }

		/// <summary>
		/// The Window for the <see cref="Scenario"/>. This should be set within the <see cref="Terminal.Gui.Application.Top"/> in most cases.
		/// </summary>
		public Window Win { get; set; }

		/// <summary>
		/// Helper that provides the default <see cref="Terminal.Gui.Window"/> implementation with a frame and 
		/// label showing the name of the <see cref="Scenario"/> and logic to exit back to 
		/// the Scenario picker UI.
		/// Override <see cref="Init(Toplevel)"/> to provide any <see cref="Terminal.Gui.Toplevel"/> behavior needed.
		/// </summary>
		/// <param name="top">The Toplevel created by the UI Catalog host.</param>
		/// <param name="colorScheme">The colorscheme to use.</param>
		/// <remarks>
		/// <para>
		/// Thg base implementation calls <see cref="Application.Init"/>, sets <see cref="Top"/> to the passed in <see cref="Toplevel"/>, creates a <see cref="Window"/> for <see cref="Win"/> and adds it to <see cref="Top"/>.
		/// </para>
		/// <para>
		/// Overrides that do not call the base.<see cref="Run"/>, must call <see cref="Application.Init "/> before creating any views or calling other Terminal.Gui APIs.
		/// </para>
		/// </remarks>
		public virtual void Init(Toplevel top, ColorScheme colorScheme)
		{
			Application.Init ();

			Top = top;
			if (Top == null) {
				Top = Application.Top;
			}

			Win = new Window ($"CTRL-Q to Close - Scenario: {GetName ()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = colorScheme,
			};
			Top.Add (Win);
		}

		/// <summary>
		/// Defines the metadata (Name and Description) for a <see cref="Scenario"/>
		/// </summary>
		[System.AttributeUsage (System.AttributeTargets.Class)]
		public class ScenarioMetadata : System.Attribute {
			/// <summary>
			/// <see cref="Scenario"/> Name
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// <see cref="Scenario"/> Description
			/// </summary>
			public string Description { get; set; }

			public ScenarioMetadata (string Name, string Description)
			{
				this.Name = Name;
				this.Description = Description;
			}

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Name given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static string GetName (Type t) => ((ScenarioMetadata)System.Attribute.GetCustomAttributes (t) [0]).Name;

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Description given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static string GetDescription (Type t) => ((ScenarioMetadata)System.Attribute.GetCustomAttributes (t) [0]).Description;
		}

		/// <summary>
		/// Helper to get the <see cref="Scenario"/> Name (defined in <see cref="ScenarioMetadata"/>)
		/// </summary>
		/// <returns></returns>
		public string GetName () => ScenarioMetadata.GetName (this.GetType ());

		/// <summary>
		/// Helper to get the <see cref="Scenario"/> Description (defined in <see cref="ScenarioMetadata"/>)
		/// </summary>
		/// <returns></returns>
		public string GetDescription () => ScenarioMetadata.GetDescription (this.GetType ());

		/// <summary>
		/// Defines the category names used to catagorize a <see cref="Scenario"/>
		/// </summary>
		[System.AttributeUsage (System.AttributeTargets.Class, AllowMultiple = true)]
		public class ScenarioCategory : System.Attribute {
			/// <summary>
			/// Category Name
			/// </summary>
			public string Name { get; set; }

			public ScenarioCategory (string Name) => this.Name = Name;

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Name given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns>Name of the category</returns>
			public static string GetName (Type t) => ((ScenarioCategory)System.Attribute.GetCustomAttributes (t) [0]).Name;

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Categories given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns>list of category names</returns>
			public static List<string> GetCategories (Type t) => System.Attribute.GetCustomAttributes (t)
				.ToList ()
				.Where (a => a is ScenarioCategory)
				.Select (a => ((ScenarioCategory)a).Name)
				.ToList ();
		}

		/// <summary>
		/// Helper function to get the list of categories a <see cref="Scenario"/> belongs to (defined in <see cref="ScenarioCategory"/>)
		/// </summary>
		/// <returns>list of category names</returns>
		public List<string> GetCategories () => ScenarioCategory.GetCategories (this.GetType ());

		/// <inheritdoc/>
		public override string ToString () => $"{GetName (),-30}{GetDescription ()}";

		/// <summary>
		/// Override this to implement the <see cref="Scenario"/> setup logic (create controls, etc...). 
		/// </summary>
		/// <remarks>This is typically the best place to put scenario logic code.</remarks>
		public virtual void Setup ()
		{
		}

		/// <summary>
		/// Runs the <see cref="Scenario"/>. Override to start the <see cref="Scenario"/> using a <see cref="Toplevel"/> different than `Top`.
		/// 
		/// </summary>
		/// <remarks>
		/// Overrides that do not call the base.<see cref="Run"/>, must call <see cref="Application.Shutdown"/> before returning.
		/// </remarks>
		public virtual void Run ()
		{
			// Must explicit call Application.Shutdown method to shutdown.
			Application.Run (Top);
		}

		/// <summary>
		/// Stops the scenario. Override to change shutdown behavior for the <see cref="Scenario"/>.
		/// </summary>
		public virtual void RequestStop ()
		{
			Application.RequestStop ();
		}

		/// <summary>
		/// Returns a list of all Categories set by all of the <see cref="Scenario"/>s defined in the project.
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
		/// Returns an instance of each <see cref="Scenario"/> defined in the project. 
		/// https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
		/// </summary>
		public static List<Type> GetDerivedClasses<T> ()
		{
			List<Type> objects = new List<Type> ();
			foreach (Type type in typeof (T).Assembly.GetTypes ()
			 .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf (typeof (T)))) {
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
