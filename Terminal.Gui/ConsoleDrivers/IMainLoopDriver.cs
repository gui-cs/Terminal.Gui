#nullable enable

namespace Terminal.Gui.ConsoleDrivers;

/// <summary>Interface to create a platform specific <see cref="MainLoop"/> driver.</summary>
internal interface IMainLoopDriver
{
    /// <summary>Must report whether there are any events pending, or even block waiting for events.</summary>
    /// <returns><c>true</c>, if there were pending events, <c>false</c> otherwise.</returns>
    bool EventsPending ();

    /// <summary>The iteration function.</summary>
    void Iteration ();

    /// <summary>Initializes the <see cref="MainLoop"/>, gets the calling main loop for the initialization.</summary>
    /// <remarks>Call <see cref="TearDown"/> to release resources.</remarks>
    /// <param name="mainLoop">Main loop.</param>
    void Setup (MainLoop mainLoop);

    /// <summary>Tears down the <see cref="MainLoop"/> driver. Releases resources created in <see cref="Setup"/>.</summary>
    void TearDown ();

    /// <summary>Wakes up the <see cref="MainLoop"/> that might be waiting on input, must be thread safe.</summary>
    void Wakeup ();
}
