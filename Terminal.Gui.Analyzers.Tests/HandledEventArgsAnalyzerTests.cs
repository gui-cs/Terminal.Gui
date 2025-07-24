using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Terminal.Gui.Analyzers.Tests;

public class HandledEventArgsAnalyzerTests
{
    [Theory]
    [InlineData("e")]
    [InlineData ("args")]
    public async Task Should_ReportDiagnostic_When_EHandledNotSet_Lambda (string paramName)
    {
        var originalCode = $$"""
                            using Terminal.Gui.Views;

                            class TestClass
                            {
                                void Setup()
                                {
                                    var b = new Button();
                                    b.Accepting += (s, {{paramName}}) =>
                                    {
                                        // Forgot {{paramName}}.Handled = true;
                                    };
                                }
                            }
                            """;
        await new ProjectBuilder ()
              .WithSourceCode (originalCode)
              .WithAnalyzer (new HandledEventArgsAnalyzer ())
              .ValidateAsync ();
    }

    [Theory]
    [InlineData ("e")]
    [InlineData ("args")]
    public async Task Should_ReportDiagnostic_When_EHandledNotSet_Method (string paramName)
    {
        var originalCode = $$"""
                            using Terminal.Gui.Views;
                            using Terminal.Gui.Input;

                            class TestClass
                            {
                                void Setup()
                                {
                                    var b = new Button();
                                    b.Accepting += BOnAccepting;
                                }
                                private void BOnAccepting (object? sender, CommandEventArgs {{paramName}})
                                {

                                }
                            }
                            """;
        await new ProjectBuilder ()
              .WithSourceCode (originalCode)
              .WithAnalyzer (new HandledEventArgsAnalyzer ())
              .ValidateAsync ();
    }
}
