namespace UnitTests.ViewBaseTests;

public class PaddingTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeApplication]
    public void Padding_Uses_Parent_Scheme ()
    {
        ApplicationImpl.Instance.Driver!.SetScreenSize (5, 5);
        var view = new View { App = ApplicationImpl.Instance, Height = 3, Width = 3 };
        view.Padding.EnsureView ();
        view.Padding.Thickness = new Thickness (1);
        view.Padding.Diagnostics = ViewDiagnosticFlags.Thickness;

        view.SetScheme (new Scheme { Normal = new Attribute (Color.Red, Color.Green), Focus = new Attribute (Color.Green, Color.Red) });

        Assert.Equal (ColorName16.Red, view.Padding.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (view.GetAttributeForRole (VisualRole.Normal), view.Padding.GetAttributeForRole (VisualRole.Normal));

        view.BeginInit ();
        view.EndInit ();
        view.Draw ();

        DriverAssert.AssertDriverContentsAre ("""

                                              PPP
                                              P P
                                              PPP
                                              """,
                                              output);
        DriverAssert.AssertDriverAttributesAre ("0", output, null, view.GetAttributeForRole (VisualRole.Normal));
    }
}
