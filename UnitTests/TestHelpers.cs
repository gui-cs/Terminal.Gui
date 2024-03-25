using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Terminal.Gui;

// This class enables test functions annotated with the [AutoInitShutdown] attribute to 
// automatically call Application.Init at start of the test and Application.Shutdown after the
// test exits. 
// 
// This is necessary because a) Application is a singleton and Init/Shutdown must be called
// as a pair, and b) all unit test functions should be atomic..
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class AutoInitShutdownAttribute : BeforeAfterTestAttribute
{
    private readonly Type _driverType;

    /// <summary>
    ///     Initializes a [AutoInitShutdown] attribute, which determines if/how Application.Init and Application.Shutdown
    ///     are automatically called Before/After a test runs.
    /// </summary>
    /// <param name="autoInit">If true, Application.Init will be called Before the test runs.</param>
    /// <param name="autoShutdown">If true, Application.Shutdown will be called After the test runs.</param>
    /// <param name="consoleDriverType">
    ///     Determines which ConsoleDriver (FakeDriver, WindowsDriver, CursesDriver, NetDriver)
    ///     will be used when Application.Init is called. If null FakeDriver will be used. Only valid if
    ///     <paramref name="autoInit"/> is true.
    /// </param>
    /// <param name="useFakeClipboard">
    ///     If true, will force the use of <see cref="FakeDriver.FakeClipboard"/>. Only valid if
    ///     <see cref="ConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.
    /// </param>
    /// <param name="fakeClipboardAlwaysThrowsNotSupportedException">
    ///     Only valid if <paramref name="autoInit"/> is true. Only
    ///     valid if <see cref="ConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.
    /// </param>
    /// <param name="fakeClipboardIsSupportedAlwaysTrue">
    ///     Only valid if <paramref name="autoInit"/> is true. Only valid if
    ///     <see cref="ConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.
    /// </param>
    /// <param name="configLocation">Determines what config file locations <see cref="ConfigurationManager"/> will load from.</param>
    public AutoInitShutdownAttribute (
        bool autoInit = true,
        Type consoleDriverType = null,
        bool useFakeClipboard = true,
        bool fakeClipboardAlwaysThrowsNotSupportedException = false,
        bool fakeClipboardIsSupportedAlwaysTrue = false,
        ConfigurationManager.ConfigLocations configLocation = ConfigurationManager.ConfigLocations.DefaultOnly
    )
    {
        AutoInit = autoInit;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
        _driverType = consoleDriverType ?? typeof (FakeDriver);
        FakeDriver.FakeBehaviors.UseFakeClipboard = useFakeClipboard;

        FakeDriver.FakeBehaviors.FakeClipboardAlwaysThrowsNotSupportedException =
            fakeClipboardAlwaysThrowsNotSupportedException;
        FakeDriver.FakeBehaviors.FakeClipboardIsSupportedAlwaysFalse = fakeClipboardIsSupportedAlwaysTrue;
        ConfigurationManager.Locations = configLocation;
    }

    private bool AutoInit { get; }

    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        // Turn off diagnostic flags in case some test left them on
        View.Diagnostics = ViewDiagnosticFlags.Off;

        if (AutoInit)
        {
            Application.Top?.Dispose ();
            Application.Shutdown ();
#if DEBUG_IDISPOSABLE
            if (Responder.Instances.Count == 0)
            {
                Assert.Empty (Responder.Instances);
            }
            else
            {
                Responder.Instances.Clear ();
            }
#endif
        }
    }

    public override void Before (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");

        if (AutoInit)
        {
#if DEBUG_IDISPOSABLE

            // Clear out any lingering Responder instances from previous tests
            if (Responder.Instances.Count == 0)
            {
                Assert.Empty (Responder.Instances);
            }
            else
            {
                Responder.Instances.Clear ();
            }
#endif
            Application.Init ((ConsoleDriver)Activator.CreateInstance (_driverType));
        }
    }
}

[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class TestRespondersDisposed : BeforeAfterTestAttribute
{
    public TestRespondersDisposed () { CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US"); }

    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");
#if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
#endif
    }

    public override void Before (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");
#if DEBUG_IDISPOSABLE

        // Clear out any lingering Responder instances from previous tests
        Responder.Instances.Clear ();
        Assert.Empty (Responder.Instances);
#endif
    }
}

[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class SetupFakeDriverAttribute : BeforeAfterTestAttribute
{
    /// <summary>
    ///     Enables test functions annotated with the [SetupFakeDriver] attribute to set Application.Driver to new
    ///     FakeDriver(). The driver is setup with 25 rows and columns.
    /// </summary>
    public SetupFakeDriverAttribute () { }

    public override void After (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        // Turn off diagnostic flags in case some test left them on
        View.Diagnostics = ViewDiagnosticFlags.Off;

        Application.Driver = null;
    }

    public override void Before (MethodInfo methodUnderTest)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");
        Assert.Null (Application.Driver);
        Application.Driver = new FakeDriver { Rows = 25, Cols = 25 };
    }
}

[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class TestDateAttribute : BeforeAfterTestAttribute
{
    private readonly CultureInfo _currentCulture = CultureInfo.CurrentCulture;
    public TestDateAttribute () { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; }

    public override void After (MethodInfo methodUnderTest)
    {
        CultureInfo.CurrentCulture = _currentCulture;
        Assert.Equal (CultureInfo.CurrentCulture, _currentCulture);
    }

    public override void Before (MethodInfo methodUnderTest) { Assert.Equal (CultureInfo.CurrentCulture, CultureInfo.InvariantCulture); }
}

internal partial class TestHelpers
{
    private const char SpaceChar = ' ';
    private static readonly Rune SpaceRune = (Rune)SpaceChar;
#pragma warning disable xUnit1013 // Public method should be marked as test
    /// <summary>
    ///     Verifies <paramref name="expectedAttributes"/> are found at the locations specified by
    ///     <paramref name="expectedLook"/>. <paramref name="expectedLook"/> is a bitmap of indexes into
    ///     <paramref name="expectedAttributes"/> (e.g. "00110" means the attribute at <c>expectedAttributes[1]</c> is expected
    ///     at the 3rd and 4th columns of the 1st row of driver.Contents).
    /// </summary>
    /// <param name="expectedLook">
    ///     Numbers between 0 and 9 for each row/col of the console.  Must be valid indexes into
    ///     <paramref name="expectedAttributes"/>.
    /// </param>
    /// <param name="driver">The ConsoleDriver to use. If null <see cref="Application.Driver"/> will be used.</param>
    /// <param name="expectedAttributes"></param>
    public static void AssertDriverAttributesAre (
        string expectedLook,
        ConsoleDriver driver = null,
        params Attribute [] expectedAttributes
    )
    {
#pragma warning restore xUnit1013 // Public method should be marked as test

        if (expectedAttributes.Length > 10)
        {
            throw new ArgumentException ("This method only works for UIs that use at most 10 colors");
        }

        expectedLook = expectedLook.Trim ();
        driver ??= Application.Driver;

        Cell [,] contents = driver.Contents;

        var line = 0;

        foreach (string lineString in expectedLook.Split ('\n').Select (l => l.Trim ()))
        {
            for (var c = 0; c < lineString.Length; c++)
            {
                Attribute? val = contents [line, c].Attribute;

                List<Attribute> match = expectedAttributes.Where (e => e == val).ToList ();

                switch (match.Count)
                {
                    case 0:
                        throw new Exception (
                                             $"{DriverContentsToString (driver)}\n"
                                             + $"Expected Attribute {val} (PlatformColor = {val.Value.PlatformColor}) at Contents[{line},{c}] {contents [line, c]} ((PlatformColor = {contents [line, c].Attribute.Value.PlatformColor}) was not found.\n"
                                             + $"  Expected: {string.Join (",", expectedAttributes.Select (c => c))}\n"
                                             + $"  But Was: <not found>"
                                            );
                    case > 1:
                        throw new ArgumentException (
                                                     $"Bad value for expectedColors, {match.Count} Attributes had the same Value"
                                                    );
                }

                char colorUsed = Array.IndexOf (expectedAttributes, match [0]).ToString () [0];
                char userExpected = lineString [c];

                if (colorUsed != userExpected)
                {
                    throw new Exception (
                                         $"{DriverContentsToString (driver)}\n"
                                         + $"Unexpected Attribute at Contents[{line},{c}] {contents [line, c]}.\n"
                                         + $"  Expected: {userExpected} ({expectedAttributes [int.Parse (userExpected.ToString ())]})\n"
                                         + $"  But Was:   {colorUsed} ({val})\n"
                                        );
                }
            }

            line++;
        }
    }

#pragma warning disable xUnit1013 // Public method should be marked as test
    /// <summary>Asserts that the driver contents match the expected contents, optionally ignoring any trailing whitespace.</summary>
    /// <param name="expectedLook"></param>
    /// <param name="output"></param>
    /// <param name="driver">The ConsoleDriver to use. If null <see cref="Application.Driver"/> will be used.</param>
    /// <param name="ignoreLeadingWhitespace"></param>
    public static void AssertDriverContentsAre (
        string expectedLook,
        ITestOutputHelper output,
        ConsoleDriver driver = null,
        bool ignoreLeadingWhitespace = false
    )
    {
#pragma warning restore xUnit1013 // Public method should be marked as test
        string actualLook = DriverContentsToString (driver);

        if (string.Equals (expectedLook, actualLook))
        {
            return;
        }

        // get rid of trailing whitespace on each line (and leading/trailing whitespace of start/end of full string)
        expectedLook = TrailingWhiteSpaceRegEx ().Replace (expectedLook, "").Trim ();
        actualLook = TrailingWhiteSpaceRegEx ().Replace (actualLook, "").Trim ();

        if (ignoreLeadingWhitespace)
        {
            expectedLook = LeadingWhitespaceRegEx ().Replace (expectedLook, "").Trim ();
            actualLook = LeadingWhitespaceRegEx ().Replace (actualLook, "").Trim ();
        }

        // standardize line endings for the comparison
        expectedLook = expectedLook.Replace ("\r\n", "\n");
        actualLook = actualLook.Replace ("\r\n", "\n");

        // If test is about to fail show user what things looked like
        if (!string.Equals (expectedLook, actualLook))
        {
            output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
            output?.WriteLine (" But Was:" + Environment.NewLine + actualLook);
        }

        Assert.Equal (expectedLook, actualLook);
    }

    /// <summary>
    ///     Asserts that the driver contents are equal to the expected look, and that the cursor is at the expected
    ///     position.
    /// </summary>
    /// <param name="expectedLook"></param>
    /// <param name="output"></param>
    /// <param name="driver">The ConsoleDriver to use. If null <see cref="Application.Driver"/> will be used.</param>
    /// <returns></returns>
    public static Rectangle AssertDriverContentsWithFrameAre (
        string expectedLook,
        ITestOutputHelper output,
        ConsoleDriver driver = null
    )
    {
        List<List<Rune>> lines = new ();
        var sb = new StringBuilder ();
        driver ??= Application.Driver;
        int x = -1;
        int y = -1;
        int w = -1;
        int h = -1;

        Cell [,] contents = driver.Contents;


        for (var rowIndex = 0; rowIndex < driver.Rows; rowIndex++)
        {
            List<Rune> runes = [];

            for (var colIndex = 0; colIndex < driver.Cols; colIndex++)
            {
                Rune runeAtCurrentLocation = contents [rowIndex, colIndex].Rune;

                if (runeAtCurrentLocation != SpaceRune)
                {
                    if (x == -1)
                    {
                        x = colIndex;
                        y = rowIndex;

                        for (int i = 0; i < colIndex; i++)
                        {
                            runes.InsertRange (i, [SpaceRune]);
                        }
                    }

                    if (runeAtCurrentLocation.GetColumns () > 1)
                    {
                        colIndex++;
                    }

                    if (colIndex + 1 > w)
                    {
                        w = colIndex + 1;
                    }

                    h = rowIndex - y + 1;
                }

                if (x > -1)
                {
                    runes.Add (runeAtCurrentLocation);
                }

                // See Issue #2616
                //foreach (var combMark in contents [r, c].CombiningMarks) {
                //	runes.Add (combMark);
                //}
            }

            if (runes.Count > 0)
            {
                lines.Add (runes);
            }
        }

        // Remove unnecessary empty lines
        if (lines.Count > 0)
        {
            for (int r = lines.Count - 1; r > h - 1; r--)
            {
                lines.RemoveAt (r);
            }
        }

        // Remove trailing whitespace on each line
        foreach (List<Rune> row in lines)
        {
            for (int c = row.Count - 1; c >= 0; c--)
            {
                Rune rune = row [c];

                if (rune != (Rune)' ' || row.Sum (x => x.GetColumns ()) == w)
                {
                    break;
                }

                row.RemoveAt (c);
            }
        }

        // Convert Rune list to string
        for (var r = 0; r < lines.Count; r++)
        {
            var line = StringExtensions.ToString (lines [r]);

            if (r == lines.Count - 1)
            {
                sb.Append (line);
            }
            else
            {
                sb.AppendLine (line);
            }
        }

        var actualLook = sb.ToString ();

        if (string.Equals (expectedLook, actualLook))
        {
            return new Rectangle (x > -1 ? x : 0, y > -1 ? y : 0, w > -1 ? w : 0, h > -1 ? h : 0);
        }

        // standardize line endings for the comparison
        expectedLook = expectedLook.ReplaceLineEndings ();
        actualLook = actualLook.ReplaceLineEndings ();

        // Remove the first and the last line ending from the expectedLook
        if (expectedLook.StartsWith (Environment.NewLine))
        {
            expectedLook = expectedLook [Environment.NewLine.Length..];
        }

        if (expectedLook.EndsWith (Environment.NewLine))
        {
            expectedLook = expectedLook [..^Environment.NewLine.Length];
        }

        Assert.Equal (expectedLook, actualLook);

        return new Rectangle (x > -1 ? x : 0, y > -1 ? y : 0, w > -1 ? w : 0, h > -1 ? h : 0);
    }

#pragma warning disable xUnit1013 // Public method should be marked as test
    /// <summary>
    ///     Verifies two strings are equivalent. If the assert fails, output will be generated to standard output showing
    ///     the expected and actual look.
    /// </summary>
    /// <param name="output"></param>
    /// <param name="expectedLook">
    ///     A string containing the expected look. Newlines should be specified as "\r\n" as they will
    ///     be converted to <see cref="Environment.NewLine"/> to make tests platform independent.
    /// </param>
    /// <param name="actualLook"></param>
    public static void AssertEqual (ITestOutputHelper output, string expectedLook, string actualLook)
    {
        // Convert newlines to platform-specific newlines
        expectedLook = ReplaceNewLinesToPlatformSpecific (expectedLook);

        // If test is about to fail show user what things looked like
        if (!string.Equals (expectedLook, actualLook))
        {
            output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
            output?.WriteLine (" But Was:" + Environment.NewLine + actualLook);
        }

        Assert.Equal (expectedLook, actualLook);
    }
#pragma warning restore xUnit1013 // Public method should be marked as test

    public static string DriverContentsToString (ConsoleDriver driver = null)
    {
        var sb = new StringBuilder ();
        driver ??= Application.Driver;

        Cell [,] contents = driver.Contents;

        for (var r = 0; r < driver.Rows; r++)
        {
            for (var c = 0; c < driver.Cols; c++)
            {
                Rune rune = contents [r, c].Rune;

                if (rune.DecodeSurrogatePair (out char [] sp))
                {
                    sb.Append (sp);
                }
                else
                {
                    sb.Append ((char)rune.Value);
                }

                if (rune.GetColumns () > 1)
                {
                    c++;
                }

                // See Issue #2616
                //foreach (var combMark in contents [r, c].CombiningMarks) {
                //	sb.Append ((char)combMark.Value);
                //}
            }

            sb.AppendLine ();
        }

        return sb.ToString ();
    }

    // TODO: Update all tests that use GetALlViews to use GetAllViewsTheoryData instead
    /// <summary>Gets a list of instances of all classes derived from View.</summary>
    /// <returns>List of View objects</returns>
    public static List<View> GetAllViews ()
    {
        return typeof (View).Assembly.GetTypes ()
                            .Where (
                                    type => type.IsClass
                                            && !type.IsAbstract
                                            && type.IsPublic
                                            && type.IsSubclassOf (typeof (View))
                                   )
                            .Select (type => CreateView (type, type.GetConstructor (Array.Empty<Type> ())))
                            .ToList ();
    }

    public static TheoryData<View, string> GetAllViewsTheoryData ()
    {
        // TODO: Figure out how to simplify this. I couldn't figure out how to not have to iterate over ret.
        (View view, string name)[] ret =
            typeof (View).Assembly
                               .GetTypes ()
                               .Where (
                                       type => type.IsClass
                                               && !type.IsAbstract
                                               && type.IsPublic
                                               && type.IsSubclassOf (typeof (View))
                                      )
                               .Select (
                                        type => (
                                                    view: CreateView (
                                                                   type, type.GetConstructor (Array.Empty<Type> ())),
                                                    name: type.Name)
                                        ).ToArray();

        TheoryData<View, string> td = new ();
        foreach ((View view, string name) in ret)
        {
            td.Add(view, name);
        }

        return td;
    }


    /// <summary>
    ///     Verifies the console used all the <paramref name="expectedColors"/> when rendering. If one or more of the
    ///     expected colors are not used then the failure will output both the colors that were found to be used and which of
    ///     your expectations was not met.
    /// </summary>
    /// <param name="driver">if null uses <see cref="Application.Driver"/></param>
    /// <param name="expectedColors"></param>
    internal static void AssertDriverUsedColors (ConsoleDriver driver = null, params Attribute [] expectedColors)
    {
        driver ??= Application.Driver;
        Cell [,] contents = driver.Contents;

        List<Attribute> toFind = expectedColors.ToList ();

        // Contents 3rd column is an Attribute
        HashSet<Attribute> colorsUsed = new ();

        for (var r = 0; r < driver.Rows; r++)
        {
            for (var c = 0; c < driver.Cols; c++)
            {
                Attribute? val = contents [r, c].Attribute;

                if (val.HasValue)
                {
                    colorsUsed.Add (val.Value);

                    Attribute match = toFind.FirstOrDefault (e => e == val);

                    // need to check twice because Attribute is a struct and therefore cannot be null
                    if (toFind.Any (e => e == val))
                    {
                        toFind.Remove (match);
                    }
                }
            }
        }

        if (!toFind.Any ())
        {
            return;
        }

        var sb = new StringBuilder ();
        sb.AppendLine ("The following colors were not used:" + string.Join ("; ", toFind.Select (a => a.ToString ())));
        sb.AppendLine ("Colors used were:" + string.Join ("; ", colorsUsed.Select (a => a.ToString ())));

        throw new Exception (sb.ToString ());
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

    public static View CreateView (Type type, ConstructorInfo ctor)
    {
        View view = null;

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
                view = (View)Activator.CreateInstance (type);
                Assert.IsType (type, view);
            }
            else
            {
                view = (View)ctor.Invoke (pTypes.ToArray ());
                Assert.IsType (type, view);
            }
        }

        return view;
    }

    [GeneratedRegex ("^\\s+", RegexOptions.Multiline)]
    private static partial Regex LeadingWhitespaceRegEx ();

    private static string ReplaceNewLinesToPlatformSpecific (string toReplace)
    {
        string replaced = toReplace;

        replaced = Environment.NewLine.Length switch
        {
            2 when !replaced.Contains ("\r\n") => replaced.Replace ("\n", Environment.NewLine),
            1 => replaced.Replace ("\r\n", Environment.NewLine),
            var _ => replaced
        };

        return replaced;
    }

    [GeneratedRegex ("\\s+$", RegexOptions.Multiline)]
    private static partial Regex TrailingWhiteSpaceRegEx ();
}
