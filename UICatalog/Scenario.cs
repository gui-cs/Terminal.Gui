using System;
using System.Collections.Generic;
using System.Linq;

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
		[System.AttributeUsage (System.AttributeTargets.Class)]
		public class ScenarioMetadata : System.Attribute {
			public string Name { get; set; }
			public List<string> Categories { get; set; } = new List<string> ();
			public string Description { get; set; }

			public ScenarioMetadata (string Name, string Description)
			{
				this.Name = Name;
				this.Description = Description;
			}
		}

		public string GetName ()
		{
			List<System.Attribute> attrs = System.Attribute.GetCustomAttributes (this.GetType ()).ToList ();
			if (attrs [0] is ScenarioMetadata)
				return $"{((ScenarioMetadata)attrs [0]).Name}";
			else
				return "<error>";
		}

		public string GetDescription ()
		{
			List<System.Attribute> attrs = System.Attribute.GetCustomAttributes (this.GetType ()).ToList ();
			if (attrs [0] is ScenarioMetadata)
				return $"{((ScenarioMetadata)attrs [0]).Description}";
			else
				return "<error>";
		}

		[System.AttributeUsage (System.AttributeTargets.Class, AllowMultiple = true)]
		public class ScenarioCategory : System.Attribute {
			public string Name { get; set; }
			public ScenarioCategory (string Name)
			{
				this.Name = Name;
			}
		}

		public List<string> GetCategories ()
		{
			List<System.Attribute> attrs = System.Attribute.GetCustomAttributes (this.GetType ()).ToList ();
			return attrs.Where (a => a is ScenarioCategory).Select (a => ((ScenarioCategory)a).Name).ToList ();
		}

		public override string ToString ()
		{
			return $"{GetName (),-30}{GetDescription ()}";
		}

		public virtual void Run ()
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
		public static ICollection<Scenario> GetDerivedClassesCollection ()
		{
			List<Scenario> objects = new List<Scenario> ();
			foreach (Type type in typeof (Scenario).Assembly.GetTypes ()
			    .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf (typeof (Scenario)))) {
				objects.Add ((Scenario)Activator.CreateInstance (type));
			}
			return objects;
		}
	}
}
