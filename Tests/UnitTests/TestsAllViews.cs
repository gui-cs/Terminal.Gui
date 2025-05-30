#nullable enable
using System.Drawing;
using System.Reflection;

namespace UnitTests;

/// <summary>
///     Base class for tests that need to test all views.
/// </summary>
public class TestsAllViews
{
    /// <summary>
    ///     Gets all view types.
    /// </summary>
    public static IEnumerable<object []> AllViewTypes =>
        typeof (View).Assembly
                     .GetTypes ()
                     .Where (
                             type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                                     && (type.IsSubclassOf (typeof (View)) || type == typeof (View)))
                     .Select (type => new object [] { type });

    /// <summary>
    ///    Creates an instance of a view if it is not a generic type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static View? CreateInstanceIfNotGeneric (Type type)
    {
        if (type.IsGenericType)
        {
            // Return null for generic types
            return null;
        }

        return Activator.CreateInstance (type) as View;
    }

    /// <summary>
    ///     Gets a list of all view classes.
    /// </summary>
    /// <returns></returns>
    public static List<Type> GetAllViewClasses ()
    {
        return typeof (View).Assembly.GetTypes ()
                            .Where (
                                    myType => myType is { IsClass: true, IsAbstract: false, IsPublic: true }
                                              && myType.IsSubclassOf (typeof (View))
                                   )
                            .ToList ();
    }

    /// <summary>
    ///     Creates a view from a type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="ctor">The constructor to call.</param>
    /// <returns></returns>
    public static View? CreateViewFromType (Type type, ConstructorInfo ctor)
    {
        View? viewType = null;

        if (type is { IsGenericType: true, IsTypeDefinition: true })
        {
            List<Type> typeArguments = new ();

            // use <object> or the original type if applicable
            foreach (Type arg in type.GetGenericArguments ())
            {
                if (arg.IsValueType && Nullable.GetUnderlyingType (arg) == null)
                {
                    typeArguments.Add (arg);
                }
                else
                {
                    typeArguments.Add (typeof (object));
                }
            }

            type = type.MakeGenericType (typeArguments.ToArray ());

            // Ensure the type does not contain any generic parameters
            if (type.ContainsGenericParameters)
            {
                Logging.Warning ($"Cannot create an instance of {type} because it contains generic parameters.");
                //throw new ArgumentException ($"Cannot create an instance of {type} because it contains generic parameters.");
                return null;
            }

            Assert.IsType (type, (View)Activator.CreateInstance (type)!);
        }
        else
        {
            ParameterInfo [] paramsInfo = ctor.GetParameters ();
            Type paramType;
            List<object> pTypes = new ();

            if (type.IsGenericType)
            {
                foreach (Type args in type.GetGenericArguments ())
                {
                    paramType = args.GetType ();

                    if (args.Name == "T")
                    {
                        pTypes.Add (typeof (object));
                    }
                    else
                    {
                        AddArguments (paramType, pTypes);
                    }
                }
            }

            foreach (ParameterInfo p in paramsInfo)
            {
                paramType = p.ParameterType;

                if (p.HasDefaultValue)
                {
                    pTypes.Add (p.DefaultValue!);
                }
                else
                {
                    AddArguments (paramType, pTypes);
                }
            }

            if (type is { IsGenericType: true, IsTypeDefinition: false })
            {
                viewType = Activator.CreateInstance (type) as View;
            }
            else
            {
                viewType = (View)ctor.Invoke (pTypes.ToArray ());
            }

            Assert.IsType (type, viewType);
        }

        return viewType;
    }

    private static void AddArguments (Type paramType, List<object> pTypes)
    {
        if (paramType == typeof (Rectangle))
        {
            pTypes.Add (Rectangle.Empty);
        }
        else if (paramType == typeof (string))
        {
            pTypes.Add (string.Empty);
        }
        else if (paramType == typeof (int))
        {
            pTypes.Add (0);
        }
        else if (paramType == typeof (bool))
        {
            pTypes.Add (true);
        }
        else if (paramType.Name == "IList")
        {
            pTypes.Add (new List<object> ());
        }
        else if (paramType.Name == "View")
        {
            var top = new Toplevel ();
            var view = new View ();
            top.Add (view);
            pTypes.Add (view);
        }
        else if (paramType.Name == "View[]")
        {
            pTypes.Add (new View [] { });
        }
        else if (paramType.Name == "Stream")
        {
            pTypes.Add (new MemoryStream ());
        }
        else if (paramType.Name == "String")
        {
            pTypes.Add (string.Empty);
        }
        else if (paramType.Name == "TreeView`1[T]")
        {
            pTypes.Add (string.Empty);
        }
        else
        {
            pTypes.Add (null!);
        }
    }
}
