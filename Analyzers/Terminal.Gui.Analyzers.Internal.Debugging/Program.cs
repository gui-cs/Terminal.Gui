﻿using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui.Analyzers.Internal.Debugging;

class Program
{
    static void Main (string [] args)
    {
        
    }
}

[GenerateEnumExtensionMethods]
public enum TestEnum
{
    Zero = 0,
    One,
    Two = 2,
    Three,
    Six = 6
}