using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Terminal.Gui.Examples;

/// <summary>
///     Provides methods for discovering example applications by scanning assemblies for example metadata attributes.
/// </summary>
public static class ExampleDiscovery
{
    /// <summary>
    ///     Discovers examples from the specified assembly file paths.
    /// </summary>
    /// <param name="assemblyPaths">The paths to assembly files to scan for examples.</param>
    /// <returns>An enumerable of <see cref="ExampleInfo"/> objects for each discovered example.</returns>
    [RequiresUnreferencedCode ("Calls System.Reflection.Assembly.LoadFrom")]
    [RequiresDynamicCode ("Calls System.Reflection.Assembly.LoadFrom")]
    public static IEnumerable<ExampleInfo> DiscoverFromFiles (params string [] assemblyPaths)
    {
        foreach (string path in assemblyPaths)
        {
            if (!File.Exists (path))
            {
                continue;
            }

            Assembly? asm = null;

            try
            {
                asm = Assembly.LoadFrom (path);
            }
            catch
            {
                // Skip assemblies that can't be loaded
                continue;
            }

            ExampleMetadataAttribute? metadata = asm.GetCustomAttribute<ExampleMetadataAttribute> ();

            if (metadata is null)
            {
                continue;
            }

            ExampleInfo info = new ()
            {
                Name = metadata.Name,
                Description = metadata.Description,
                AssemblyPath = path,
                Categories = asm.GetCustomAttributes<ExampleCategoryAttribute> ()
                                .Select (c => c.Category)
                                .ToList (),
                DemoKeyStrokes = ParseDemoKeyStrokes (asm)
            };

            yield return info;
        }
    }

    /// <summary>
    ///     Discovers examples from assemblies in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to search for assembly files.</param>
    /// <param name="searchPattern">The search pattern for assembly files (default is "*.dll").</param>
    /// <param name="searchOption">The search option for traversing subdirectories.</param>
    /// <returns>An enumerable of <see cref="ExampleInfo"/> objects for each discovered example.</returns>
    [RequiresUnreferencedCode ("Calls System.Reflection.Assembly.LoadFrom")]
    [RequiresDynamicCode ("Calls System.Reflection.Assembly.LoadFrom")]
    public static IEnumerable<ExampleInfo> DiscoverFromDirectory (
        string directory,
        string searchPattern = "*.dll",
        SearchOption searchOption = SearchOption.AllDirectories
    )
    {
        if (!Directory.Exists (directory))
        {
            return [];
        }

        string [] assemblyPaths = Directory.GetFiles (directory, searchPattern, searchOption);

        return DiscoverFromFiles (assemblyPaths);
    }

    private static List<DemoKeyStrokeSequence> ParseDemoKeyStrokes (Assembly assembly)
    {
        List<DemoKeyStrokeSequence> sequences = new ();

        foreach (ExampleDemoKeyStrokesAttribute attr in assembly.GetCustomAttributes<ExampleDemoKeyStrokesAttribute> ())
        {
            List<string> keys = new ();

            if (attr.KeyStrokes is { Length: > 0 })
            {
                keys.AddRange (attr.KeyStrokes);
            }

            if (!string.IsNullOrEmpty (attr.RepeatKey))
            {
                for (var i = 0; i < attr.RepeatCount; i++)
                {
                    keys.Add (attr.RepeatKey);
                }
            }

            if (keys.Count > 0)
            {
                sequences.Add (
                               new ()
                               {
                                   KeyStrokes = keys.ToArray (),
                                   DelayMs = attr.DelayMs,
                                   Order = attr.Order
                               });
            }
        }

        return sequences.OrderBy (s => s.Order).ToList ();
    }
}
