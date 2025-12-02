using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Terminal.Gui.Examples;

/// <summary>
///     Provides methods for running example applications in various execution modes.
/// </summary>
public static class ExampleRunner
{
    /// <summary>
    ///     Runs an example with the specified context.
    /// </summary>
    /// <param name="example">The example information.</param>
    /// <param name="context">The execution context.</param>
    /// <returns>The result of running the example.</returns>
    [RequiresUnreferencedCode ("Calls System.Reflection.Assembly.LoadFrom")]
    [RequiresDynamicCode ("Calls System.Reflection.Assembly.LoadFrom")]
    public static ExampleResult Run (ExampleInfo example, ExampleContext context)
    {
        return context.Mode == ExecutionMode.InProcess
                   ? RunInProcess (example, context)
                   : RunOutOfProcess (example, context);
    }

    private static ExampleMetrics? ExtractMetricsFromOutput (string output)
    {
        // Look for the metrics marker in the output
        Match match = Regex.Match (output, @"###TERMGUI_METRICS:(.+?)###");

        if (!match.Success)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ExampleMetrics> (match.Groups [1].Value);
        }
        catch
        {
            return null;
        }
    }

    [RequiresUnreferencedCode ("Calls System.Reflection.Assembly.LoadFrom")]
    [RequiresDynamicCode ("Calls System.Reflection.Assembly.LoadFrom")]
    private static ExampleResult RunInProcess (ExampleInfo example, ExampleContext context)
    {
        Environment.SetEnvironmentVariable (
                                            ExampleContext.ENVIRONMENT_VARIABLE_NAME,
                                            context.ToJson ());

        try
        {
            Assembly asm = Assembly.LoadFrom (example.AssemblyPath);
            MethodInfo? entryPoint = asm.EntryPoint;

            if (entryPoint is null)
            {
                return new ()
                {
                    Success = false,
                    ErrorMessage = "Assembly does not have an entry point"
                };
            }

            ParameterInfo [] parameters = entryPoint.GetParameters ();

            Task executionTask = Task.Run (() =>
            {
                object? result = null;

                if (parameters.Length == 0)
                {
                    result = entryPoint.Invoke (null, null);
                }
                else if (parameters.Length == 1 && parameters [0].ParameterType == typeof (string []))
                {
                    result = entryPoint.Invoke (null, [Array.Empty<string> ()]);
                }
                else
                {
                    throw new InvalidOperationException ("Entry point has unsupported signature");
                }

                // If entry point returns Task, wait for it
                if (result is Task task)
                {
                    task.GetAwaiter ().GetResult ();
                }
            });

            bool completed = executionTask.Wait (context.TimeoutMs);

            if (!completed)
            {
                // reset terminal
                Console.Clear ();
                return new ()
                {
                    Success = false,
                    TimedOut = true
                };
            }

            if (executionTask.Exception is { })
            {
                throw executionTask.Exception.GetBaseException ();
            }

            return new ()
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new ()
            {
                Success = false,
                ErrorMessage = ex.ToString ()
            };
        }
        finally
        {
            Environment.SetEnvironmentVariable (ExampleContext.ENVIRONMENT_VARIABLE_NAME, null);
        }
    }

    private static ExampleResult RunOutOfProcess (ExampleInfo example, ExampleContext context)
    {
        ProcessStartInfo psi = new ()
        {
            FileName = "dotnet",
            Arguments = $"\"{example.AssemblyPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        psi.Environment [ExampleContext.ENVIRONMENT_VARIABLE_NAME] = context.ToJson ();

        using Process? process = Process.Start (psi);

        if (process is null)
        {
            return new ()
            {
                Success = false,
                ErrorMessage = "Failed to start process"
            };
        }

        bool exited = process.WaitForExit (context.TimeoutMs);
        string stdout = process.StandardOutput.ReadToEnd ();
        string stderr = process.StandardError.ReadToEnd ();

        if (!exited)
        {
            try
            {
                const bool KILL_ENTIRE_PROCESS_TREE = true;
                process.Kill (KILL_ENTIRE_PROCESS_TREE);
            }
            catch
            {
                // Ignore errors killing the process
            }

            return new ()
            {
                Success = false,
                TimedOut = true,
                StandardOutput = stdout,
                StandardError = stderr
            };
        }

        ExampleMetrics? metrics = ExtractMetricsFromOutput (stdout);

        return new ()
        {
            Success = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            StandardOutput = stdout,
            StandardError = stderr,
            Metrics = metrics
        };
    }
}
