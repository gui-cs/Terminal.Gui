namespace Terminal.Gui;

public static partial class Application
{
    /// <summary>
    ///     provides the sync context set while executing code in Terminal.Gui, to let
    ///     users use async/await on their code
    /// </summary>
    private sealed class MainLoopSyncContext : SynchronizationContext
    {
        public override SynchronizationContext CreateCopy () { return new MainLoopSyncContext (); }

        public override void Post (SendOrPostCallback d, object state)
        {
            MainLoop?.AddIdle (
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
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                d (state);
            }
            else
            {
                var wasExecuted = false;

                Invoke (
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
}
