namespace Terminal.Gui;

/// <summary>
///     provides the sync context set while executing code in Terminal.Gui, to let
///     users use async/await on their code
/// </summary>
internal sealed class MainLoopSyncContext : SynchronizationContext
{
    public override SynchronizationContext CreateCopy () { return new MainLoopSyncContext (); }

    public override void Post (SendOrPostCallback d, object state)
    {
        Application.MainLoop?.AddIdle (
                                       () =>
                                       {
                                           d (state);

                                           return false;
                                       }
                                      );
    }

    //_mainLoop.Driver.Wakeup ();
    public override void Send (SendOrPostCallback d, object state)
    {
        if (Thread.CurrentThread.ManagedThreadId == Application._mainThreadId)
        {
            d (state);
        }
        else
        {
            var wasExecuted = false;

            Application.Invoke (
                                () =>
                                {
                                    d (state);
                                    wasExecuted = true;
                                }
                               );

            while (!wasExecuted)
            {
                Thread.Sleep (15);
            }
        }
    }
}
