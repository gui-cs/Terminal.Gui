using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
///     Tests of the <see cref="View.Text"/> and <see cref="View.TextFormatter"/> properties (independent of
///     AutoSize).
/// </summary>
public class TextTests {
    private readonly ITestOutputHelper _output;
    public TextTests (ITestOutputHelper output) { _output = output; }

    // Test that View.PreserveTrailingSpaces removes trailing spaces
    [Fact]
    public void PreserveTrailingSpaces_Removes_Trailing_Spaces () {
        var view = new View {
                                Text = "Hello World "
                            };
        Assert.Equal ("Hello World ", view.TextFormatter.Text);

        view.TextFormatter.WordWrap = true;
        view.TextFormatter.Size = new Size (5, 3);

        view.PreserveTrailingSpaces = false;
        Assert.Equal ($"Hello{Environment.NewLine}World", view.TextFormatter.Format ());

        view.PreserveTrailingSpaces = true;
        Assert.Equal ($"Hello{Environment.NewLine} {Environment.NewLine}World", view.TextFormatter.Format ());
    }

    // View.PreserveTrailingSpaces Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
    // or not when <see cref="TextFormatter.WordWrap"/> is enabled.
    // If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when
    // <see cref = "Text" / > is formatted for display.The default is <see langword = "false" / >.
    [Fact]
    public void PreserveTrailingSpaces_Set_Get () {
        var view = new View {
                                Text = "Hello World"
                            };

        Assert.False (view.PreserveTrailingSpaces);

        view.PreserveTrailingSpaces = true;
        Assert.True (view.PreserveTrailingSpaces);
    }

    // Setting TextFormatter DOES NOT update Text
    [Fact]
    public void SettingTextFormatterDoesNotUpdateText () {
        var view = new View ();
        view.TextFormatter.Text = "Hello World";

        Assert.Empty (view.Text);
    }

    // Setting Text updates TextFormatter
    [Fact]
    public void SettingTextUpdatesTextFormatter () {
        var view = new View {
                                Text = "Hello World"
                            };

        Assert.Equal ("Hello World", view.Text);
        Assert.Equal ("Hello World", view.TextFormatter.Text);
    }

    // Test that TextFormatter is init only
    [Fact]
    public void TextFormatterIsInitOnly () {
        var view = new View ();

        // Use reflection to ensure the TextFormatter property is `init` only
        Assert.True (
                     typeof (View).GetMethod ("set_TextFormatter")
                                  .ReturnParameter.GetRequiredCustomModifiers ()
                                  .Contains (typeof (IsExternalInit)));
    }

    // Test that the Text property is set correctly.
    [Fact]
    public void TextProperty () {
        var view = new View {
                                Text = "Hello World"
                            };

        Assert.Equal ("Hello World", view.Text);
    }

    // Setting Text sets the HotKey
    [Fact]
    public void TextSetsHotKey () {
        var view = new View {
                                HotKeySpecifier = (Rune)'_',
                                Text = "_Hello World"
                            };

        Assert.Equal (Key.H, view.HotKey);
    }

    // Test view.UpdateTextFormatterText overridden in a subclass updates TextFormatter.Text
    [Fact]
    public void UpdateTextFormatterText_Overridden () {
        var view = new TestView {
                                    Text = "Hello World"
                                };

        Assert.Equal ("Hello World", view.Text);
        Assert.Equal (">Hello World<", view.TextFormatter.Text);
    }

    private class TestView : View {
        protected override void UpdateTextFormatterText () { TextFormatter.Text = $">{Text}<"; }
    }

    // Test behavior of AutoSize property. 
    // - Default is false
    // - Setting to true invalidates Height/Width
    // - Setting to false invalidates Height/Width
}
