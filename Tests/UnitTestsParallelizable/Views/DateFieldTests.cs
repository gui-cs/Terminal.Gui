#nullable enable
using System.Globalization;
using System.Runtime.InteropServices;

namespace UnitTests_Parallelizable.ViewsTests;

public class DateFieldTests
{
    [Fact]
    [TestDate]
    public void Constructors_Defaults ()
    {
        var df = new DateField ();
        df.Layout ();
        Assert.Equal (DateTime.MinValue, df.Date);
        Assert.Equal (1, df.CursorPosition);
        Assert.Equal (new (0, 0, 12, 1), df.Frame);
        Assert.Equal (" 01/01/0001", df.Text);

        DateTime date = DateTime.Now;
        df = new (date);
        df.Layout ();
        Assert.Equal (date, df.Date);
        Assert.Equal (1, df.CursorPosition);
        Assert.Equal (new (0, 0, 12, 1), df.Frame);
        Assert.Equal ($" {date.ToString (CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern)}", df.Text);

        df = new (date) { X = 1, Y = 2 };
        df.Layout ();
        Assert.Equal (date, df.Date);
        Assert.Equal (1, df.CursorPosition);
        Assert.Equal (new (1, 2, 12, 1), df.Frame);
        Assert.Equal ($" {date.ToString (CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern)}", df.Text);
    }

    [Fact]
    [TestDate]
    public void Copy_Paste ()
    {
        IApplication app = Application.Create();
        app.Init("fake");

        try
        {
            var df1 = new DateField (DateTime.Parse ("12/12/1971")) { App = app };
            var df2 = new DateField (DateTime.Parse ("12/31/2023")) { App = app };

            // Select all text
            Assert.True (df2.NewKeyDownEvent (Key.End.WithShift));
            Assert.Equal (1, df2.SelectedStart);
            Assert.Equal (10, df2.SelectedLength);
            Assert.Equal (11, df2.CursorPosition);

            // Copy from df2
            Assert.True (df2.NewKeyDownEvent (Key.C.WithCtrl));

            // Paste into df1
            Assert.True (df1.NewKeyDownEvent (Key.V.WithCtrl));
            Assert.Equal (" 12/31/2023", df1.Text);
            Assert.Equal (11, df1.CursorPosition);
        }
        finally
        {
            app.Shutdown();
        }
    }

    [Fact]
    [TestDate]
    public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format ()
    {
        var df = new DateField ();
        Assert.Equal (1, df.CursorPosition);
        df.CursorPosition = 0;
        Assert.Equal (1, df.CursorPosition);
        df.CursorPosition = 11;
        Assert.Equal (10, df.CursorPosition);
    }

    [Fact]
    [TestDate]
    public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format_After_Selection ()
    {
        var df = new DateField ();

        // Start selection
        Assert.True (df.NewKeyDownEvent (Key.CursorLeft.WithShift));
        Assert.Equal (1, df.SelectedStart);
        Assert.Equal (1, df.SelectedLength);
        Assert.Equal (0, df.CursorPosition);

        // Without selection
        Assert.True (df.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (-1, df.SelectedStart);
        Assert.Equal (0, df.SelectedLength);
        Assert.Equal (1, df.CursorPosition);
        df.CursorPosition = 10;
        Assert.True (df.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (10, df.SelectedStart);
        Assert.Equal (1, df.SelectedLength);
        Assert.Equal (11, df.CursorPosition);
        Assert.True (df.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (-1, df.SelectedStart);
        Assert.Equal (0, df.SelectedLength);
        Assert.Equal (10, df.CursorPosition);
    }

    [Fact]
    [TestDate]
    public void Date_Start_From_01_01_0001_And_End_At_12_31_9999 ()
    {
        var df = new DateField (DateTime.Parse ("01/01/0001"));
        Assert.Equal (" 01/01/0001", df.Text);
        df.Date = DateTime.Parse ("12/31/9999");
        Assert.Equal (" 12/31/9999", df.Text);
    }

    [Fact]
    [TestDate]
    public void KeyBindings_Command ()
    {
        var df = new DateField (DateTime.Parse ("12/12/1971")) { ReadOnly = true };
        Assert.True (df.NewKeyDownEvent (Key.Delete));
        Assert.Equal (" 12/12/1971", df.Text);
        df.ReadOnly = false;
        Assert.True (df.NewKeyDownEvent (Key.D.WithCtrl));
        Assert.Equal (" 02/12/1971", df.Text);
        df.CursorPosition = 4;
        df.ReadOnly = true;
        Assert.True (df.NewKeyDownEvent (Key.Delete));
        Assert.Equal (" 02/12/1971", df.Text);
        df.ReadOnly = false;
        Assert.True (df.NewKeyDownEvent (Key.Backspace));
        Assert.Equal (" 02/02/1971", df.Text);
        Assert.True (df.NewKeyDownEvent (Key.Home));
        Assert.Equal (1, df.CursorPosition);
        Assert.True (df.NewKeyDownEvent (Key.End));
        Assert.Equal (10, df.CursorPosition);
        Assert.True (df.NewKeyDownEvent (Key.E.WithCtrl));
        Assert.Equal (10, df.CursorPosition);
        Assert.True (df.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (9, df.CursorPosition);
        Assert.True (df.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (10, df.CursorPosition);

        // Non-numerics are ignored
        Assert.False (df.NewKeyDownEvent (Key.A));
        df.ReadOnly = true;
        df.CursorPosition = 1;
        Assert.True (df.NewKeyDownEvent (Key.D1));
        Assert.Equal (" 02/02/1971", df.Text);
        df.ReadOnly = false;
        Assert.True (df.NewKeyDownEvent (Key.D1));
        Assert.Equal (" 12/02/1971", df.Text);
        Assert.Equal (2, df.CursorPosition);
#if UNIX_KEY_BINDINGS
        Assert.True (df.NewKeyDownEvent (Key.D.WithAlt));
        Assert.Equal (" 10/02/1971", df.Text);
#endif
    }

    [Fact]
    [TestDate]
    public void Typing_With_Selection_Normalize_Format ()
    {
        var df = new DateField (DateTime.Parse ("12/12/1971"))
        {
            // Start selection at before the first separator /
            CursorPosition = 2
        };

        // Now select the separator /
        Assert.True (df.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (2, df.SelectedStart);
        Assert.Equal (1, df.SelectedLength);
        Assert.Equal (3, df.CursorPosition);

        // Type 3 over the separator
        Assert.True (df.NewKeyDownEvent (Key.D3));

        // The format was normalized and replaced again with /
        Assert.Equal (" 12/12/1971", df.Text);
        Assert.Equal (4, df.CursorPosition);
    }

    [Fact]
    [TestDate]
    public void Culture_Pt_Portuguese ()
    {
        CultureInfo cultureBackup = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new ("pt-PT");

            var df = new DateField (DateTime.Parse ("12/12/1971"))
            {
                // Move to the first 2
                CursorPosition = 2
            };

            // Type 3 over the separator
            Assert.True (df.NewKeyDownEvent (Key.D3));

            // If InvariantCulture was used this will fail but not with PT culture
            Assert.Equal (" 13/12/1971", df.Text);
            Assert.Equal ("13/12/1971", df.Date!.Value.ToString (CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            Assert.Equal (4, df.CursorPosition);
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureBackup;
        }
    }

    /// <summary>
    ///     Tests specific culture date formatting edge cases.
    ///     Split from the monolithic culture test for better isolation and maintainability.
    /// </summary>
    [Theory]
    [TestDate]
    [InlineData ("en-US", "01/01/1971", '/')]
    [InlineData ("en-GB", "01/01/1971", '/')]
    [InlineData ("de-DE", "01.01.1971", '.')]
    [InlineData ("fr-FR", "01/01/1971", '/')]
    [InlineData ("es-ES", "01/01/1971", '/')]
    [InlineData ("it-IT", "01/01/1971", '/')]
    [InlineData ("ja-JP", "1971/01/01", '/')]
    [InlineData ("zh-CN", "1971/01/01", '/')]
    [InlineData ("ko-KR", "1971.01.01", '.')]
    [InlineData ("pt-PT", "01/01/1971", '/')]
    [InlineData ("pt-BR", "01/01/1971", '/')]
    [InlineData ("ru-RU", "01.01.1971", '.')]
    [InlineData ("nl-NL", "01-01-1971", '-')]
    [InlineData ("sv-SE", "1971-01-01", '-')]
    [InlineData ("pl-PL", "01.01.1971", '.')]
    [InlineData ("tr-TR", "01.01.1971", '.')]
    public void Culture_SpecificCultures_ProducesExpectedFormat (string cultureName, string expectedDate, char expectedSeparator)
    {
        // Skip cultures that may have platform-specific issues
        if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            // macOS has known issues with certain cultures - see #3592
            string [] problematicOnMac = { "ar-SA", "en-SA", "en-TH", "th", "th-TH" };

            if (problematicOnMac.Contains (cultureName))
            {
                return;
            }
        }

        CultureInfo cultureBackup = CultureInfo.CurrentCulture;

        try
        {
            var culture = new CultureInfo (cultureName);

            // Parse date using InvariantCulture BEFORE changing CurrentCulture
            DateTime date = DateTime.Parse ("1/1/1971", CultureInfo.InvariantCulture);

            CultureInfo.CurrentCulture = culture;

            var df = new DateField (date);

            // Verify the text contains the expected separator
            Assert.Contains (expectedSeparator, df.Text);

            // Verify the date is formatted correctly (accounting for leading space)
            Assert.Equal ($" {expectedDate}", df.Text);
        }
        catch (CultureNotFoundException)
        {
            // Skip cultures not available on this system
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureBackup;
        }
    }

    /// <summary>
    ///     Tests right-to-left cultures separately due to their complexity.
    /// </summary>
    [Theory]
    [TestDate]
    [InlineData ("ar-SA")] // Arabic (Saudi Arabia)
    [InlineData ("he-IL")] // Hebrew (Israel)
    [InlineData ("fa-IR")] // Persian (Iran)
    public void Culture_RightToLeft_HandlesFormatting (string cultureName)
    {
        if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            // macOS has known issues with RTL cultures - see #3592
            return;
        }

        CultureInfo cultureBackup = CultureInfo.CurrentCulture;

        try
        {
            var culture = new CultureInfo (cultureName);

            // Parse date using InvariantCulture BEFORE changing CurrentCulture
            // This is critical because RTL cultures may use different calendars
            DateTime date = DateTime.Parse ("1/1/1971", CultureInfo.InvariantCulture);

            CultureInfo.CurrentCulture = culture;

            var df = new DateField (date);

            // Just verify DateField doesn't crash with RTL cultures
            // and produces some text
            Assert.NotEmpty (df.Text);
            Assert.NotNull (df.Date);
        }
        catch (CultureNotFoundException)
        {
            // Skip cultures not available on this system
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureBackup;
        }
    }

    /// <summary>
    ///     Tests that DateField handles calendar systems that differ from Gregorian.
    /// </summary>
    [Theory]
    [TestDate]
    [InlineData ("th-TH")] // Thai Buddhist calendar
    public void Culture_NonGregorianCalendar_HandlesFormatting (string cultureName)
    {
        if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            // macOS has known issues with certain calendars - see #3592
            return;
        }

        CultureInfo cultureBackup = CultureInfo.CurrentCulture;

        try
        {
            var culture = new CultureInfo (cultureName);

            // Parse date using InvariantCulture BEFORE changing CurrentCulture
            DateTime date = DateTime.Parse ("1/1/1971", CultureInfo.InvariantCulture);

            CultureInfo.CurrentCulture = culture;

            var df = new DateField (date);

            // Buddhist calendar is 543 years ahead (1971 + 543 = 2514)
            // Just verify it doesn't crash and produces valid output
            Assert.NotEmpty (df.Text);
            Assert.NotNull (df.Date);
        }
        catch (CultureNotFoundException)
        {
            // Skip cultures not available on this system
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureBackup;
        }
    }
}
