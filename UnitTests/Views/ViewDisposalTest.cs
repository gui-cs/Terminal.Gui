using System.Reflection;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ViewDisposalTest
{
    private readonly ITestOutputHelper _output;
#nullable enable
    private readonly Dictionary<Type, object? []?> _special_params = new ();
#nullable restore
    public ViewDisposalTest (ITestOutputHelper output) { _output = output; }

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
            Assert.Fail ($"Some Views didnt get Garbage Collected: {((View)reference.Target).Subviews}");
        }
#endif
    }

    private WeakReference DoTest ()
    {
        GetSpecialParams ();
        var Container = new View ();
        Toplevel top = new ();
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
            Container.Add (instance);
            _output.WriteLine ($"Added instance of {view}!");
        }

        top.Add (Container);

        // make sure the application is doing to the views whatever its supposed to do to the views
        for (var i = 0; i < 100; i++)
        {
            Application.Refresh ();
        }

        top.Remove (Container);
        WeakReference reference = new (Container, true);
        Container.Dispose ();

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
            _output.WriteLine ($"Found Type {type.Name}");
            Assert.DoesNotContain (type, valid);
            Assert.True (type.IsAssignableTo (typeof (IDisposable))); // Just to be safe
            valid.Add (type);
            _output.WriteLine ("	-Added!");
        } //end body of foreach loop

        return valid;
    }
}
