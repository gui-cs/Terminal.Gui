using Xunit;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "View.Scheme")]
public class SchemeTests
{
    [Fact]
    [UnitTests.AutoInitShutdown]
    public void View_Resolves_Attributes_From_Scheme ()
    {
        View view = new Label { SchemeName = "Base" };

        foreach (VisualRole role in Enum.GetValues<VisualRole> ())
        {
            Attribute attr = view.GetAttributeForRole (role);
            Assert.NotEqual (default, attr.Foreground); // Defensive: avoid all-defaults
        }

        view.Dispose ();
    }
}
