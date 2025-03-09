using System.Reflection;

namespace UnitTests;

/// <summary>
///     Helpers for View tests.
/// </summary>
public class ViewTestHelpers
{
    public static View CreateViewFromType (Type type, ConstructorInfo ctor)
    {
        View viewType = null;

        if (type.IsGenericType && type.IsTypeDefinition)
        {
            List<Type> gTypes = new ();

            foreach (Type args in type.GetGenericArguments ())
            {
                gTypes.Add (typeof (object));
            }

            type = type.MakeGenericType (gTypes.ToArray ());

            Assert.IsType (type, (View)Activator.CreateInstance (type));
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
                    pTypes.Add (p.DefaultValue);
                }
                else
                {
                    AddArguments (paramType, pTypes);
                }
            }

            if (type.IsGenericType && !type.IsTypeDefinition)
            {
                viewType = (View)Activator.CreateInstance (type);
                Assert.IsType (type, viewType);
            }
            else
            {
                viewType = (View)ctor.Invoke (pTypes.ToArray ());
                Assert.IsType (type, viewType);
            }
        }

        return viewType;
    }

    public static List<Type> GetAllViewClasses ()
    {
        return typeof (View).Assembly.GetTypes ()
                            .Where (
                                    myType => myType.IsClass
                                              && !myType.IsAbstract
                                              && myType.IsPublic
                                              && myType.IsSubclassOf (typeof (View))
                                   )
                            .ToList ();
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
            pTypes.Add (null);
        }
    }
}
