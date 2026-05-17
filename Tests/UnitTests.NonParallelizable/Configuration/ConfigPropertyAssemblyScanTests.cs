using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

namespace UnitTests.NonParallelizable.ConfigurationTests;

public class ConfigPropertyAssemblyScanTests
{
    [ConfigurationProperty (Scope = typeof (ConfigPropertyAssemblyScanTestsScope))]
    public static bool? ScanSentinel { get; set; }

    private class ConfigPropertyAssemblyScanTestsScope : Scope<ConfigPropertyAssemblyScanTestsScope>
    {
    }

    [Fact]
    public void ScanAssembliesForConfigPropertyHosts_Skips_TypeLoadException_From_Foreign_CustomAttribute_Metadata ()
    {
        string fixtureRoot = Path.Combine (AppContext.BaseDirectory, "ConfigPropertyTypeLoadFixtures");
        string providerPath = Path.Combine (fixtureRoot, "FakeCodeAnalysis.dll");
        string consumerPath = Path.Combine (fixtureRoot, "ConsumerLib.dll");

        Assert.True (File.Exists (providerPath));
        Assert.True (File.Exists (consumerPath));

        AssemblyLoadContext loadContext = new (nameof (ConfigPropertyAssemblyScanTests), isCollectible: true);
        loadContext.LoadFromAssemblyPath (providerPath);
        Assembly consumerAssembly = loadContext.LoadFromAssemblyPath (consumerPath);
        Type consumerType = consumerAssembly.GetType ("ConsumerLib.FixtureType", throwOnError: true)!;
        PropertyInfo consumerProperty = consumerType.GetProperty ("HasValue")!;

        Assert.Throws<TypeLoadException> (
                                          () => consumerProperty.GetCustomAttribute (typeof (ConfigurationPropertyAttribute))
                                         );

        ImmutableSortedDictionary<string, Type> hosts =
            ConfigProperty.ScanAssembliesForConfigPropertyHosts ([consumerAssembly, typeof (ConfigPropertyAssemblyScanTests).Assembly]);

        Assert.Contains (nameof (ConfigPropertyAssemblyScanTests), hosts.Keys);
        Assert.Same (typeof (ConfigPropertyAssemblyScanTests), hosts [nameof (ConfigPropertyAssemblyScanTests)]);

        loadContext.Unload ();
    }
}
