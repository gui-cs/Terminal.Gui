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
    public void New ()
    {
        var scheme = new Scheme ();
        var lbl = new Label ();
        lbl.SetScheme (scheme);
        lbl.Draw ();
    }

    [Fact]
    public void Built_Ins ()
    {
        Dictionary<string, Scheme?> schemes = SchemeManager.GetSchemes ();
        Assert.NotNull (schemes);
        Assert.Equal (5, schemes.Count);
        Assert.True (schemes.ContainsKey ("Base"));
        Assert.True (schemes.ContainsKey ("Dialog"));
        Assert.True (schemes.ContainsKey ("Error"));
        Assert.True (schemes.ContainsKey ("Menu"));
        Assert.True (schemes.ContainsKey ("TopLevel"));
    }

    [Fact]
    public void Built_Ins_Are_Implicit ()
    {
        Dictionary<string, Scheme?> schemes = SchemeManager.GetSchemes ();

        Assert.True (schemes ["Base"]!.TryGetExplicitlySetAttributeForRole (VisualRole.Normal, out _));
        Assert.False (schemes ["Base"]!.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out _));
    }


    [Fact]
    public void With_Same_Attributes_AreEqual ()
    {
        Attribute attr = new (Color.Red, Color.Blue, TextStyle.Bold);
        Scheme s1 = new (attr);
        Scheme s2 = new (attr);

        Assert.Equal (s1, s2);
        Assert.Equal (s1.GetHashCode (), s2.GetHashCode ());
    }

    [Fact]
    public void Scheme_Properties_Are_Immutable ()
    {
        Scheme scheme = new (new Attribute ("Red", "Blue"));
        // The following line should not compile if uncommented:
        // scheme.Normal = new Attribute("Green", "Yellow");
        // Immutability is enforced by the C# compiler for init-only properties.
        Assert.True (true); // This test is a placeholder for documentation purposes.
    }

    [Fact]
    public void ObjectInitializer_Sets_Properties ()
    {
        Scheme scheme = new ()
        {
            Normal = new Attribute ("Red", "Blue"),
            Focus = new Attribute ("Green", "Yellow"),
            HotNormal = new Attribute ("White", "Black"),
            HotFocus = new Attribute ("Black", "White"),
            Active = new Attribute ("Cyan", "Magenta"),
            HotActive = new Attribute ("Magenta", "Cyan"),
            Highlight = new Attribute ("Yellow", "Red"),
            Editable = new Attribute ("Blue", "Yellow"),
            ReadOnly = new Attribute ("Gray", "Black"),
            Disabled = new Attribute ("DarkGray", "White")
        };

        Assert.Equal (new Attribute ("Red", "Blue"), scheme.Normal);
        Assert.Equal (new Attribute ("Green", "Yellow"), scheme.Focus);
        Assert.Equal (new Attribute ("White", "Black"), scheme.HotNormal);
        Assert.Equal (new Attribute ("Black", "White"), scheme.HotFocus);
        Assert.Equal (new Attribute ("Cyan", "Magenta"), scheme.Active);
        Assert.Equal (new Attribute ("Magenta", "Cyan"), scheme.HotActive);
        Assert.Equal (new Attribute ("Yellow", "Red"), scheme.Highlight);
        Assert.Equal (new Attribute ("Blue", "Yellow"), scheme.Editable);
        Assert.Equal (new Attribute ("Gray", "Black"), scheme.ReadOnly);
        Assert.Equal (new Attribute ("DarkGray", "White"), scheme.Disabled);
    }

    [Fact]
    public void With_Different_Attributes_AreNotEqual ()
    {
        Scheme s1 = new (new Attribute ("Red", "Blue"));
        Scheme s2 = new (new Attribute ("Green", "Yellow"));
        Assert.NotEqual (s1, s2);
        Assert.NotEqual (s1.GetHashCode (), s2.GetHashCode ());
    }

    [Fact]
    public void Default_Constructor_Has_Default_Values ()
    {
        Scheme scheme = new ();
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Normal, out _));

        // All other roles should be implicit and derived from Normal
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Focus, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotFocus, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotActive, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Highlight, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Editable, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.ReadOnly, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Disabled, out _));
    }

    [Fact]
    public void ToString_Outputs_All_Properties ()
    {
        Scheme scheme = new (new Attribute ("Red", "Blue"));
        string str = scheme.ToString ();
        Assert.Contains ("Normal", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("HotNormal", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("Focus", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("HotFocus", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("Active", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("HotActive", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("Highlight", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("Editable", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("ReadOnly", str, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("Disabled", str, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CopyConstructor_Null_Throws ()
    {
        Assert.Throws<ArgumentNullException> (() => new Scheme (null));
    }

    [Fact]
    public void Is_Thread_Safe_For_Concurrent_Reads ()
    {
        Scheme scheme = new (new Attribute ("Red", "Blue"));
        Parallel.For (0, 1000, i =>
                               {
                                   // All threads can safely read properties
                                   _ = scheme.Normal;
                                   _ = scheme.GetAttributeForRole (VisualRole.Focus);
                                   _ = scheme.ToString ();
                               });
    }
}
