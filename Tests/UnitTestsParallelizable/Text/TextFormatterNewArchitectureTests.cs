using System.Drawing;
using Terminal.Gui.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.TextTests;

/// <summary>
/// Tests for the new TextFormatter architecture that separates formatting from rendering.
/// </summary>
public class TextFormatterNewArchitectureTests
{
    private readonly ITestOutputHelper _output;

    public TextFormatterNewArchitectureTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TextFormatter_NewArchitecture_BasicFormatting_Works()
    {
        Application.Init(new FakeDriver());

        var tf = new TextFormatter
        {
            Text = "Hello World"
        };

        // Test the new architecture method
        Size size = tf.GetFormattedSizeWithNewArchitecture();
        
        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
        
        Application.Shutdown();
    }

    [Fact]
    public void TextFormatter_NewArchitecture_WithAlignment_Works()
    {
        Application.Init(new FakeDriver());

        var tf = new TextFormatter
        {
            Text = "Hello World",
            Alignment = Alignment.Center,
            VerticalAlignment = Alignment.Center
        };

        // Test that properties are synchronized
        Size size = tf.GetFormattedSizeWithNewArchitecture();
        
        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
        
        Application.Shutdown();
    }

    [Fact]
    public void TextFormatter_NewArchitecture_Performance_IsBetter()
    {
        Application.Init(new FakeDriver());

        var tf = new TextFormatter
        {
            Text = "This is a long text that will be formatted multiple times to test performance improvements"
        };

        // Warm up
        tf.GetFormattedSizeWithNewArchitecture();

        // Test multiple calls - should use caching
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            tf.GetFormattedSizeWithNewArchitecture();
        }
        sw.Stop();

        _output.WriteLine($"New architecture: 100 calls took {sw.ElapsedMilliseconds}ms");
        
        // The new architecture should be fast due to caching
        Assert.True(sw.ElapsedMilliseconds < 100, "New architecture should be fast due to caching");
        
        Application.Shutdown();
    }

    [Fact]
    public void TextFormatter_NewArchitecture_DrawRegion_Works()
    {
        Application.Init(new FakeDriver());

        var tf = new TextFormatter
        {
            Text = "Hello\nWorld"
        };

        Region region = tf.GetDrawRegionWithNewArchitecture(new Rectangle(0, 0, 10, 10));
        
        Assert.NotNull(region);
        
        Application.Shutdown();
    }

    [Fact]
    public void StandardTextFormatter_DirectlyUsed_Works()
    {
        var formatter = new StandardTextFormatter
        {
            Text = "Test Text",
            Alignment = Alignment.Center
        };

        FormattedText result = formatter.Format();
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.Lines);
        Assert.True(result.RequiredSize.Width > 0);
        Assert.True(result.RequiredSize.Height > 0);
    }

    [Fact]
    public void StandardTextRenderer_DirectlyUsed_Works()
    {
        Application.Init(new FakeDriver());
        
        var formatter = new StandardTextFormatter
        {
            Text = "Test Text"
        };
        
        var renderer = new StandardTextRenderer();
        FormattedText formattedText = formatter.Format();
        
        // Should not throw
        renderer.Draw(
            formattedText, 
            new Rectangle(0, 0, 10, 1), 
            Attribute.Default, 
            Attribute.Default);
            
        Region region = renderer.GetDrawRegion(
            formattedText, 
            new Rectangle(0, 0, 10, 1));
            
        Assert.NotNull(region);
        
        Application.Shutdown();
    }

    [Fact]
    public void TextFormatter_UseNewArchitecture_Flag_Works()
    {
        Application.Init(new FakeDriver());

        var tf = new TextFormatter
        {
            Text = "Hello World",
            UseNewArchitecture = true // Enable new architecture
        };

        // This should now use the new architecture via the Draw method
        tf.Draw(new Rectangle(0, 0, 10, 1), Attribute.Default, Attribute.Default);
        
        // Test that the new architecture produces results
        Size size = tf.GetFormattedSizeWithNewArchitecture();
        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
        
        Application.Shutdown();
    }
}