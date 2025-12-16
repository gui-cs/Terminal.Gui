using Microsoft.Extensions.Logging;

namespace TerminalGuiFluentTesting;

internal class TextWriterLoggerProvider (TextWriter writer) : ILoggerProvider
{
    public ILogger CreateLogger (string category) { return new TextWriterLogger (writer); }

    public void Dispose () { writer.Dispose (); }
}
