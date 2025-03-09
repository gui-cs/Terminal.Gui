#nullable enable

using Terminal.Gui;

namespace UnitTests;

/// <summary>
///     Base class for tests that need to test all views.
/// </summary>
public class TestsAllViews
{
    public static IEnumerable<object []> AllViewTypes =>
        typeof (View).Assembly
                     .GetTypes ()
                     .Where (type => type.IsClass && !type.IsAbstract && type.IsPublic && (type.IsSubclassOf (typeof (View)) || type == typeof (View)))
                     .Select (type => new object [] { type });

    public static View CreateInstanceIfNotGeneric (Type type)
    {
        if (type.IsGenericType)
        {
            // Return null for generic types
            return null;
        }

        return Activator.CreateInstance (type) as View;
    }
}
