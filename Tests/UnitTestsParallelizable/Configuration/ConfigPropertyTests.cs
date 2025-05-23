using System.Collections.Concurrent;

namespace Terminal.Gui.ConfigurationTests;

public class ConfigPropertyTests
{
    private class Dummy
    {
        public string Value { get; set; } = string.Empty;
    }

    [Fact]
    public void PropertyValue_CanBeSetAndReadConcurrently ()
    {
        var propertyInfo = typeof (Dummy).GetProperty (nameof (Dummy.Value));
        var configProperty = new ConfigProperty { PropertyInfo = propertyInfo };

        int numTasks = 20;
        var values = new string [numTasks];
        for (int i = 0; i < numTasks; i++)
        {
            values [i] = $"Value_{i}";
        }

        Parallel.For (0, numTasks, i =>
                                   {
                                       configProperty.PropertyValue = values [i];
                                       // Remove the per-thread assertion, as it is not valid in a concurrent context.
                                       // Optionally, you can check that the value is one of the expected values:
                                       var currentValue = configProperty.PropertyValue as string;
                                       Assert.Contains (currentValue, values);
                                   });
    }


    [Fact]
    public void UpdateFrom_CanBeCalledConcurrently ()
    {
        var propertyInfo = typeof (Dummy).GetProperty (nameof (Dummy.Value));
        var configProperty = new ConfigProperty { PropertyInfo = propertyInfo, PropertyValue = "Initial" };

        int numTasks = 20;
        Parallel.For (0, numTasks, i =>
                                   {
                                       var newValue = $"Thread_{i}";
                                       configProperty.UpdateFrom (newValue);
                                   });

        var finalValue = configProperty.PropertyValue as string;
        Assert.StartsWith ("Thread_", finalValue);
    }


    [Fact]
    public void DeepCloner_DeepClone_IsThreadSafe ()
    {
        var propertyInfo = typeof (Dummy).GetProperty (nameof (Dummy.Value));
        var configProperty = new ConfigProperty { PropertyInfo = propertyInfo, PropertyValue = "DeepCloneValue" };

        int numTasks = 20;
        Parallel.For (0, numTasks, i =>
        {
            var clone = DeepCloner.DeepClone (configProperty);
            Assert.NotSame (configProperty, clone);
            Assert.Equal ("DeepCloneValue", clone.PropertyValue);
        });
    }

    [Fact]
    public void ConcurrentDictionary_UpdateFrom_IsThreadSafe ()
    {
        var propertyInfo = typeof (Dummy).GetProperty (nameof (Dummy.Value));
        var destinationDict = new ConcurrentDictionary<string, ConfigProperty> (StringComparer.InvariantCultureIgnoreCase);
        destinationDict.TryAdd ("prop1", new ConfigProperty { PropertyValue = "Original", HasValue = true });

        var configProperty = new ConfigProperty
        {
            PropertyInfo = propertyInfo,
            PropertyValue = destinationDict
        };

        int numTasks = 20;
        Parallel.For (0, numTasks, i =>
        {
            var sourceDict = new ConcurrentDictionary<string, ConfigProperty> (StringComparer.InvariantCultureIgnoreCase);
            sourceDict.TryAdd ($"prop{i}", new ConfigProperty { PropertyValue = $"Value_{i}", HasValue = true });
            configProperty.UpdateFrom (sourceDict);
        });

        var resultDict = configProperty.PropertyValue as ConcurrentDictionary<string, ConfigProperty>;
        Assert.NotNull (resultDict);
        for (int i = 0; i < numTasks; i++)
        {
            Assert.True (resultDict!.ContainsKey ($"prop{i}"));
            Assert.Equal ($"Value_{i}", resultDict [$"prop{i}"].PropertyValue);
        }
        Assert.True (resultDict!.ContainsKey ("prop1"));
    }
}
