﻿//
// MainLoop.cs: IMainLoopDriver and MainLoop for Terminal.Gui
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

namespace Terminal.Gui;

/// <summary>Provides data for timers running manipulation.</summary>
public sealed class Timeout
{
    /// <summary>The function that will be invoked.</summary>
    public Func<bool> Callback;

    /// <summary>Time to wait before invoke the callback.</summary>
    public TimeSpan Span;
}
