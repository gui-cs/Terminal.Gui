using System.Reflection;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ViewDisposalTest (ITestOutputHelper output)
{
#nullable enable
    private readonly Dictionary<Type, object? []?> _special_params = new ();
#nullable restore

    [Fact]
    [AutoInitShutdown]
    public void TestViewsDisposeCorrectly ()
    {
        WeakReference reference = DoTest ();

        for (var i = 0; i < 10 && reference.IsAlive; i++)
        {
            GC.Collect ();
            GC.WaitForPendingFinalizers ();
        }
#if DEBUG_IDISPOSABLE
        if (reference.IsAlive)
        {
            Assert.True (((View)reference.Target).WasDisposed);
            Assert.Fail ($"Some Views didnt get Garbage Collected: {((View)reference.Target).SubViews}");
        }
#endif
    }

    private WeakReference DoTest ()
    {
        GetSpecialParams ();
        var container = new View () { Id = "container" };
        Toplevel top = new () { Id = "top" };
        List<Type> views = GetViews ();

        foreach (Type view in views)
        {
            View instance;

            //Create instance of view and add to container
            if (_special_params.TryGetValue (view, out object [] param))
            {
                instance = (View)Activator.CreateInstance (view, param);
            }
            else
            {
                instance = (View)Activator.CreateInstance (view);
            }

            Assert.NotNull (instance);
            instance.Id = $"{view.Name}";
            container.Add (instance);
            output.WriteLine ($"Added instance of {view}!");
        }

        top.Add (container);

        // make sure the application is doing to the views whatever its supposed to do to the views
        for (var i = 0; i < 100; i++)
        {
            Application.LayoutAndDraw ();
        }

        top.Remove (container);
        WeakReference reference = new (container, true);
        container.Dispose ();

        return reference;
    }

    private void GetSpecialParams ()
    {
        _special_params.Clear ();

        //special_params.Add (typeof (LineView), new object [] { Orientation.Horizontal });
    }

    // TODO: Consoldate this with same fn that's in AllViewsTester, ScenarioTests etc...
    /// <summary>Get all types derived from <see cref="View"/> using reflection</summary>
    /// <returns></returns>
    private List<Type> GetViews ()
    {
        List<Type> valid = new ();

        // Filter all types that can be instantiated, are public, arent generic,  aren't the view type itself, but derive from view
        foreach (Type type in Assembly.GetAssembly (typeof (View))
                                      .GetTypes ()
                                      .Where (
                                              T =>
                                              { //body of anonymous check function
                                                  return !T.IsAbstract
                                                         && T.IsPublic
                                                         && T.IsClass
                                                         && T.IsAssignableTo (typeof (View))
                                                         && !T.IsGenericType
                                                         && !(T == typeof (View));
                                              }
                                             )) //end of body of anonymous check function
        { //body of the foreach loop
            output.WriteLine ($"Found Type {type.Name}");
            Assert.DoesNotContain (type, valid);
            Assert.True (type.IsAssignableTo (typeof (IDisposable))); // Just to be safe
            valid.Add (type);
            output.WriteLine ("	-Added!");
        } //end body of foreach loop

        return valid;
    }
}
