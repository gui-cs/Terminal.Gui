using Moq;

namespace UnitTests.DriverTests;

public class WindowSizeMonitorTests
{
    public WindowSizeMonitorTests () { ConsoleDriver.RunningUnitTests = false; }

    [Fact]
    public void TestWindowSizeMonitor_RaisesEventWhenChanges ()
    {
        Mock<IConsoleOutput> consoleOutput = new ();

        Queue<Size> queue = new (
                                 [
                                     new (30, 20),
                                     new (20, 20)
                                 ]);

        consoleOutput.Setup (m => m.GetSize ())
                     .Returns (queue.Dequeue);

        var outputBuffer = Mock.Of<IOutputBuffer> ();

        var monitor = new ConsoleSizeMonitor (consoleOutput.Object, outputBuffer);

        List<SizeChangedEventArgs> result = new ();
        monitor.SizeChanged += (s, e) => { result.Add (e); };

        Assert.Empty (result);
        monitor.Poll ();

        Assert.Single (result);
        Assert.Equal (new Size (30, 20), result [0].Size);

        monitor.Poll ();

        Assert.Equal (2, result.Count);
        Assert.Equal (new Size (30, 20), result [0].Size);
        Assert.Equal (new Size (20, 20), result [1].Size);
    }

    [Fact]
    public void TestWindowSizeMonitor_DoesNotRaiseEventWhen_NoChanges ()
    {
        Mock<IConsoleOutput> consoleOutput = new ();

        Queue<Size> queue = new (
                                 new []
                                 {
                                     new Size (30, 20),
                                     new Size (30, 20)
                                 });

        consoleOutput.Setup (m => m.GetSize ())
                     .Returns (queue.Dequeue);

        var outputBuffer = Mock.Of<IOutputBuffer> ();

        var monitor = new ConsoleSizeMonitor (consoleOutput.Object, outputBuffer);

        List<SizeChangedEventArgs> result = new ();
        monitor.SizeChanged += (s, e) => { result.Add (e); };

        // First poll always raises event because going from unknown size i.e. 0,0
        Assert.Empty (result);
        monitor.Poll ();

        Assert.Single (result);
        Assert.Equal (new Size (30, 20), result [0].Size);

        // No change 
        monitor.Poll ();

        Assert.Single (result);
        Assert.Equal (new Size (30, 20), result [0].Size);
    }
}
