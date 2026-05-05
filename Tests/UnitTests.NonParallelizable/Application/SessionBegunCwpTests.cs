#nullable enable
namespace UnitTests.NonParallelizable.ApplicationTests;

/// <summary>
///     Regression tests for issue #5162. Ensures that <see cref="IApplication.SessionBegun"/>
///     is raised AFTER all session state mutations, per the Cancellable Workflow Pattern (CWP):
///     subscribers must observe a fully-consistent <see cref="SessionToken"/> with
///     <see cref="IRunnable.IsRunning"/> and <see cref="IRunnable.IsModal"/> already set to
///     <see langword="true"/>.
/// </summary>
public class SessionBegunCwpTests
{
    // Claude - Opus 4.7
    [Fact]
    public void SessionBegun_Raised_After_IsRunning_And_IsModal_Set_True ()
    {
        ApplicationImpl.ResetModelUsageTracking ();

        IApplication app = Application.Create ();
        Runnable? runnable = null;

        bool? observedIsRunning = null;
        bool? observedIsModal = null;

        try
        {
            runnable = new ();

            EventHandler<SessionTokenEventArgs> handler = (_, e) =>
                                                          {
                                                              IRunnable? r = e.State.Runnable;

                                                              if (r is null) { return; }

                                                              observedIsRunning = r.IsRunning;
                                                              observedIsModal = r.IsModal;
                                                          };

            app.SessionBegun += handler;

            SessionToken? token = app.Begin (runnable);

            app.SessionBegun -= handler;

            Assert.NotNull (token);
            Assert.True (observedIsRunning, "SessionBegun fired before SetIsRunning(true).");
            Assert.True (observedIsModal, "SessionBegun fired before SetIsModal(true).");

            app.End (token);
        }
        finally
        {
            runnable?.Dispose ();
            app.Dispose ();
            ApplicationImpl.ResetModelUsageTracking ();
        }
    }
}
