#nullable enable

using System.Collections;
using System.Globalization;
using System.Resources;
using Terminal.Gui.Resources;

namespace Terminal.Gui.ResourcesTests;

public class ResourceManagerTests
{
    private const string DODGER_BLUE_COLOR_KEY = "DodgerBlue";
    private const string DODGER_BLUE_COLOR_NAME = "DodgerBlue";
    private const string NO_NAMED_COLOR_KEY = "#1E80FF";
    private const string NO_NAMED_COLOR_NAME = "#1E80FF";
    private const string EXISTENT_CULTURE = "pt-PT";
    private const string NO_EXISTENT_CULTURE = "de-DE";
    private const string NO_EXISTENT_KEY = "blabla";
    private const string NO_TRANSLATED_KEY = "fdDeleteTitle";
    private const string NO_TRANSLATED_VALUE = "Delete {0}";
    private const string TRANSLATED_KEY = "ctxSelectAll";
    private const string TRANSLATED_VALUE = "_Selecionar Tudo";
    private static readonly string _stringsNoTranslatedKey = Strings.fdDeleteTitle;
    private static readonly string _stringsTranslatedKey = Strings.ctxSelectAll;
    private static readonly CultureInfo _savedCulture = CultureInfo.CurrentCulture;
    private static readonly CultureInfo _savedUICulture = CultureInfo.CurrentUICulture;

    [Fact]
    public void GetObject_Does_Not_Overflows_If_Key_Does_Not_Exist () { Assert.Null (GlobalResources.GetObject (NO_EXISTENT_KEY, CultureInfo.CurrentCulture)); }

    [Fact]
    public void GetObject_FallBack_To_Default_For_No_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetObject (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));

        RestoreCurrentCultures ();
    }

    [Fact]
    public void GetObject_FallBack_To_Default_For_Not_Translated_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetObject (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));

        RestoreCurrentCultures ();
    }

    [Fact]
    public void GetResourceSet_FallBack_To_Default_For_No_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        // W3CColors.GetColorNames also calls ColorStrings.GetW3CColorNames
        string [] colorNames = new W3CColors ().GetColorNames ().ToArray ();
        Assert.Contains (DODGER_BLUE_COLOR_NAME, colorNames);
        Assert.DoesNotContain (NO_TRANSLATED_VALUE, colorNames);

        RestoreCurrentCultures ();
    }

    [Fact]
    public void GetResourceSet_FallBack_To_Default_For_Not_Translated_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (EXISTENT_CULTURE);

        // These aren't already translated
        // ColorStrings.GetW3CColorNames method uses GetResourceSet method to retrieve color names
        IEnumerable<string> colorNames = ColorStrings.GetW3CColorNames ();
        Assert.NotEmpty (colorNames);

        // W3CColors.GetColorNames also calls ColorStrings.GetW3CColorNames
        colorNames = new W3CColors ().GetColorNames ().ToArray ();
        Assert.Contains (DODGER_BLUE_COLOR_NAME, colorNames);
        Assert.DoesNotContain (NO_TRANSLATED_VALUE, colorNames);

        // ColorStrings.TryParseW3CColorName method uses GetResourceSet method to retrieve a color value
        Assert.True (ColorStrings.TryParseW3CColorName (DODGER_BLUE_COLOR_NAME, out Color color));
        Assert.Equal (DODGER_BLUE_COLOR_KEY, color.ToString ());

        // W3CColors.GetColorNames also calls ColorStrings.GetW3CColorNames for no-named colors
        colorNames = new W3CColors ().GetColorNames ().ToArray ();
        Assert.DoesNotContain (NO_NAMED_COLOR_NAME, colorNames);

        // ColorStrings.TryParseW3CColorName method uses GetResourceSet method to retrieve a color value for no-named colors
        Assert.True (ColorStrings.TryParseW3CColorName (NO_NAMED_COLOR_NAME, out color));
        Assert.Equal (NO_NAMED_COLOR_KEY, color.ToString ());

        RestoreCurrentCultures ();
    }

    [Fact]
    public void GetResourceSet_With_Filter_Does_Not_Overflows_If_Key_Does_Not_Exist ()
    {
        ResourceSet value = GlobalResources.GetResourceSet (CultureInfo.CurrentCulture, true, true, d => (string)d.Key == NO_EXISTENT_KEY)!;
        Assert.NotNull (value);
        Assert.Empty (value.Cast<DictionaryEntry> ());
    }

    [Fact]
    public void GetResourceSet_Without_Filter_Does_Not_Overflows_If_Key_Does_Not_Exist ()
    {
        ResourceSet value = GlobalResources.GetResourceSet (CultureInfo.CurrentCulture, true, true)!;
        Assert.NotNull (value);
        Assert.NotEmpty (value.Cast<DictionaryEntry> ());
    }

    [Fact]
    public void GetString_Does_Not_Overflows_If_Key_Does_Not_Exist () { Assert.Null (GlobalResources.GetString (NO_EXISTENT_KEY, CultureInfo.CurrentCulture)); }

    [Fact]
    public void GetString_FallBack_To_Default_For_No_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetString (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));

        RestoreCurrentCultures ();
    }

    [Fact]
    public void GetString_FallBack_To_Default_For_Not_Translated_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (EXISTENT_CULTURE);

        // This is really already translated
        Assert.Equal (TRANSLATED_VALUE, GlobalResources.GetString (TRANSLATED_KEY, CultureInfo.CurrentCulture));

        // These aren't already translated
        // Calling Strings.fdDeleteBody return always the invariant culture
        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetString (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));

        RestoreCurrentCultures ();
    }

    [Fact]
    public void Strings_Always_FallBack_To_Default_For_No_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        Assert.Equal (NO_TRANSLATED_VALUE, _stringsNoTranslatedKey);

        RestoreCurrentCultures ();
    }

    [Fact]
    public void Strings_Always_FallBack_To_Default_For_Not_Translated_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (EXISTENT_CULTURE);

        // This is really already translated
        Assert.Equal (TRANSLATED_VALUE, _stringsTranslatedKey);

        // This isn't already translated
        Assert.Equal (NO_TRANSLATED_VALUE, _stringsNoTranslatedKey);

        RestoreCurrentCultures ();
    }

    private void RestoreCurrentCultures ()
    {
        CultureInfo.CurrentCulture = _savedCulture;
        CultureInfo.CurrentUICulture = _savedUICulture;
    }
}
