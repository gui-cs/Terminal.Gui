using System.Text;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class TestOutputWriter : TextWriter
{
    private readonly ITestOutputHelper _output;

    public TestOutputWriter (ITestOutputHelper output) { _output = output; }

    public override void WriteLine (string? value) { _output.WriteLine (value ?? string.Empty); }

    public override Encoding Encoding => Encoding.UTF8;
}
