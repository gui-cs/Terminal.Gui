using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.DriverTests;

public class UrlDetectionTests
{
    private readonly ITestOutputHelper _output;

    // This regex matches the one in OutputBase.cs
    private static readonly Regex UrlRegex = new Regex(
        @"\b(?:https?|ftps?)://[^\s<>""{}|\\^`\[\]\x1B]+[^\s<>""{}|\\^`\[\]\x1B.!?,;:)]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public UrlDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("https://github.com", "https://github.com")]
    [InlineData("http://example.com", "http://example.com")]
    [InlineData("https://github.com/gui-cs/Terminal.Gui", "https://github.com/gui-cs/Terminal.Gui")]
    [InlineData("https://commons.wikimedia.org/wiki/File:Spinning_globe.gif", "https://commons.wikimedia.org/wiki/File:Spinning_globe.gif")]
    [InlineData("https://example.com/path/to/file.html", "https://example.com/path/to/file.html")]
    [InlineData("https://example.com:8080/path", "https://example.com:8080/path")]
    [InlineData("https://example.com/path?query=value&other=123", "https://example.com/path?query=value&other=123")]
    [InlineData("https://example.com/path#anchor", "https://example.com/path#anchor")]
    [InlineData("https://user:pass@example.com/path", "https://user:pass@example.com/path")]
    [InlineData("ftp://ftp.example.com/file.zip", "ftp://ftp.example.com/file.zip")]
    [InlineData("https://example.com/path_(with)_parens", "https://example.com/path_(with)_parens")]
    [InlineData("https://example.com/~user/path", "https://example.com/~user/path")]
    [InlineData("https://192.168.1.1/path", "https://192.168.1.1/path")]
    [InlineData("https://example.com/path-with-dashes", "https://example.com/path-with-dashes")]
    [InlineData("https://example.com/path_with_underscores", "https://example.com/path_with_underscores")]
    [InlineData("https://example.com/file.tar.gz", "https://example.com/file.tar.gz")]
    public void UrlRegex_Should_Match_Valid_URLs(string input, string expectedMatch)
    {
        var match = UrlRegex.Match(input);
        
        Assert.True(match.Success, $"Failed to match URL: {input}");
        Assert.Equal(expectedMatch, match.Value);
        
        _output.WriteLine($"✓ Matched: {input}");
    }

    [Theory]
    [InlineData("Visit https://github.com for more info", "https://github.com")]
    [InlineData("Check out https://commons.wikimedia.org/wiki/File:Spinning_globe.gif!", "https://commons.wikimedia.org/wiki/File:Spinning_globe.gif")]
    [InlineData("URLs: https://example.com and http://other.com", "https://example.com")]
    public void UrlRegex_Should_Extract_URLs_From_Text(string input, string expectedFirstMatch)
    {
        var match = UrlRegex.Match(input);
        
        Assert.True(match.Success, $"Failed to find URL in: {input}");
        Assert.Equal(expectedFirstMatch, match.Value);
        
        _output.WriteLine($"✓ Extracted from '{input}': {match.Value}");
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("http://")]
    [InlineData("://missing-protocol.com")]
    public void UrlRegex_Should_Not_Match_Invalid_URLs(string input)
    {
        var match = UrlRegex.Match(input);
        
        Assert.False(match.Success, $"Should not match invalid URL: {input}");
        
        _output.WriteLine($"✓ Correctly rejected: {input}");
    }

    [Fact]
    public void UrlRegex_Should_Stop_At_ANSI_Escape_Sequence()
    {
        // Simulate a URL with ANSI escape sequence in the middle
        string input = "https://example.com\x1B[38;2;173m/more";
        var match = UrlRegex.Match(input);
        
        Assert.True(match.Success);
        Assert.Equal("https://example.com", match.Value);
        // Ensure the matched URL does not contain ESC character
        Assert.DoesNotContain('\x1B', match.Value);
        
        _output.WriteLine($"✓ Stopped at ESC: matched '{match.Value}'");
    }
}
