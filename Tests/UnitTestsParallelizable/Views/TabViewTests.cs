using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests;

[TestSubject (typeof (TabView))]
public class TabViewTests : TestDriverBase
{
    /// <summary>
    ///     Verifies that <see cref="TabView" /> measures tab text width using grapheme-aware
    ///     <c>string.GetColumns()</c> rather than <c>EnumerateRunes().Sum(GetColumns)</c>.
    ///     A ZWJ family emoji should occupy 2 cells as a tab name, not 8.
    /// </summary>
    [Fact]
    public void ShowTopLine_True_TabTextWidth_GraphemeCluster ()
    {
        // setup
        IDriver driver = CreateTestDriver ();
        var tv = new TabView ()
        {
            Driver = driver,
            Id = "tv"
        };
        tv.BeginInit ();
        tv.EndInit ();

        string family = "\U0001F468\u200D\U0001F469\u200D\U0001F466\u200D\U0001F466"; // 👨‍👩‍👦‍👦

        tv.AddTab (
                   new () { Id = "emojiTab", DisplayText = family, View = new TextField { Id = "tf", Width = 4, Text = "hi" } },
                   false
                  );
        tv.AddTab (new () { Id = "tab2", DisplayText = "B", View = new Label { Id = "lbl", Text = "hi2" } }, false);
        tv.Width = 20;
        tv.Height = 5;

        // execute
        tv.Layout ();
        tv.SetClipToScreen ();
        tv.Draw ();

        // verify
        string actual = driver.ToString ()!;
        string [] lines = actual.Replace ("\r\n", "\n").Split ('\n');
        string? headerRow = lines.FirstOrDefault (l => l.Contains ('B') && l.Length > 1);
        Assert.NotNull (headerRow);

        int bIndex = headerRow.IndexOf ('B');
        int bColumnPosition = headerRow [..bIndex].GetColumns ();

        Assert.True (
                     bColumnPosition <= 8,
                     $"Tab 'B' should be near the start (emoji tab is 2 cells wide), but found at column {bColumnPosition}. Row: '{headerRow}'"
                    );
    }
}
