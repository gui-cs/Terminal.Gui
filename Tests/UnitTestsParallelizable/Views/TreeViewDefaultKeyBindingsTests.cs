// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TreeView{T}.DefaultKeyBindings"/> static property.
/// </summary>
public class TreeViewDefaultKeyBindingsTests
{
    [Fact]
    public void TreeView_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TreeView<ITreeNode>.DefaultKeyBindings);

    [Fact]
    public void TreeView_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((string commandName, PlatformKeyBinding platformBinding) in TreeView<ITreeNode>.DefaultKeyBindings!)
        {
            string [] [] allKeyArrays = [platformBinding.All ?? [], platformBinding.Windows ?? [], platformBinding.Linux ?? [], platformBinding.Macos ?? []];

            foreach (string [] keyArray in allKeyArrays)
            {
                foreach (string keyString in keyArray)
                {
                    Assert.True (Key.TryParse (keyString, out _), $"Key string '{keyString}' for command '{commandName}' should be parseable.");
                }
            }
        }
    }

    [Fact]
    public void TreeView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (string commandName in TreeView<ITreeNode>.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.TryParse<Command> (commandName, out _), $"Command name '{commandName}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TreeView_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property =
            typeof (TreeView<ITreeNode>).GetProperty (nameof (TreeView<ITreeNode>.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }

    [Fact]
    public void TreeView_DefaultKeyBindings_ContainsUniqueCommands ()
    {
        Dictionary<string, PlatformKeyBinding> bindings = TreeView<ITreeNode>.DefaultKeyBindings!;

        // Tree-specific commands
        Assert.True (bindings.ContainsKey ("Expand"), "DefaultKeyBindings should contain Expand.");
        Assert.True (bindings.ContainsKey ("ExpandAll"), "DefaultKeyBindings should contain ExpandAll.");
        Assert.True (bindings.ContainsKey ("Collapse"), "DefaultKeyBindings should contain Collapse.");
        Assert.True (bindings.ContainsKey ("CollapseAll"), "DefaultKeyBindings should contain CollapseAll.");
        Assert.True (bindings.ContainsKey ("LineUpToFirstBranch"), "DefaultKeyBindings should contain LineUpToFirstBranch.");
        Assert.True (bindings.ContainsKey ("LineDownToLastBranch"), "DefaultKeyBindings should contain LineDownToLastBranch.");

        // TreeView overrides Start/End to use Home/End instead of Ctrl+Home/Ctrl+End
        Assert.True (bindings.ContainsKey ("Start"), "DefaultKeyBindings should contain Start.");
        Assert.True (bindings.ContainsKey ("End"), "DefaultKeyBindings should contain End.");
    }
}
