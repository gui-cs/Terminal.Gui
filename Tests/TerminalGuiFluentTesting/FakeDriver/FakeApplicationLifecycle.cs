#nullable enable
namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591
internal class FakeApplicationLifecycle (IApplication origApp, CancellationTokenSource hardStop) : IDisposable
{
    /// <inheritdoc/>
    public void Dispose ()
    {
        hardStop.Cancel ();

        Application.Top?.Dispose ();
        Application.Shutdown ();
        ApplicationImpl.ChangeInstance (origApp);
    }
}
