using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog;

/// <summary>
///     <para>Base class for each demo/scenario.</para>
///     <para>
///         To define a new scenario:
///         <list type="number">
///             <item>
///                 <description>
///                     Create a new <c>.cs</c> file in the <cs>Scenarios</cs> directory that derives from
///                     <see cref="Scenario"/>.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Annotate the <see cref="Scenario"/> derived class with a
///                     <see cref="Scenario.ScenarioMetadata"/> attribute specifying the scenario's name and description.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Add one or more <see cref="Scenario.ScenarioCategory"/> attributes to the class specifying
///                     which categories the scenario belongs to. If you don't specify a category the scenario will show up
///                     in "_All".
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Implement the <see cref="Setup"/> override which will be called when a user selects the
///                     scenario to run.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Optionally, implement the <see cref="Init()"/> and/or <see cref="Run"/> overrides to
///                     provide a custom implementation.
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The UI Catalog program uses reflection to find all scenarios and adds them to the ListViews. Press ENTER to
///         run the selected scenario. Press the default quit key to quit.
///     </para>
/// </summary>
/// <example>
///     The example below is provided in the `Scenarios` directory as a generic sample that can be copied and re-named:
///     <code>
/// using Terminal.Gui;
/// 
/// namespace UICatalog {
/// 	[ScenarioMetadata (Name: "Generic", Description: "Generic sample - A template for creating new Scenarios")]
/// 	[ScenarioCategory ("Controls")]
/// 	class MyScenario : Scenario {
/// 		public override void Setup ()
/// 		{
/// 			// Put your scenario code here, e.g.
/// 			Win.Add (new Button () { Text = "Press me!", 
/// 				X = Pos.Center (),
/// 				Y = Pos.Center (),
/// 				Clicked = () => MessageBox.Query (20, 7, "Hi", "Neat?", "Yes", "No")
/// 			});
/// 		}
/// 	}
/// }
/// </code>
/// </example>
public class Scenario : IDisposable
{
    private static int _maxScenarioNameLen = 30;
    public string Theme = "Default";
    public string TopLevelColorScheme = "Base";
    private bool _disposedValue;

    /// <summary>
    ///     Helper function to get the list of categories a <see cref="Scenario"/> belongs to (defined in
    ///     <see cref="ScenarioCategory"/>)
    /// </summary>
    /// <returns>list of category names</returns>
    public List<string> GetCategories () { return ScenarioCategory.GetCategories (GetType ()); }

    /// <summary>Helper to get the <see cref="Scenario"/> Description (defined in <see cref="ScenarioMetadata"/>)</summary>
    /// <returns></returns>
    public string GetDescription () { return ScenarioMetadata.GetDescription (GetType ()); }

    /// <summary>Helper to get the <see cref="Scenario"/> Name (defined in <see cref="ScenarioMetadata"/>)</summary>
    /// <returns></returns>
    public string GetName () { return ScenarioMetadata.GetName (GetType ()); }

    /// <summary>
    ///     Returns a list of all <see cref="Scenario"/> instanaces defined in the project, sorted by
    ///     <see cref="ScenarioMetadata.Name"/>.
    ///     https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
    /// </summary>
    public static List<Scenario> GetScenarios ()
    {
        List<Scenario> objects = new ();

        foreach (Type type in typeof (Scenario).Assembly.ExportedTypes
                                               .Where (
                                                       myType => myType.IsClass
                                                                 && !myType.IsAbstract
                                                                 && myType.IsSubclassOf (typeof (Scenario))
                                                      ))
        {
            var scenario = (Scenario)Activator.CreateInstance (type);
            objects.Add (scenario);
            _maxScenarioNameLen = Math.Max (_maxScenarioNameLen, scenario.GetName ().Length + 1);
        }

        return objects.OrderBy (s => s.GetName ()).ToList ();
    }

    /// <summary>
    ///     Helper that calls <see cref="Application.Init"/> and creates the default <see cref="Terminal.Gui.Window"/>
    ///     implementation with a frame and label
    ///     showing the name of the <see cref="Scenario"/> and logic to exit back to the Scenario picker UI. Override
    ///     <see cref="Init"/> to provide any <see cref="Terminal.Gui.Toplevel"/> behavior needed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The base implementation calls <see cref="Application.Init"/> and creates a <see cref="Window"/> for
    ///         <see cref="Win"/> and adds it to <see cref="Application.Top"/>.
    ///     </para>
    ///     <para>
    ///         Overrides that do not call the base.<see cref="Run"/>, must call <see cref="Application.Init"/> before
    ///         creating any views or calling other Terminal.Gui APIs.
    ///     </para>
    /// </remarks>
    [ObsoleteAttribute ("This method is obsolete and will be removed in v2. Use Main instead.", false)]
    public virtual void Init ()
    {
        Application.Init ();

        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();

        Top = new ();

        Win = new()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes [TopLevelColorScheme]
        };
        Top.Add (Win);
    }

    /// <summary>
    ///     Called by UI Catalog to run the <see cref="Scenario"/>. This is the main entry point for the <see cref="Scenario"/>
    ///     .
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Scenario developers are encouraged to override this method as the primary way of authoring a new
    ///         scenario.
    ///     </para>
    ///     <para>
    ///         The base implementation calls <see cref="Init"/>, <see cref="Setup"/>, and <see cref="Run"/>.
    ///     </para>
    public virtual void Main ()
    {
        Init ();
        Setup ();
        Run ();
    }

    /// <summary>
    ///     Runs the <see cref="Scenario"/>. Override to start the <see cref="Scenario"/> using a <see cref="Toplevel"/>
    ///     different than `Top`.
    /// </summary>
    /// <remarks>
    ///     Overrides that do not call the base.<see cref="Run"/>, must call <see cref="Application.Shutdown"/> before
    ///     returning.
    /// </remarks>
    [ObsoleteAttribute ("This method is obsolete and will be removed in v2. Use Main instead.", false)]
    public virtual void Run ()
    {
        // Must explicitly call Application.Shutdown method to shutdown.
        Application.Run (Top);
    }

    /// <summary>Override this to implement the <see cref="Scenario"/> setup logic (create controls, etc...).</summary>
    /// <remarks>This is typically the best place to put scenario logic code.</remarks>
    [ObsoleteAttribute ("This method is obsolete and will be removed in v2. Use Main instead.", false)]
    public virtual void Setup () { }

    /// <summary>
    ///     The Toplevel for the <see cref="Scenario"/>. This should be set to <see cref="Terminal.Gui.Application.Top"/>.
    /// </summary>
    //[ObsoleteAttribute ("This property is obsolete and will be removed in v2. Use Main instead.", false)]
    public Toplevel Top { get; set; }

    /// <summary>Gets the Scenario Name + Description with the Description padded based on the longest known Scenario name.</summary>
    /// <returns></returns>
    public override string ToString () { return $"{GetName ().PadRight (_maxScenarioNameLen)}{GetDescription ()}"; }

    /// <summary>
    ///     The Window for the <see cref="Scenario"/>. This should be set to <see cref="Terminal.Gui.Application.Top"/> in
    ///     most cases.
    /// </summary>
    //[ObsoleteAttribute ("This property is obsolete and will be removed in v2. Use Main instead.", false)]
    public Window Win { get; set; }

#region IDispose
    public void Dispose ()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose (true);
        GC.SuppressFinalize (this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Top?.Dispose ();
                Win?.Dispose ();
            }

            _disposedValue = true;
        }
    }
#endregion IDispose

    /// <summary>Returns a list of all Categories set by all of the <see cref="Scenario"/>s defined in the project.</summary>
    internal static List<string> GetAllCategories ()
    {
        List<string> categories = new ();

        foreach (Type type in typeof (Scenario).Assembly.GetTypes ()
                                               .Where (
                                                       myType => myType.IsClass
                                                                 && !myType.IsAbstract
                                                                 && myType.IsSubclassOf (typeof (Scenario))
                                                      ))
        {
            List<System.Attribute> attrs = System.Attribute.GetCustomAttributes (type).ToList ();

            categories = categories
                         .Union (attrs.Where (a => a is ScenarioCategory).Select (a => ((ScenarioCategory)a).Name))
                         .ToList ();
        }

        // Sort
        categories = categories.OrderBy (c => c).ToList ();

        // Put "All" at the top
        categories.Insert (0, "All Scenarios");

        return categories;
    }

    /// <summary>Defines the category names used to catagorize a <see cref="Scenario"/></summary>
    [AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
    public class ScenarioCategory : System.Attribute
    {
        public ScenarioCategory (string Name) { this.Name = Name; }

        /// <summary>Static helper function to get the <see cref="Scenario"/> Categories given a Type</summary>
        /// <param name="t"></param>
        /// <returns>list of category names</returns>
        public static List<string> GetCategories (Type t)
        {
            return GetCustomAttributes (t)
                   .ToList ()
                   .Where (a => a is ScenarioCategory)
                   .Select (a => ((ScenarioCategory)a).Name)
                   .ToList ();
        }

        /// <summary>Static helper function to get the <see cref="Scenario"/> Name given a Type</summary>
        /// <param name="t"></param>
        /// <returns>Name of the category</returns>
        public static string GetName (Type t) { return ((ScenarioCategory)GetCustomAttributes (t) [0]).Name; }

        /// <summary>Category Name</summary>
        public string Name { get; set; }
    }

    /// <summary>Defines the metadata (Name and Description) for a <see cref="Scenario"/></summary>
    [AttributeUsage (AttributeTargets.Class)]
    public class ScenarioMetadata : System.Attribute
    {
        public ScenarioMetadata (string Name, string Description)
        {
            this.Name = Name;
            this.Description = Description;
        }

        /// <summary><see cref="Scenario"/> Description</summary>
        public string Description { get; set; }

        /// <summary>Static helper function to get the <see cref="Scenario"/> Description given a Type</summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetDescription (Type t) { return ((ScenarioMetadata)GetCustomAttributes (t) [0]).Description; }

        /// <summary>Static helper function to get the <see cref="Scenario"/> Name given a Type</summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetName (Type t) { return ((ScenarioMetadata)GetCustomAttributes (t) [0]).Name; }

        /// <summary><see cref="Scenario"/> Name</summary>
        public string Name { get; set; }
    }
}
