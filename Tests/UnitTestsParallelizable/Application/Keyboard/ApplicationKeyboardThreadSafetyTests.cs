// ReSharper disable AccessToDisposedClosure

#nullable enable
namespace ApplicationTests.Keyboard;

/// <summary>
///     Tests to verify that ApplicationKeyboard is thread-safe for concurrent access scenarios.
/// </summary>
[Collection("Application Tests")]
public class ApplicationKeyboardThreadSafetyTests
{
    [Fact]
    public void AddCommand_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 50;

        // Act
        List<Task> tasks = [];

        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (
                       Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             // AddKeyBindings internally calls AddCommand multiple times
                                             keyboard.AddKeyBindings ();
                                         }
                                         catch (InvalidOperationException)
                                         {
                                             // Expected - AddKeyBindings tries to add keys that already exist
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 }));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray ());
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
    }

    [Fact]
    public void Dispose_WhileOperationsInProgress_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        var continueRunning = true;

        // Act
        Task operationsTask = Task.Run (() =>
                                        {
                                            while (continueRunning)
                                            {
                                                try
                                                {
                                                    keyboard.InvokeCommandsBoundToKey (Key.Q.WithCtrl);
                                                    IEnumerable<KeyValuePair<Key, KeyBinding>> bindings = keyboard.KeyBindings.GetBindings ();
                                                    int count = bindings.Count ();
                                                }
                                                catch (ObjectDisposedException)
                                                {
                                                    // Expected - keyboard was disposed
                                                    break;
                                                }
                                                catch (Exception ex)
                                                {
                                                    exceptions.Add (ex);

                                                    break;
                                                }
                                            }
                                        });

        // Give operations a chance to start
        Thread.Sleep (10);

        // Dispose while operations are running
        keyboard.Dispose ();
        continueRunning = false;

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        operationsTask.Wait (TimeSpan.FromSeconds (2));
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        app.Dispose ();
    }

    [Fact]
    public void InvokeCommand_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 50;

        // Act
        List<Task> tasks = new ();

        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (
                       Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             var binding = new KeyBinding ([Command.Quit]);
                                             keyboard.InvokeCommand (Command.Quit, Key.Q.WithCtrl, binding);
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 }));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray ());
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void InvokeCommandsBoundToKey_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 50;

        // Act
        List<Task> tasks = [];

        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (
                       Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             keyboard.InvokeCommandsBoundToKey (Key.Q.WithCtrl);
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 }));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray ());
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void KeyBindings_ConcurrentAdd_NoExceptions ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Don't call AddKeyBindings here to avoid conflicts
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 50;

        // Act
        List<Task> tasks = new ();

        for (var i = 0; i < NUM_THREADS; i++)
        {
            int threadId = i;

            tasks.Add (
                       Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             // Use unique keys per thread to avoid conflicts
                                             Key key = Key.F1 + threadId * OPERATIONS_PER_THREAD + j;
                                             keyboard.KeyBindings.Add (key, Command.Refresh);
                                         }
                                         catch (InvalidOperationException)
                                         {
                                             // Expected - duplicate key
                                         }
                                         catch (ArgumentException)
                                         {
                                             // Expected - invalid key
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 }));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray ());
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
    }

    [Fact]
    public void KeyDown_Events_ConcurrentSubscription_NoExceptions ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 20;
        var keyDownCount = 0;

        // Act
        List<Task> tasks = new ();

        // Threads subscribing to events
        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (
                       Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             EventHandler<Key> handler = (s, e) => { Interlocked.Increment (ref keyDownCount); };
                                             keyboard.KeyDown += handler;
                                             keyboard.KeyDown -= handler;

                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 }));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray ());
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
    }

    [Fact]
    public void KeyProperty_Setters_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Initialize once before concurrent access
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 20;

        // Act
        List<Task> tasks = new ();

        for (var i = 0; i < NUM_THREADS; i++)
        {
            int threadId = i;

            tasks.Add (
                       Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             // Cycle through different key combinations
                                             switch (j % 6)
                                             {
                                                 case 0:
                                                     keyboard.QuitKey = Key.Q.WithCtrl;

                                                     break;
                                                 case 1:
                                                     keyboard.ArrangeKey = Key.F6.WithCtrl;

                                                     break;
                                                 case 2:
                                                     keyboard.NextTabKey = Key.Tab;

                                                     break;
                                                 case 3:
                                                     keyboard.PrevTabKey = Key.Tab.WithShift;

                                                     break;
                                                 case 4:
                                                     keyboard.NextTabGroupKey = Key.F6;

                                                     break;
                                                 case 5:
                                                     keyboard.PrevTabGroupKey = Key.F6.WithShift;

                                                     break;
                                             }
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 }));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray ());
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
    }

    [Fact]
    public void MixedOperations_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int OPERATIONS_PER_THREAD = 30;

        // Act
        List<Task> tasks = new ();

        // Thread 1: Add bindings with unique keys
        tasks.Add (
                   Task.Run (() =>
                             {
                                 for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                 {
                                     try
                                     {
                                         // Use high key codes to avoid conflicts
                                         var key = new Key ((KeyCode)((int)KeyCode.F20 + j));
                                         keyboard.KeyBindings.Add (key, Command.Refresh);
                                     }
                                     catch (InvalidOperationException)
                                     {
                                         // Expected - duplicate
                                     }
                                     catch (ArgumentException)
                                     {
                                         // Expected - invalid key
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 }
                             }));

        // Thread 2: Invoke commands
        tasks.Add (
                   Task.Run (() =>
                             {
                                 for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                 {
                                     try
                                     {
                                         keyboard.InvokeCommandsBoundToKey (Key.Q.WithCtrl);
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 }
                             }));

        // Thread 3: Read bindings
        tasks.Add (
                   Task.Run (() =>
                             {
                                 for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                 {
                                     try
                                     {
                                         IEnumerable<KeyValuePair<Key, KeyBinding>> bindings = keyboard.KeyBindings.GetBindings ();
                                         int count = bindings.Count ();
                                         Assert.True (count >= 0);
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 }
                             }));

        // Thread 4: Change key properties
        tasks.Add (
                   Task.Run (() =>
                             {
                                 for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                 {
                                     try
                                     {
                                         keyboard.QuitKey = j % 2 == 0 ? Key.Q.WithCtrl : Key.Esc;
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 }
                             }));

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray ());
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void RaiseKeyDownEvent_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 5;
        const int OPERATIONS_PER_THREAD = 20;

        // Act
        List<Task> tasks = new ();

        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (
                       Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             keyboard.RaiseKeyDownEvent (Key.A);
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 }));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray ());
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
        app.Dispose ();
    }
}
