#nullable enable
using System.Threading;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class OverlappedTests
{
    private readonly ITestOutputHelper _output;

    public OverlappedTests (ITestOutputHelper output)
    {
        _output = output;
#if DEBUG_IDISPOSABLE
        Responder.Instances.Clear ();
        RunState.Instances.Clear ();
#endif
    }

    [Fact]
    [AutoInitShutdown]
    public void AllChildClosed_Event_Test ()
    {
        var overlapped = new Overlapped ();
        var c1 = new Toplevel ();
        var c2 = new Window ();
        var c3 = new Window ();

        // OverlappedChild = c1, c2, c3
        var iterations = 3;

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                Application.Run (c1);
                            };

        c1.Ready += (s, e) =>
                    {
                        Assert.Single (ApplicationOverlapped.OverlappedChildren!);
                        Application.Run (c2);
                    };

        c2.Ready += (s, e) =>
                    {
                        Assert.Equal (2, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (c3);
                    };

        c3.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        c3.RequestStop ();
                        c2.RequestStop ();
                        c1.RequestStop ();
                    };

        // Now this will close the OverlappedContainer when all OverlappedChildren was closed
        overlapped.AllChildClosed += (s, e) => { overlapped.RequestStop (); };

        Application.Iteration += (s, a) =>
                                 {
                                     if (iterations == 3)
                                     {
                                         // The Current still is c3 because Current.Running is false.
                                         Assert.True (Application.Current == c3);
                                         Assert.False (Application.Current.Running);

                                         // But the Children order were reorder by Running = false
                                         Assert.True (ApplicationOverlapped.OverlappedChildren! [0] == c3);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren [1] == c2);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren [^1] == c1);
                                     }
                                     else if (iterations == 2)
                                     {
                                         // The Current is c2 and Current.Running is false.
                                         Assert.True (Application.Current == c2);
                                         Assert.False (Application.Current.Running);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren ![0] == c2);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren [^1] == c1);
                                     }
                                     else if (iterations == 1)
                                     {
                                         // The Current is c1 and Current.Running is false.
                                         Assert.True (Application.Current == c1);
                                         Assert.False (Application.Current.Running);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren! [^1] == c1);
                                     }
                                     else
                                     {
                                         // The Current is overlapped.
                                         Assert.True (Application.Current == overlapped);
                                         Assert.False (Application.Current.Running);
                                         Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                     }

                                     iterations--;
                                 };

        Application.Run (overlapped);

        Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
        Assert.NotNull (ApplicationOverlapped.OverlappedTop);
        Assert.NotNull (Application.Top);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Application_RequestStop_With_Params_On_A_Not_OverlappedContainer_Always_Use_Application_Current ()
    {
        var top1 = new Toplevel ();
        var top2 = new Toplevel ();
        var top3 = new Window ();
        var top4 = new Window ();
        var d = new Dialog ();

        // top1, top2, top3, d1 = 4
        var iterations = 4;

        top1.Ready += (s, e) =>
                      {
                          Assert.Null (ApplicationOverlapped.OverlappedChildren);
                          Application.Run (top2);
                      };

        top2.Ready += (s, e) =>
                      {
                          Assert.Null (ApplicationOverlapped.OverlappedChildren);
                          Application.Run (top3);
                      };

        top3.Ready += (s, e) =>
                      {
                          Assert.Null (ApplicationOverlapped.OverlappedChildren);
                          Application.Run (top4);
                      };

        top4.Ready += (s, e) =>
                      {
                          Assert.Null (ApplicationOverlapped.OverlappedChildren);
                          Application.Run (d);
                      };

        d.Ready += (s, e) =>
                   {
                       Assert.Null (ApplicationOverlapped.OverlappedChildren);

                       // This will close the d because on a not OverlappedContainer the Application.Current it always used.
                       Application.RequestStop (top1);
                       Assert.True (Application.Current == d);
                   };

        d.Closed += (s, e) => Application.RequestStop (top1);

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.Null (ApplicationOverlapped.OverlappedChildren);

                                     if (iterations == 4)
                                     {
                                         Assert.True (Application.Current == d);
                                     }
                                     else if (iterations == 3)
                                     {
                                         Assert.True (Application.Current == top4);
                                     }
                                     else if (iterations == 2)
                                     {
                                         Assert.True (Application.Current == top3);
                                     }
                                     else if (iterations == 1)
                                     {
                                         Assert.True (Application.Current == top2);
                                     }
                                     else
                                     {
                                         Assert.True (Application.Current == top1);
                                     }

                                     Application.RequestStop (top1);
                                     iterations--;
                                 };

        Application.Run (top1);

        Assert.Null (ApplicationOverlapped.OverlappedChildren);
        top1.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void Dispose_Toplevel_IsOverlappedContainer_False_With_Begin_End ()
    {
        Application.Init (new FakeDriver ());

        var top = new Toplevel ();
        RunState rs = Application.Begin (top);

        Application.End (rs);
        top.Dispose ();
        Application.Shutdown ();

#if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
#endif
    }

    [Fact]
    [TestRespondersDisposed]
    public void Dispose_Toplevel_IsOverlappedContainer_True_With_Begin ()
    {
        Application.Init (new FakeDriver ());

        var overlapped = new Toplevel { IsOverlappedContainer = true };
        RunState rs = Application.Begin (overlapped);
        Application.End (rs);
        overlapped.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    [AutoInitShutdown]
    public void IsOverlappedChild_Testing ()
    {
        var overlapped = new Overlapped ();
        var c1 = new Toplevel ();
        var c2 = new Window ();
        var c3 = new Window ();
        var d = new Dialog ();

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.False (ApplicationOverlapped.IsOverlapped(overlapped));
                                     Assert.True (ApplicationOverlapped.IsOverlapped(c1));
                                     Assert.True (ApplicationOverlapped.IsOverlapped(c2));
                                     Assert.True (ApplicationOverlapped.IsOverlapped(c3));
                                     Assert.False (ApplicationOverlapped.IsOverlapped(d));

                                     overlapped.RequestStop ();
                                 };

        Application.Run (overlapped);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void
        Modal_Toplevel_Can_Open_Another_Modal_Toplevel_But_RequestStop_To_The_Caller_Also_Sets_Current_Running_To_False_Too ()
    {
        var overlapped = new Overlapped ();
        var c1 = new Toplevel ();
        var c2 = new Window ();
        var c3 = new Window ();
        var d1 = new Dialog ();
        var d2 = new Dialog ();

        // OverlappedChild = c1, c2, c3 = 3
        // d1, d2 = 2
        var iterations = 5;

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                Application.Run (c1);
                            };

        c1.Ready += (s, e) =>
                    {
                        Assert.Single (ApplicationOverlapped.OverlappedChildren!);
                        Application.Run (c2);
                    };

        c2.Ready += (s, e) =>
                    {
                        Assert.Equal (2, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (c3);
                    };

        c3.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (d1);
                    };

        d1.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (d2);
                    };

        d2.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        Assert.True (Application.Current == d2);
                        Assert.True (Application.Current.Running);

                        // Trying to close the Dialog1
                        d1.RequestStop ();
                    };

        // Now this will close the OverlappedContainer propagating through the OverlappedChildren.
        d1.Closed += (s, e) =>
                     {
                         Assert.True (Application.Current == d1);
                         Assert.False (Application.Current.Running);
                         overlapped.RequestStop ();
                     };

        Application.Iteration += (s, a) =>
                                 {
                                     if (iterations == 5)
                                     {
                                         // The Dialog2 still is the current top and we can't request stop to OverlappedContainer
                                         // because Dialog2 and Dialog1 must be closed first.
                                         // Dialog2 will be closed in this iteration.
                                         Assert.True (Application.Current == d2);
                                         Assert.False (Application.Current.Running);
                                         Assert.False (d1.Running);
                                     }
                                     else if (iterations == 4)
                                     {
                                         // Dialog1 will be closed in this iteration.
                                         Assert.True (Application.Current == d1);
                                         Assert.False (Application.Current.Running);
                                     }
                                     else
                                     {
                                         Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);

                                         for (var i = 0; i < iterations; i++)
                                         {
                                             Assert.Equal ((iterations - i + 1).ToString (), ApplicationOverlapped.OverlappedChildren [i].Id);
                                         }
                                     }

                                     iterations--;
                                 };

        Application.Run (overlapped);

        Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
        Assert.NotNull (ApplicationOverlapped.OverlappedTop);
        Assert.NotNull (Application.Top);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void
        Modal_Toplevel_Can_Open_Another_Not_Modal_Toplevel_But_RequestStop_To_The_Caller_Also_Sets_Current_Running_To_False_Too ()
    {
        var overlapped = new Overlapped ();
        var c1 = new Toplevel ();
        var c2 = new Window ();
        var c3 = new Window ();
        var d1 = new Dialog ();
        var c4 = new Toplevel ();

        // OverlappedChild = c1, c2, c3, c4 = 4
        // d1 = 1
        var iterations = 5;

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                Application.Run (c1);
                            };

        c1.Ready += (s, e) =>
                    {
                        Assert.Single (ApplicationOverlapped.OverlappedChildren!);
                        Application.Run (c2);
                    };

        c2.Ready += (s, e) =>
                    {
                        Assert.Equal (2, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (c3);
                    };

        c3.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (d1);
                    };

        d1.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (c4);
                    };

        c4.Ready += (s, e) =>
                    {
                        Assert.Equal (4, ApplicationOverlapped.OverlappedChildren!.Count);

                        // Trying to close the Dialog1
                        d1.RequestStop ();
                    };

        // Now this will close the OverlappedContainer propagating through the OverlappedChildren.
        d1.Closed += (s, e) => { overlapped.RequestStop (); };

        Application.Iteration += (s, a) =>
                                 {
                                     if (iterations == 5)
                                     {
                                         // The Dialog2 still is the current top and we can't request stop to OverlappedContainer
                                         // because Dialog2 and Dialog1 must be closed first.
                                         // Using request stop here will call the Dialog again without need
                                         Assert.True (Application.Current == d1);
                                         Assert.False (Application.Current.Running);
                                         Assert.True (c4.Running);
                                     }
                                     else
                                     {
                                         Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);

                                         for (var i = 0; i < iterations; i++)
                                         {
                                             Assert.Equal (
                                                           (iterations - i + (iterations == 4 && i == 0 ? 2 : 1)).ToString (),
                                                           ApplicationOverlapped.OverlappedChildren [i].Id
                                                          );
                                         }
                                     }

                                     iterations--;
                                 };

        Application.Run (overlapped);

        Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
        Assert.NotNull (ApplicationOverlapped.OverlappedTop);
        Assert.NotNull (Application.Top);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MoveCurrent_Returns_False_If_The_Current_And_Top_Parameter_Are_Both_With_Running_Set_To_False ()
    {
        Overlapped? overlapped = new Overlapped ();
        var c1 = new Toplevel ();
        var c2 = new Window ();
        var c3 = new Window ();

        // OverlappedChild = c1, c2, c3
        var iterations = 3;

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                Application.Run (c1);
                            };

        c1.Ready += (s, e) =>
                    {
                        Assert.Single (ApplicationOverlapped.OverlappedChildren!);
                        Application.Run (c2);
                    };

        c2.Ready += (s, e) =>
                    {
                        Assert.Equal (2, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (c3);
                    };

        c3.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        c3.RequestStop ();
                        c1.RequestStop ();
                    };

        // Now this will close the OverlappedContainer propagating through the OverlappedChildren.
        c1.Closed += (s, e) => { overlapped.RequestStop (); };

        Application.Iteration += (s, a) =>
                                 {
                                     if (iterations == 3)
                                     {
                                         // The Current still is c3 because Current.Running is false.
                                         Assert.True (Application.Current == c3);
                                         Assert.False (Application.Current.Running);

                                         // But the Children order were reorder by Running = false
                                         Assert.True (ApplicationOverlapped.OverlappedChildren! [0] == c3);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren [1] == c1);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren [^1] == c2);
                                     }
                                     else if (iterations == 2)
                                     {
                                         // The Current is c1 and Current.Running is false.
                                         Assert.True (Application.Current == c1);
                                         Assert.False (Application.Current.Running);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren! [0] == c1);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren [^1] == c2);
                                     }
                                     else if (iterations == 1)
                                     {
                                         // The Current is c2 and Current.Running is false.
                                         Assert.True (Application.Current == c2);
                                         Assert.False (Application.Current.Running);
                                         Assert.True (ApplicationOverlapped.OverlappedChildren! [^1] == c2);
                                     }
                                     else
                                     {
                                         // The Current is overlapped.
                                         Assert.True (Application.Current == overlapped);
                                         Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                     }

                                     iterations--;
                                 };

        Application.Run (overlapped);

        Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
        Assert.NotNull (ApplicationOverlapped.OverlappedTop);
        Assert.NotNull (Application.Top);
        overlapped.Dispose ();
    }

    [Fact]
    public void MoveToOverlappedChild_Throw_NullReferenceException_Passing_Null_Parameter ()
    {
        Assert.Throws<ArgumentNullException> (delegate { ApplicationOverlapped.MoveToOverlappedChild (null); });
    }

    [Fact]
    [AutoInitShutdown]
    public void OverlappedContainer_Open_And_Close_Modal_And_Open_Not_Modal_Toplevels_Randomly ()
    {
        var overlapped = new Overlapped ();
        var logger = new Toplevel ();

        var iterations = 1; // The logger
        var running = true;
        var stageCompleted = true;
        var allStageClosed = false;
        var overlappedRequestStop = false;

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                Application.Run (logger);
                            };

        logger.Ready += (s, e) => Assert.Single (ApplicationOverlapped.OverlappedChildren!);

        Application.Iteration += (s, a) =>
                                 {
                                     if (stageCompleted && running)
                                     {
                                         stageCompleted = false;
                                         var stage = new Window { Modal = true };

                                         stage.Ready += (s, e) =>
                                                        {
                                                            Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);
                                                            stage.RequestStop ();
                                                        };

                                         stage.Closed += (_, _) =>
                                                         {
                                                             if (iterations == 11)
                                                             {
                                                                 allStageClosed = true;
                                                             }

                                                             Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);

                                                             if (running)
                                                             {
                                                                 stageCompleted = true;

                                                                 var rpt = new Window ();

                                                                 rpt.Ready += (s, e) =>
                                                                              {
                                                                                  iterations++;
                                                                                  Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren.Count);
                                                                              };

                                                                 Application.Run (rpt);
                                                             }
                                                         };

                                         Application.Run (stage);
                                     }
                                     else if (iterations == 11 && running)
                                     {
                                         running = false;
                                         Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);
                                     }
                                     else if (!overlappedRequestStop && running && !allStageClosed)
                                     {
                                         Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);
                                     }
                                     else if (!overlappedRequestStop && !running && allStageClosed)
                                     {
                                         Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);
                                         overlappedRequestStop = true;
                                         overlapped?.RequestStop ();
                                     }
                                     else
                                     {
                                         Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                     }
                                 };

        Application.Run (overlapped);

        Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
        Assert.NotNull (ApplicationOverlapped.OverlappedTop);
        Assert.NotNull (Application.Top);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void OverlappedContainer_Throws_If_More_Than_One ()
    {
        var overlapped = new Overlapped ();
        var overlapped2 = new Overlapped ();

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Throws<InvalidOperationException> (() => Application.Run (overlapped2));
                                overlapped.RequestStop ();
                            };

        Application.Run (overlapped);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void OverlappedContainer_With_Application_RequestStop_OverlappedTop_With_Params ()
    {
        var overlapped = new Overlapped ();
        var c1 = new Toplevel ();
        var c2 = new Window ();
        var c3 = new Window ();
        var d = new Dialog ();

        // OverlappedChild = c1, c2, c3
        // d1 = 1
        var iterations = 4;

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                Application.Run (c1);
                            };

        c1.Ready += (s, e) =>
                    {
                        Assert.Single (ApplicationOverlapped.OverlappedChildren!);
                        Application.Run (c2);
                    };

        c2.Ready += (s, e) =>
                    {
                        Assert.Equal (2, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (c3);
                    };

        c3.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (d);
                    };

        // Also easy because the Overlapped Container handles all at once
        d.Ready += (s, e) =>
                   {
                       Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);

                       // This will not close the OverlappedContainer because d is a modal Toplevel
                       Application.RequestStop (overlapped);
                   };

        // Now this will close the OverlappedContainer propagating through the OverlappedChildren.
        d.Closed += (s, e) => Application.RequestStop (overlapped);

        Application.Iteration += (s, a) =>
                                 {
                                     if (iterations == 4)
                                     {
                                         // The Dialog was not closed before and will be closed now.
                                         Assert.True (Application.Current == d);
                                         Assert.False (d.Running);
                                     }
                                     else
                                     {
                                         Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);

                                         for (var i = 0; i < iterations; i++)
                                         {
                                             Assert.Equal ((iterations - i + 1).ToString (), ApplicationOverlapped.OverlappedChildren [i].Id);
                                         }
                                     }

                                     iterations--;
                                 };

        Application.Run (overlapped);

        Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
        Assert.NotNull (ApplicationOverlapped.OverlappedTop);
        Assert.NotNull (Application.Top);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void OverlappedContainer_With_Application_RequestStop_OverlappedTop_Without_Params ()
    {
        var overlapped = new Overlapped ();
        var c1 = new Toplevel ();
        var c2 = new Window ();
        var c3 = new Window ();
        var d = new Dialog ();

        // OverlappedChild = c1, c2, c3 = 3
        // d1 = 1
        var iterations = 4;

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                Application.Run (c1);
                            };

        c1.Ready += (s, e) =>
                    {
                        Assert.Single (ApplicationOverlapped.OverlappedChildren!);
                        Application.Run (c2);
                    };

        c2.Ready += (s, e) =>
                    {
                        Assert.Equal (2, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (c3);
                    };

        c3.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (d);
                    };

        //More harder because it's sequential.
        d.Ready += (s, e) =>
                   {
                       Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);

                       // Close the Dialog
                       Application.RequestStop ();
                   };

        // Now this will close the OverlappedContainer propagating through the OverlappedChildren.
        d.Closed += (s, e) => Application.RequestStop (overlapped);

        Application.Iteration += (s, a) =>
                                 {
                                     if (iterations == 4)
                                     {
                                         // The Dialog still is the current top and we can't request stop to OverlappedContainer
                                         // because we are not using parameter calls.
                                         Assert.True (Application.Current == d);
                                         Assert.False (d.Running);
                                     }
                                     else
                                     {
                                         Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);

                                         for (var i = 0; i < iterations; i++)
                                         {
                                             Assert.Equal ((iterations - i + 1).ToString (), ApplicationOverlapped.OverlappedChildren [i].Id);
                                         }
                                     }

                                     iterations--;
                                 };

        Application.Run (overlapped);

        Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
        Assert.NotNull (ApplicationOverlapped.OverlappedTop);
        Assert.NotNull (Application.Top);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void OverlappedContainer_With_Toplevel_RequestStop_Balanced ()
    {
        var overlapped = new Overlapped ();
        var c1 = new Toplevel ();
        var c2 = new Window ();
        var c3 = new Window ();
        var d = new Dialog ();

        // OverlappedChild = c1, c2, c3
        // d1 = 1
        var iterations = 4;

        overlapped.Ready += (s, e) =>
                            {
                                Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
                                Application.Run (c1);
                            };

        c1.Ready += (s, e) =>
                    {
                        Assert.Single (ApplicationOverlapped.OverlappedChildren!);
                        Application.Run (c2);
                    };

        c2.Ready += (s, e) =>
                    {
                        Assert.Equal (2, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (c3);
                    };

        c3.Ready += (s, e) =>
                    {
                        Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);
                        Application.Run (d);
                    };

        // More easy because the Overlapped Container handles all at once
        d.Ready += (s, e) =>
                   {
                       Assert.Equal (3, ApplicationOverlapped.OverlappedChildren!.Count);

                       // This will not close the OverlappedContainer because d is a modal Toplevel and will be closed.
                       overlapped.RequestStop ();
                   };

        // Now this will close the OverlappedContainer propagating through the OverlappedChildren.
        d.Closed += (s, e) => { overlapped.RequestStop (); };

        Application.Iteration += (s, a) =>
                                 {
                                     if (iterations == 4)
                                     {
                                         // The Dialog was not closed before and will be closed now.
                                         Assert.True (Application.Current == d);
                                         Assert.False (d.Running);
                                     }
                                     else
                                     {
                                         Assert.Equal (iterations, ApplicationOverlapped.OverlappedChildren!.Count);

                                         for (var i = 0; i < iterations; i++)
                                         {
                                             Assert.Equal ((iterations - i + 1).ToString (), ApplicationOverlapped.OverlappedChildren [i].Id);
                                         }
                                     }

                                     iterations--;
                                 };

        Application.Run (overlapped);

        Assert.Empty (ApplicationOverlapped.OverlappedChildren!);
        Assert.NotNull (ApplicationOverlapped.OverlappedTop);
        Assert.NotNull (Application.Top);
        overlapped.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Visible_False_Does_Not_Clear ()
    {
        var overlapped = new Overlapped ();
        var win1 = new Window { Width = 5, Height = 5, Visible = false };
        var win2 = new Window { X = 1, Y = 1, Width = 5, Height = 5 };
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 10);
        RunState rsOverlapped = Application.Begin (overlapped);

        // Need to fool MainLoop into thinking it's running
        Application.MainLoop!.Running = true;

        // RunIteration must be call on each iteration because
        // it's using the Begin and not the Run method
        var firstIteration = false;
        Application.RunIteration (ref rsOverlapped, ref firstIteration);

        Assert.Equal (overlapped, rsOverlapped.Toplevel);
        Assert.Equal (Application.Top, rsOverlapped.Toplevel);
        Assert.Equal (ApplicationOverlapped.OverlappedTop, rsOverlapped.Toplevel);
        Assert.Equal (Application.Current, rsOverlapped.Toplevel);
        Assert.Equal (overlapped, Application.Current);

        RunState rsWin1 = Application.Begin (win1);
        Application.RunIteration (ref rsOverlapped, ref firstIteration);

        Assert.Equal (overlapped, rsOverlapped.Toplevel);
        Assert.Equal (Application.Top, rsOverlapped.Toplevel);
        Assert.Equal (ApplicationOverlapped.OverlappedTop, rsOverlapped.Toplevel);
        // The win1 Visible is false and cannot be set as the Current
        Assert.Equal (Application.Current, rsOverlapped.Toplevel);
        Assert.Equal (overlapped, Application.Current);
        Assert.Equal (win1, rsWin1.Toplevel);

        RunState rsWin2 = Application.Begin (win2);
        Application.RunIteration (ref rsOverlapped, ref firstIteration);

        // Here the Current and the rsOverlapped.Toplevel is now the win2
        // and not the original overlapped
        Assert.Equal (win2, rsOverlapped.Toplevel);
        Assert.Equal (Application.Top, overlapped);
        Assert.Equal (ApplicationOverlapped.OverlappedTop, overlapped);
        Assert.Equal (Application.Current, rsWin2.Toplevel);
        Assert.Equal (win2, Application.Current);
        Assert.Equal (win1, rsWin1.Toplevel);

        // Tests that rely on visuals are too fragile. If border style changes they break.
        // Instead we should just rely on the test above.

        Application.OnMouseEvent (new MouseEvent { Position = new (1, 1), Flags = MouseFlags.Button1Pressed });
        Assert.Equal (win2.Border, Application.MouseGrabView);

        Application.RunIteration (ref rsOverlapped, ref firstIteration);

        Assert.Equal (win2, rsOverlapped.Toplevel);
        Assert.Equal (Application.Top, overlapped);
        Assert.Equal (ApplicationOverlapped.OverlappedTop, overlapped);
        Assert.Equal (Application.Current, rsWin2.Toplevel);
        Assert.Equal (win2, Application.Current);
        Assert.Equal (win1, rsWin1.Toplevel);

        Application.OnMouseEvent (new MouseEvent
        {
            Position = new (2, 2), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        });

        Application.RunIteration (ref rsOverlapped, ref firstIteration);

        Assert.Equal (win2, rsOverlapped.Toplevel);
        Assert.Equal (Application.Top, overlapped);
        Assert.Equal (ApplicationOverlapped.OverlappedTop, overlapped);
        Assert.Equal (Application.Current, rsWin2.Toplevel);
        Assert.Equal (win2, Application.Current);
        Assert.Equal (win1, rsWin1.Toplevel);

        // Tests that rely on visuals are too fragile. If border style changes they break.
        // Instead we should just rely on the test above.

        // This will end the win2 and not the overlapped
        Application.End (rsOverlapped);
        // rsOverlapped has been disposed and Toplevel property is null
        // So we must use another valid RunState to iterate
        Application.RunIteration (ref rsWin1, ref firstIteration);
#if DEBUG_IDISPOSABLE
        Assert.True (rsOverlapped.WasDisposed);
#endif
        Assert.Null (rsOverlapped.Toplevel);
        Assert.Equal (Application.Top, overlapped);
        Assert.Equal (ApplicationOverlapped.OverlappedTop, overlapped);
        Assert.Equal (Application.Current, rsWin1.Toplevel);
        Assert.Equal (win1, Application.Current);
        Assert.Equal (win1, rsWin1.Toplevel);

        Application.End (rsWin1);
        // rsWin1 has been disposed and Toplevel property is null
        // So we must use another valid RunState to iterate
        Application.RunIteration (ref rsWin2, ref firstIteration);
#if DEBUG_IDISPOSABLE
        Assert.True (rsOverlapped.WasDisposed);
        Assert.True (rsWin1.WasDisposed);
#endif
        Assert.Null (rsOverlapped.Toplevel);
        Assert.Equal (Application.Top, overlapped);
        Assert.Equal (ApplicationOverlapped.OverlappedTop, overlapped);
        Assert.Equal (Application.Current, overlapped);
        Assert.Null (rsWin1.Toplevel);
        // See here that the only Toplevel that needs to End is the overlapped
        // which the rsWin2 now has the Toplevel set to the overlapped
        Assert.Equal (overlapped, rsWin2.Toplevel);

        Application.End (rsWin2);
        // There is no more RunState to iteration
#if DEBUG_IDISPOSABLE
        Assert.True (rsOverlapped.WasDisposed);
        Assert.True (rsWin1.WasDisposed);
        Assert.True (rsWin2.WasDisposed);
#endif
        Assert.Null (rsOverlapped.Toplevel);
        Assert.Equal (Application.Top, overlapped);
        Assert.Equal (ApplicationOverlapped.OverlappedTop, overlapped);
        Assert.Null (Application.Current);
        Assert.Null (rsWin1.Toplevel);
        Assert.Null (rsWin2.Toplevel);

#if DEBUG_IDISPOSABLE
        Assert.False (win2.WasDisposed);
        Assert.False (win1.WasDisposed);
        Assert.False (overlapped.WasDisposed);
#endif
        // Now dispose all them
        win2.Dispose ();
        win1.Dispose ();
        overlapped.Dispose ();
        Application.Shutdown ();
    }

    private class Overlapped : Toplevel
    {
        public Overlapped () { IsOverlappedContainer = true; }
    }

    [Fact (Skip = "#2491: This test is really bogus. It does things like Runnable = false and is overly convolulted. Replace.")]
    [AutoInitShutdown]
    public void KeyBindings_Command_With_OverlappedTop ()
    {
        Toplevel top = new ();
        Assert.Null (ApplicationOverlapped.OverlappedTop);
        top.IsOverlappedContainer = true;
        Application.Begin (top);
        Assert.Equal (Application.Top, ApplicationOverlapped.OverlappedTop);

        var isRunning = true;

        var win1 = new Window { Id = "win1", Width = Dim.Percent (50), Height = Dim.Fill () };
        var lblTf1W1 = new Label { Text = "Enter text in TextField on Win1:" };
        var tf1W1 = new TextField { Id="tf1W1", X = Pos.Right (lblTf1W1) + 1, Width = Dim.Fill (), Text = "Text1 on Win1" };
        var lblTvW1 = new Label { Y = Pos.Bottom (lblTf1W1) + 1, Text = "Enter text in TextView on Win1:" };

        var tvW1 = new TextView
        {
            Id = "tvW1",
            X = Pos.Left (tf1W1), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win1"
        };
        var lblTf2W1 = new Label { Y = Pos.Bottom (lblTvW1) + 1, Text = "Enter text in TextField on Win1:" };
        var tf2W1 = new TextField { Id = "tf2W1", X = Pos.Left (tf1W1), Width = Dim.Fill (), Text = "Text2 on Win1" };
        win1.Add (lblTf1W1, tf1W1, lblTvW1, tvW1, lblTf2W1, tf2W1);

        var win2 = new Window { Id = "win2", Width = Dim.Percent (50), Height = Dim.Fill () };
        var lblTf1W2 = new Label { Text = "Enter text in TextField on Win2:" };
        var tf1W2 = new TextField { Id = "tf1W2", X = Pos.Right (lblTf1W2) + 1, Width = Dim.Fill (), Text = "Text1 on Win2" };
        var lblTvW2 = new Label { Y = Pos.Bottom (lblTf1W2) + 1, Text = "Enter text in TextView on Win2:" };

        var tvW2 = new TextView
        {
            Id = "tvW2",
            X = Pos.Left (tf1W2), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win2"
        };
        var lblTf2W2 = new Label { Y = Pos.Bottom (lblTvW2) + 1, Text = "Enter text in TextField on Win2:" };
        var tf2W2 = new TextField { Id = "tf2W2", X = Pos.Left (tf1W2), Width = Dim.Fill (), Text = "Text2 on Win2" };
        win2.Add (lblTf1W2, tf1W2, lblTvW2, tvW2, lblTf2W2, tf2W2);

        win1.Closing += (s, e) => isRunning = false;
        Assert.Null (top.Focused);
        Assert.Equal (top, Application.Current);
        Assert.True (top.IsCurrentTop);
        Assert.Equal (top, ApplicationOverlapped.OverlappedTop);

        Application.Begin (win1);

        Assert.Equal (new (0, 0, 40, 25), win1.Frame);
        Assert.NotEqual (top, Application.Current);
        Assert.False (top.IsCurrentTop);
        Assert.Equal (win1, Application.Current);
        Assert.True (win1.IsCurrentTop);
        Assert.True (ApplicationOverlapped.IsOverlapped(win1));
        Assert.Null (top.Focused);
        Assert.Null (top.MostFocused);
        Assert.Equal (tf1W1, win1.MostFocused);
        Assert.True (ApplicationOverlapped.IsOverlapped(win1));
        Assert.Single (ApplicationOverlapped.OverlappedChildren!);

        Application.Begin (win2);

        Assert.Equal (new (0, 0, 40, 25), win2.Frame);
        Assert.NotEqual (top, Application.Current);
        Assert.False (top.IsCurrentTop);
        Assert.Equal (win2, Application.Current);
        Assert.True (win2.IsCurrentTop);
        Assert.True (ApplicationOverlapped.IsOverlapped(win2));
        Assert.Null (top.Focused);
        Assert.Null (top.MostFocused);
        Assert.Equal (tf1W2, win2.MostFocused);
        Assert.Equal (2, ApplicationOverlapped.OverlappedChildren!.Count);

        ApplicationOverlapped.MoveToOverlappedChild (win1);
        Assert.Equal (win1, Application.Current);
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        win1.Running = true;
        Assert.True (Application.OnKeyDown (Application.QuitKey));
        Assert.False (isRunning);
        Assert.False (win1.Running);
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);

        // win1 has been closed. It can no longer be focused or acted upon.
        // win2 should now have focus
        Assert.Equal (win2, Application.Current);
        Assert.True (win2.IsCurrentTop);

        Assert.Equal (Environment.OSVersion.Platform == PlatformID.Unix, Application.OnKeyDown (Key.Z.WithCtrl)); // suspend

        Assert.True (Application.OnKeyDown (Key.F5)); // refresh

        Assert.True (win1.IsCurrentTop);
        Assert.Equal (tvW1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Key.Tab));
        Assert.Equal ($"\tFirst line Win1{Environment.NewLine}Second line Win1", tvW1.Text);

        Assert.True (Application.OnKeyDown (Key.Tab.WithShift));
        Assert.Equal ($"First line Win1{Environment.NewLine}Second line Win1", tvW1.Text);

        Assert.True (Application.OnKeyDown (Key.Tab.WithCtrl));    // move to win2
        Assert.Equal (win2, ApplicationOverlapped.OverlappedChildren [0]);

        Assert.True (Application.OnKeyDown (Key.Tab.WithCtrl.WithShift));    // move back to win1
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);

        Assert.Equal (tvW1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Key.Tab));    // text view eats tab
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tvW1, win1.MostFocused);

        tvW1.AllowsTab = false;
        Assert.True (Application.OnKeyDown (Key.Tab));    // text view eats tab
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf2W1, win1.MostFocused);

        Assert.True (Application.OnKeyDown (Key.CursorRight));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf2W1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Key.CursorDown));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf1W1, win1.MostFocused);
#if UNIX_KEY_BINDINGS
        Assert.True (ApplicationOverlapped.OverlappedChildren [0].ProcessKeyDown (new (Key.I.WithCtrl)));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf2W1, win1.MostFocused);
#endif
        Assert.True (Application.OnKeyDown (Key.Tab));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tvW1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Key.CursorLeft));  // The view to the left of tvW1 is tf2W1, but tvW1 is still focused and eats cursor keys
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tvW1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Key.CursorUp));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tvW1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Key.Tab));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf2W1, win1.MostFocused);

        Assert.True (Application.OnKeyDown (Key.Tab.WithCtrl)); // Move to win2
        Assert.Equal (win2, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf1W2, win2.MostFocused);
        tf2W2.SetFocus ();
        Assert.True (tf2W2.HasFocus);

        Assert.True (Application.OnKeyDown (Key.Tab.WithCtrl.WithShift));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf2W1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Application.AlternateForwardKey));
        Assert.Equal (win2, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf2W2, win2.MostFocused);
        Assert.True (Application.OnKeyDown (Application.AlternateBackwardKey));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf2W1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Key.CursorDown));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf1W1, win1.MostFocused);
#if UNIX_KEY_BINDINGS
        Assert.True (Application.OnKeyDown (new (Key.B.WithCtrl)));
#else
        Assert.True (Application.OnKeyDown (Key.CursorLeft));
#endif
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf1W1, win1.MostFocused);
        Assert.True (Application.OnKeyDown (Key.CursorDown));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tvW1, win1.MostFocused);
        Assert.Equal (Point.Empty, tvW1.CursorPosition);

        Assert.True (Application.OnKeyDown (Key.End.WithCtrl));
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tvW1, win1.MostFocused);
        Assert.Equal (new (16, 1), tvW1.CursorPosition); // Last position of the text
#if UNIX_KEY_BINDINGS
        Assert.True (Application.OnKeyDown (new (Key.F.WithCtrl)));
#else
        Assert.True (Application.OnKeyDown (Key.CursorRight)); // should move to next view w/ in Group (tf2W1)
#endif
        Assert.Equal (win1, ApplicationOverlapped.OverlappedChildren [0]);
        Assert.Equal (tf2W1, win1.MostFocused);

#if UNIX_KEY_BINDINGS
        Assert.True (ApplicationOverlapped.OverlappedChildren [0].ProcessKeyDown (new (Key.L.WithCtrl)));
#endif
        win2.Dispose ();
        win1.Dispose ();
        top.Dispose ();
    }


    [Fact]
    public void SetFocusToNextViewWithWrap_ShouldFocusNextView ()
    {
        // Arrange
        var superView = new TestToplevel () { Id = "superView", IsOverlappedContainer = true };

        var view1 = new TestView () { Id = "view1" };
        var view2 = new TestView () { Id = "view2" };
        var view3 = new TestView () { Id = "view3" }; ;
        superView.Add (view1, view2, view3);

        var current = new TestToplevel () { Id = "current", IsOverlappedContainer = true };

        superView.Add (current);
        superView.BeginInit ();
        superView.EndInit ();
        current.SetFocus ();

        Application.Current = current;
        Assert.True (current.HasFocus);
        Assert.Equal (superView.Focused, current);
        Assert.Equal (superView.MostFocused, current);

        // Act
        ApplicationOverlapped.SetFocusToNextViewWithWrap (Application.Current.SuperView.TabIndexes, NavigationDirection.Forward);

        // Assert
        Assert.True (view1.HasFocus);
    }

    [Fact]
    public void SetFocusToNextViewWithWrap_ShouldNotChangeFocusIfViewsIsNull ()
    {
        // Arrange
        var currentView = new TestToplevel ();
        Application.Current = currentView;

        // Act
        ApplicationOverlapped.SetFocusToNextViewWithWrap (null, NavigationDirection.Forward);

        // Assert
        Assert.Equal (currentView, Application.Current);
    }

    [Fact]
    public void SetFocusToNextViewWithWrap_ShouldNotChangeFocusIfCurrentViewNotFound ()
    {
        // Arrange
        var view1 = new TestToplevel ();
        var view2 = new TestToplevel ();
        var view3 = new TestToplevel ();

        var views = new List<View> { view1, view2, view3 };

        var currentView = new TestToplevel () { IsOverlappedContainer = true }; // Current view is not in the list
        Application.Current = currentView;

        // Act
        ApplicationOverlapped.SetFocusToNextViewWithWrap (views, NavigationDirection.Forward);

        // Assert
        Assert.False (view1.IsFocused);
        Assert.False (view2.IsFocused);
        Assert.False (view3.IsFocused);
    }

    private class TestToplevel : Toplevel
    {
        public bool IsFocused { get; private set; }

        public override bool OnEnter (View view)
        {
            IsFocused = true;
            return base.OnEnter (view);
        }

        public override bool OnLeave (View view)
        {
            IsFocused = false;
            return base.OnLeave (view);
        }
    }

    private class TestView : View
    {
        public TestView ()
        {
            CanFocus = true;
        }
        public bool IsFocused { get; private set; }

        public override bool OnEnter (View view)
        {
            IsFocused = true;
            return base.OnEnter (view);
        }

        public override bool OnLeave (View view)
        {
            IsFocused = false;
            return base.OnLeave (view);
        }
    }
}
