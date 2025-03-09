using System.Globalization;
using System.Runtime.InteropServices;
using UnitTests;

namespace Terminal.Gui.ViewsTests;

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
        Assert.Equal (new Rectangle (0, 0, 12, 1), df.Frame);
        Assert.Equal (" 01/01/0001", df.Text);

        DateTime date = DateTime.Now;
        df = new DateField (date);
        df.Layout ();
        Assert.Equal (date, df.Date);
        Assert.Equal (1, df.CursorPosition);
        Assert.Equal (new Rectangle (0, 0, 12, 1), df.Frame);
        Assert.Equal ($" {date.ToString (CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern)}", df.Text);

        df = new DateField (date) { X = 1, Y = 2 };
        df.Layout ();
        Assert.Equal (date, df.Date);
        Assert.Equal (1, df.CursorPosition);
        Assert.Equal (new Rectangle (1, 2, 12, 1), df.Frame);
        Assert.Equal ($" {date.ToString (CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern)}", df.Text);
    }

    [Fact]
    [TestDate]
    [SetupFakeDriver]
    public void Copy_Paste ()
    {
        var df1 = new DateField (DateTime.Parse ("12/12/1971"));
        var df2 = new DateField (DateTime.Parse ("12/31/2023"));

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
    public void Using_All_Culture_StandardizeDateFormat ()
    {
        // BUGBUG: This is a workaround for the issue with the date separator in macOS. See https://github.com/gui-cs/Terminal.Gui/issues/3592
        if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            return;
        }

        CultureInfo cultureBackup = CultureInfo.CurrentCulture;

        DateTime date = DateTime.Parse ("1/1/1971");

        foreach (CultureInfo culture in CultureInfo.GetCultures (CultureTypes.NeutralCultures))
        {
            CultureInfo.CurrentCulture = culture;
            string separator = culture.DateTimeFormat.DateSeparator.Trim ();

            if (separator.Length > 1 && separator.Contains ('\u200f'))
            {
                separator = separator.Replace ("\u200f", "");
            }
            else if (culture.Name == "ar-SA" && RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
            {
                separator = " ";
            }


            string format = culture.DateTimeFormat.ShortDatePattern;
            var df = new DateField (date);

            if ((!culture.TextInfo.IsRightToLeft || (culture.TextInfo.IsRightToLeft && !df.Text.Contains ('\u200f')))
                && (format.StartsWith ('d') || format.StartsWith ('M')))
            {
                switch (culture.Name)
                {
                    case "ar-SA":
                        Assert.Equal ($" 04{separator}11{separator}1390", df.Text);

                        break;
                    case "en-SA" when RuntimeInformation.IsOSPlatform (OSPlatform.OSX):
                        Assert.Equal ($" 04{separator}11{separator}1390", df.Text);

                        break;
                    case "en-TH" when RuntimeInformation.IsOSPlatform (OSPlatform.OSX):
                        Assert.Equal ($" 01{separator}01{separator}2514", df.Text);

                        break;
                    case "th":
                    case "th-TH":
                        Assert.Equal ($" 01{separator}01{separator}2514", df.Text);

                        break;
                    default:
                        Assert.Equal ($" 01{separator}01{separator}1971", df.Text);

                        break;
                }
            }
            else if (culture.TextInfo.IsRightToLeft)
            {
                if (df.Text.Contains ('\u200f'))
                {
                    // It's a Unicode Character (U+200F) - Right-to-Left Mark (RLM)
                    Assert.True (df.Text.Contains ('\u200f'));

                    switch (culture.Name)
                    {
                        case "ar-SA":
                            Assert.Equal ($" 04‏{separator}11‏{separator}1390", df.Text);

                            break;
                        default:
                            Assert.Equal ($" 01‏{separator}01‏{separator}1971", df.Text);

                            break;
                    }
                }
                else
                {
                    switch (culture.Name)
                    {
                        case "ckb-IR":
                        case "fa":
                        case "fa-AF":
                        case "fa-IR":
                        case "lrc":
                        case "lrc-IR":
                        case "mzn":
                        case "mzn-IR":
                        case "ps":
                        case "ps-AF":
                        case "uz-Arab":
                        case "uz-Arab-AF":
                            Assert.Equal ($" 1349{separator}10{separator}11", df.Text);

                            break;
                        default:
                            Assert.Equal ($" 1971{separator}01{separator}01", df.Text);

                            break;
                    }
                }
            }
            else
            {
                switch (culture.Name)
                {
                    default:
                        Assert.Equal ($" 1971{separator}01{separator}01", df.Text);

                        break;
                }
            }
        }

        CultureInfo.CurrentCulture = cultureBackup;
    }

    [Fact]
    public void Using_Pt_Culture ()
    {
        CultureInfo cultureBackup = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo ("pt-PT");

        var df = new DateField (DateTime.Parse ("12/12/1971"))
        {
            // Move to the first 2
            CursorPosition = 2
        };

        // Type 3 over the separator
        Assert.True (df.NewKeyDownEvent (Key.D3));

        // If InvariantCulture was used this will fail but not with PT culture
        Assert.Equal (" 13/12/1971", df.Text);
        Assert.Equal ("13/12/1971", df.Date.ToString (CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
        Assert.Equal (4, df.CursorPosition);
        CultureInfo.CurrentCulture = cultureBackup;
    }
}
