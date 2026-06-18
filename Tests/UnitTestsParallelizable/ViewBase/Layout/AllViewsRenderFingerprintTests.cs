// Claude - Opus 4.8
// All-views render-fingerprint harness for issue #4522 verification. Draws every concrete View type
// (via EnableForDesign) onto a test driver and emits a hash of the rendered screen per view. Used to
// diff rendering before/after the LayoutSubViews content-size change: identical fingerprints prove the
// change is render-neutral across all views. Kept as a coarse all-views smoke test.

using System.Security.Cryptography;
using System.Text;
using UnitTests;

namespace ViewBaseTests.Layout;

public class AllViewsRenderFingerprintTests (ITestOutputHelper output) : TestsAllViews
{
    private static string Hash (string s)
    {
        byte [] bytes = SHA256.HashData (Encoding.UTF8.GetBytes (s));

        return Convert.ToHexString (bytes) [..16];
    }

    [Fact]
    public void AllViews_Render_Fingerprint ()
    {
        List<Type> types = GetAllViewClasses ()
                           .Where (t => !t.IsGenericType)
                           .OrderBy (t => t.FullName, StringComparer.Ordinal)
                           .ToList ();

        StringBuilder combined = new ();
        List<string> threw = [];
        var rendered = 0;

        foreach (Type type in types)
        {
            string line = FingerprintOne (type);
            output.WriteLine (line);
            combined.AppendLine (line);
            rendered++;

            if (line.Contains ("|EX:"))
            {
                threw.Add (line);
            }
        }

        output.WriteLine ($"--- {rendered} view types fingerprinted ---");
        output.WriteLine ($"COMBINED={Hash (combined.ToString ())}");

        Assert.True (rendered > 30, $"Expected to fingerprint many view types, got {rendered}.");

        // No concrete view may throw while being laid out and drawn in design mode.
        Assert.True (threw.Count == 0, $"View(s) threw during layout/draw:\n{string.Join ("\n", threw)}");
    }

    private static string FingerprintOne (Type type)
    {
        try
        {
            IDriver driver = CreateTestDriver (60, 20);

            View? view = CreateInstanceIfNotGeneric (type);

            if (view is null)
            {
                return $"{type.FullName}|GENERIC";
            }

            view.Driver = driver;

            if (view is IDesignable designable)
            {
                designable.EnableForDesign ();
            }

            view.BeginInit ();
            view.EndInit ();
            view.Layout ();

            var fingerprint = $"frame={view.Frame};needsLayout={view.NeedsLayout}";

            if (view.Visible)
            {
                view.SetNeedsDraw ();
                view.Draw ();
                fingerprint += $";screen={Hash (driver.ToString () ?? string.Empty)}";
            }

            view.Dispose ();
            driver.Dispose ();

            return $"{type.FullName}|{fingerprint}";
        }
        catch (Exception ex)
        {
            return $"{type.FullName}|EX:{ex.GetType ().Name}";
        }
    }
}
