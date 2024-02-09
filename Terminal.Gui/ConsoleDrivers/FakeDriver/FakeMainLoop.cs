namespace Terminal.Gui;

internal class FakeMainLoop : IMainLoopDriver {
    public FakeMainLoop (ConsoleDriver consoleDriver = null) {
        // No implementation needed for FakeMainLoop
    }

    public Action<ConsoleKeyInfo> MockKeyPressed;

    public void Setup (MainLoop mainLoop) {
        // No implementation needed for FakeMainLoop
    }

    public void Wakeup () {
        // No implementation needed for FakeMainLoop
    }

    public bool EventsPending () =>

        // Always return true for FakeMainLoop
        true;

    public void Iteration () {
        if (FakeConsole.MockKeyPresses.Count > 0) {
            MockKeyPressed?.Invoke (FakeConsole.MockKeyPresses.Pop ());
        }
    }

    public void TearDown () { }
}
