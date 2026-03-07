#nullable enable
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.ApplicationTests;

public class MainLoopCoordinatorTests
{
    [Fact]
    public async Task StartInputTaskAsync_EnablesAndDisablesKittyKeyboard_WhenTerminalResponds ()
    {
        ConcurrentQueue<char> inputQueue = new ();
        TimedEvents timedEvents = new ();
        ApplicationMainLoop<char> loop = new ();
        TestAnsiInput input = new ("\u001B[?31u");
        AnsiOutput output = new ();
        TestAnsiComponentFactory factory = new (input, output);
        MainLoopCoordinator<char> coordinator = new (timedEvents, inputQueue, loop, factory);
        Mock<IApplication> appMock = new ();

        appMock.SetupProperty (a => a.Driver);
        appMock.SetupProperty (a => a.MainThreadId, 123);

        await coordinator.StartInputTaskAsync (appMock.Object);

        Assert.True (SpinWait.SpinUntil (() => input.ResponseSent, TimeSpan.FromSeconds (1)));
        loop.InputProcessor.ProcessQueue ();

        DriverImpl driver = Assert.IsType<DriverImpl> (appMock.Object.Driver);
        Assert.True (driver.KittyKeyboardProtocol.IsSupported);
        Assert.Equal (31, driver.KittyKeyboardProtocol.SupportedFlags);
        Assert.Equal (EscSeqUtils.KittyKeyboardPhase1Flags, driver.KittyKeyboardProtocol.EnabledFlags);
        Assert.Contains (EscSeqUtils.CSI_EnableKittyKeyboardFlags (EscSeqUtils.KittyKeyboardPhase1Flags), output.GetLastOutput (), StringComparison.Ordinal);

        coordinator.Stop ();

        Assert.Contains (EscSeqUtils.CSI_DisableKittyKeyboardFlags, output.GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartInputTaskAsync_DoesNotEnableKittyKeyboard_ForLegacyConsole ()
    {
        ConcurrentQueue<char> inputQueue = new ();
        TimedEvents timedEvents = new ();
        ApplicationMainLoop<char> loop = new ();
        TestAnsiInput input = new (null);
        AnsiOutput output = new () { IsLegacyConsole = true };
        TestAnsiComponentFactory factory = new (input, output);
        MainLoopCoordinator<char> coordinator = new (timedEvents, inputQueue, loop, factory);
        Mock<IApplication> appMock = new ();

        appMock.SetupProperty (a => a.Driver);
        appMock.SetupProperty (a => a.MainThreadId, 456);

        await coordinator.StartInputTaskAsync (appMock.Object);

        DriverImpl driver = Assert.IsType<DriverImpl> (appMock.Object.Driver);
        Assert.False (driver.KittyKeyboardProtocol.IsSupported);
        Assert.DoesNotContain (EscSeqUtils.CSI_EnableKittyKeyboardFlags (EscSeqUtils.KittyKeyboardPhase1Flags), output.GetLastOutput (), StringComparison.Ordinal);

        coordinator.Stop ();
    }

    [Fact]
    public async Task TestMainLoopCoordinator_InputCrashes_ExceptionSurfacesMainThread ()
    {
        Mock<ILogger> mockLogger = new ();

        ILogger beforeLogger = Logging.Logger;
        Logging.Logger = mockLogger.Object;

        Mock<IComponentFactory<char>> m = new ();

        m.Setup (f => f.CreateInput ()).Throws (new Exception ("Crash on boot"));

        MainLoopCoordinator<char> c = new (new TimedEvents (),
                                           new ConcurrentQueue<char> (),
                                           Mock.Of<IApplicationMainLoop<char>> (),
                                           m.Object);

        AggregateException ex = await Assert.ThrowsAsync<AggregateException> (() => c.StartInputTaskAsync (null));
        Assert.Equal ("Crash on boot", ex.InnerExceptions [0].Message);

        Logging.Logger = beforeLogger;

        mockLogger.Verify (l => l.Log (LogLevel.Critical,
                                       It.IsAny<EventId> (),
                                       It.Is<It.IsAnyType> ((v, t) => v.ToString ()!.Contains ("Input loop crashed")),
                                       It.IsAny<Exception> (),
                                       It.IsAny<Func<It.IsAnyType, Exception?, string>> ()),
                           Times.Once);
    }

    private sealed class TestAnsiComponentFactory : ComponentFactoryImpl<char>
    {
        private readonly TestAnsiInput _input;
        private readonly AnsiOutput _output;

        public TestAnsiComponentFactory (TestAnsiInput input, AnsiOutput output)
        {
            _input = input;
            _output = output;
        }

        public override string? GetDriverName () => DriverRegistry.Names.ANSI;

        public override IInput<char> CreateInput () => _input;

        public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer, ITimeProvider? timeProvider = null) => new AnsiInputProcessor (inputBuffer, timeProvider);

        public override IOutput CreateOutput () => _output;

        public override ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer) => new SizeMonitorImpl (consoleOutput);
    }

    private sealed class TestAnsiInput : IInput<char>
    {
        private ConcurrentQueue<char>? _inputQueue;
        private bool _responseSent;
        private readonly string? _response;

        public TestAnsiInput (string? response)
        {
            _response = response;
        }

        public CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

        public bool ResponseSent => _responseSent;

        public void Initialize (ConcurrentQueue<char> inputQueue) => _inputQueue = inputQueue;

        public void Run (CancellationToken runCancellationToken)
        {
            if (!_responseSent && !string.IsNullOrEmpty (_response) && _inputQueue is { } inputQueue)
            {
                foreach (char ch in _response)
                {
                    inputQueue.Enqueue (ch);
                }

                _responseSent = true;
            }

            WaitHandle.WaitAny ([runCancellationToken.WaitHandle]);
            throw new OperationCanceledException (runCancellationToken);
        }

        public void Dispose () { }
    }
}
