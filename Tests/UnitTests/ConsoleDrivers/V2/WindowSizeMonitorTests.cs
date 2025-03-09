using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class WindowSizeMonitorTests
{
    [Fact]
    public void TestWindowSizeMonitor_RaisesEventWhenChanges ()
    {
        var consoleOutput = new Mock<IConsoleOutput> ();

        var queue = new Queue<Size>(new []{
            new Size (30, 20),
            new Size (20, 20)

        });

        consoleOutput.Setup (m => m.GetWindowSize ())
                     .Returns (queue.Dequeue);

        var outputBuffer = Mock.Of<IOutputBuffer> ();

        var monitor = new WindowSizeMonitor (consoleOutput.Object, outputBuffer);

        var result = new List<SizeChangedEventArgs> ();
        monitor.SizeChanging += (s, e) => { result.Add (e);};

        Assert.Empty (result);
        monitor.Poll ();

        Assert.Single (result);
        Assert.Equal (new Size (30,20),result [0].Size);

        monitor.Poll ();

        Assert.Equal (2,result.Count);
        Assert.Equal (new Size (30, 20), result [0].Size);
        Assert.Equal (new Size (20, 20), result [1].Size);
    }

    [Fact]
    public void TestWindowSizeMonitor_DoesNotRaiseEventWhen_NoChanges ()
    {
        var consoleOutput = new Mock<IConsoleOutput> ();

        var queue = new Queue<Size> (new []{
            new Size (30, 20),
            new Size (30, 20),
        });

        consoleOutput.Setup (m => m.GetWindowSize ())
                     .Returns (queue.Dequeue);

        var outputBuffer = Mock.Of<IOutputBuffer> ();

        var monitor = new WindowSizeMonitor (consoleOutput.Object, outputBuffer);

        var result = new List<SizeChangedEventArgs> ();
        monitor.SizeChanging += (s, e) => { result.Add (e); };

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
