#nullable enable
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "View.Scheme")]
public class SchemeTests
{
    [Fact]
    public void GetScheme_Default_ReturnsBaseScheme ()
    {
        var view = new View ();
        var baseScheme = SchemeManager.GetHardCodedSchemes ()? ["Base"];

        Assert.Equal (baseScheme, view.Scheme);
        view.Dispose ();
    }

    [Fact]
    public void SetScheme_Explicitly_SetsSchemeCorrectly ()
    {
        var view = new View ();
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];

        view.Scheme = dialogScheme;

        Assert.True (view.HasScheme);
        Assert.Equal (dialogScheme, view.Scheme);
        view.Dispose ();
    }

    [Fact]
    public void GetScheme_InheritsFromSuperView_WhenNotExplicitlySet ()
    {
        var superView = new View ();
        var subView = new View ();

        superView.Add (subView);

        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        superView.Scheme = dialogScheme;

        Assert.Equal (dialogScheme, subView.Scheme);
        Assert.False (subView.HasScheme);

        subView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void SetSchemeName_OverridesInheritedScheme ()
    {
        var view = new View ();
        view.SchemeName = "Dialog";

        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        Assert.Equal (dialogScheme, view.Scheme);
        view.Dispose ();
    }

    [Fact]
    public void GetAttribute_ReturnsCorrectAttribute_Via_Mock ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"] };
        view.Driver = new MockConsoleDriver ();
        view.Driver.SetAttribute (new Attribute (Color.Red, Color.Green));

        // Act
        var attribute = view.GetCurrentAttribute ();

        // Assert
        Assert.Equal (new Attribute (Color.Red, Color.Green), attribute);
    }

    [Fact]
    public void GetAttributeForRole_ReturnsCorrectAttribute ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"] };

        Assert.Equal (view.Scheme!.Normal, view.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (view.Scheme.HotNormal, view.GetAttributeForRole (VisualRole.HotNormal));
        Assert.Equal (view.Scheme.Focus, view.GetAttributeForRole (VisualRole.Focus));
        Assert.Equal (view.Scheme.HotFocus, view.GetAttributeForRole (VisualRole.HotFocus));
        Assert.Equal (view.Scheme.Disabled, view.GetAttributeForRole (VisualRole.Disabled));

        view.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_DisabledView_ReturnsCorrectAttribute ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"] };

        view.Enabled = false;
        Assert.Equal (view.Scheme.Disabled, view.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (view.Scheme.Disabled, view.GetAttributeForRole (VisualRole.HotNormal));

        view.Dispose ();
    }

    [Fact]
    public void SetAttributeForRole_SetsCorrectAttribute ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"] };
        view.Driver = new MockConsoleDriver ();
        view.Driver.SetAttribute (new Attribute (Color.Red, Color.Green));

        var previousAttribute = view.SetAttributeForRole (VisualRole.Focus);
        Assert.Equal (view.Scheme.Focus, view.GetCurrentAttribute ());
        Assert.NotEqual (previousAttribute, view.GetCurrentAttribute ());

        view.Dispose ();
    }

    [Fact]
    public void OnGettingScheme_Override_StopsDefaultBehavior ()
    {
        var view = new CustomView ();
        var customScheme = SchemeManager.GetHardCodedSchemes ()? ["Error"];

        Assert.Equal (customScheme, view.Scheme);
        view.Dispose ();
    }

    [Fact]
    public void OnSettingScheme_Override_PreventsSettingScheme ()
    {
        var view = new CustomView ();
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];

        view.Scheme = dialogScheme;

        Assert.NotEqual (dialogScheme, view.Scheme);
        view.Dispose ();
    }

    [Fact]
    public void GettingScheme_Event_CanOverrideScheme ()
    {
        var view = new View ();
        var customScheme = SchemeManager.GetHardCodedSchemes ()? ["Error"];

        view.GettingScheme += (sender, args) =>
        {
            args.NewScheme = customScheme;
            args.Cancel = true;
        };

        Assert.Equal (customScheme, view.Scheme);
        view.Dispose ();
    }

    [Fact]
    public void SettingScheme_Event_CanCancelSchemeChange ()
    {
        var view = new View ();
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];

        view.SettingScheme += (sender, args) => args.Cancel = true;

        view.Scheme = dialogScheme;

        Assert.NotEqual (dialogScheme, view.Scheme);
        view.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_Event_CanOverrideAttribute ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"] };
        var customAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);

        view.GettingAttributeForRole += (sender, args) =>
        {
            if (args.Role == VisualRole.Focus)
            {
                args.NewValue = customAttribute;
                args.Cancel = true;
            }
        };

        Assert.Equal (customAttribute, view.GetAttributeForRole (VisualRole.Focus));
        view.Dispose ();
    }

    [Fact]
    public void SetAttributeForRole_Event_CanCancelAttributeChange ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"] };

        view.SettingAttributeForRole += (sender, args) =>
        {
            if (args.Role == VisualRole.Focus)
            {
                args.Cancel = true;
            }
        };

        var previousAttribute = view.SetAttributeForRole (VisualRole.Focus);
        Assert.NotEqual (view.Scheme.Focus, view.GetCurrentAttribute ());
        Assert.Equal (previousAttribute, view.GetCurrentAttribute ());

        view.Dispose ();
    }

    [Fact]
    public void GetHardCodedSchemes_ReturnsExpectedSchemes ()
    {
        var schemes = View.GetHardCodedSchemes ();

        Assert.NotNull (schemes);
        Assert.Contains ("Base", schemes.Keys);
        Assert.Contains ("Dialog", schemes.Keys);
        Assert.Contains ("Error", schemes.Keys);
        Assert.Contains ("Menu", schemes.Keys);
        Assert.Contains ("Toplevel", schemes.Keys);
    }

    [Fact]
    public void SchemeName_OverridesSuperViewScheme ()
    {
        var superView = new View ();
        var subView = new View ();

        superView.Add (subView);

        subView.SchemeName = "Error";

        var errorScheme = SchemeManager.GetHardCodedSchemes ()? ["Error"];
        Assert.Equal (errorScheme, subView.Scheme);

        subView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void Scheme_DefaultsToBase_WhenNotSet ()
    {
        var view = new View ();
        var baseScheme = SchemeManager.GetHardCodedSchemes ()? ["Base"];

        Assert.Equal (baseScheme, view.Scheme);
        view.Dispose ();
    }

    [Fact]
    public void Scheme_HandlesNullSuperViewGracefully ()
    {
        var view = new View ();
        view.SchemeName = "Dialog";

        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        Assert.Equal (dialogScheme, view.Scheme);

        view.Dispose ();
    }

    private class CustomView : View
    {
        protected override bool OnGettingScheme (out Scheme? scheme)
        {
            scheme = SchemeManager.GetHardCodedSchemes ()? ["Error"];
            return true;
        }

        protected override bool OnSettingScheme (in Scheme? scheme)
        {
            return true; // Prevent setting the scheme
        }
    }
}


// Mock implementation of IConsoleDriver