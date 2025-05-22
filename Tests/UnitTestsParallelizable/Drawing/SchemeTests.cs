#nullable enable
using System.Reflection;

namespace Terminal.Gui.DrawingTests;

public class SchemeTests
{
    [Fact]
    public void Colors_Schemes_Property_Has_Private_Setter ()
    {
        // Resharper Code Cleanup likes to remove the `private set; `
        // from the Schemes property.  This test will fail if
        // that happens.
        PropertyInfo? property = typeof (SchemeManager).GetProperty ("Schemes");
        Assert.NotNull (property);
        Assert.NotNull (property.SetMethod);
        Assert.True (property.GetSetMethod (true)!.IsPrivate);
    }

    [Fact]
    public void GetAttributeForRole_Returns_Derived_When_NotExplicitlySet ()
    {
        Attribute baseAttr = new (Color.White, Color.Black, TextStyle.None);
        Scheme scheme = new (baseAttr);

        Attribute hotFocus = scheme.GetAttributeForRole (VisualRole.HotFocus);

        Assert.Equal (baseAttr.Foreground, hotFocus.Foreground);
        Assert.Equal (baseAttr.Background.GetHighlightColor (), hotFocus.Background);
        Assert.True (hotFocus.Style.HasFlag (TextStyle.Underline));
    }

    //[Fact]
    //public void HardcodedSchemes_ExplicitAttributes_AreMarkedExplicit ()
    //{
    //    Dictionary<string, Scheme?> schemes = Scheme.GetHardCodedSchemes ().ToDictionary (StringComparer.InvariantCultureIgnoreCase);

    //    foreach (KeyValuePair<string, Scheme?> pair in schemes)
    //    {
    //        Scheme scheme = pair.Value!;
    //        string name = pair.Key;

    //        foreach (PropertyInfo prop in typeof (Scheme).GetProperties ())
    //        {
    //            if (prop.PropertyType != typeof (Attribute))
    //            {
    //                continue;
    //            }

    //            var attr = (Attribute)prop.GetValue (scheme)!;

    //            // Only validate attributes that differ from the scheme's Normal
    //            if (!ReferenceEquals (prop.Name, nameof (Scheme.Normal)) && attr != scheme.Normal)
    //            {
    //                Assert.True (attr.IsExplicitlySet, $"{name}.{prop.Name} is not explicitly set.");
    //            }
    //        }
    //    }
    //}

    [Fact]
    public void Scheme_New ()
    {
        var scheme = new Scheme ();
        var lbl = new Label ();
        lbl.SetScheme (scheme);
        lbl.Draw ();
    }

    [Fact]
    public void Schemes_Built_Ins ()
    {
        Dictionary<string, Scheme>? schemes = SchemeManager.GetSchemes ();
        Assert.NotNull (schemes);
        Assert.Equal (5, schemes.Count);
        Assert.True (schemes.ContainsKey ("TopLevel"));
        Assert.True (schemes.ContainsKey ("Base"));
        Assert.True (schemes.ContainsKey ("Dialog"));
        Assert.True (schemes.ContainsKey ("Menu"));
        Assert.True (schemes.ContainsKey ("Error"));
    }

    [Fact]
    public void Schemes_With_Same_Attributes_AreEqual ()
    {
        Attribute attr = new (Color.Red, Color.Blue, TextStyle.Bold);
        Scheme s1 = new (attr);
        Scheme s2 = new (attr);

        Assert.Equal (s1, s2);
        Assert.Equal (s1.GetHashCode (), s2.GetHashCode ());
    }
}
