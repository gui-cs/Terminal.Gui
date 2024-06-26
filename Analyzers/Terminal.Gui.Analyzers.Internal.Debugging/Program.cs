using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui.Analyzers.Internal.Debugging;

static class Program
{
    static void Main (string [] args)
    {
        
    }
}

[GenerateEnumExtensionMethods]
[SuppressMessage ("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "It's not that deep")]
public enum TestEnum
{
    Zero = 0,
    One,
    Two = 2,
    Three,
    Six = 6
}