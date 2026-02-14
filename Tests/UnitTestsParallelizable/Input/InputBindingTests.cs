namespace InputTests;

/// <summary>
///     Tests for <see cref="InputBinding"/> record struct.
/// </summary>
/// <remarks>
///     Copilot generated.
/// </remarks>
public class InputBindingTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithCommands_SetsCommands ()
    {
        Command [] commands = [Command.Activate, Command.Accept];

        InputBinding binding = new (commands);

        Assert.Equal (commands, binding.Commands);
        Assert.Null (binding.Source);
        Assert.Null (binding.Data);
    }

    [Fact]
    public void Constructor_WithCommandsAndSource_SetsBothProperties ()
    {
        Command [] commands = [Command.Activate];
        View source = new () { Id = "sourceView" };

        InputBinding binding = new (commands, source);

        Assert.Equal (commands, binding.Commands);
        Assert.Equal (source, binding.Source);
        Assert.Null (binding.Data);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties ()
    {
        Command [] commands = [Command.Accept];
        View source = new () { Id = "sourceView" };
        object data = "test data";

        InputBinding binding = new (commands, source, data);

        Assert.Equal (commands, binding.Commands);
        Assert.Equal (source, binding.Source);
        Assert.Equal ("test data", binding.Data);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Commands_CanBeModified ()
    {
        InputBinding binding = new ([Command.Activate]);

        binding.Commands = [Command.Accept, Command.Cancel];

        Assert.Equal (2, binding.Commands.Length);
        Assert.Equal (Command.Accept, binding.Commands [0]);
        Assert.Equal (Command.Cancel, binding.Commands [1]);
    }

    [Fact]
    public void Source_CanBeModified ()
    {
        InputBinding binding = new ([Command.Activate]);
        View source = new () { Id = "newSource" };

        binding.Source = source;

        Assert.Equal ("newSource", binding.Source?.Id);
    }

    [Fact]
    public void Data_CanBeModified ()
    {
        InputBinding binding = new ([Command.Activate]);

        binding.Data = 42;

        Assert.Equal (42, binding.Data);
    }

    #endregion

    #region IInputBinding Interface Tests

    [Fact]
    public void ImplementsIInputBinding ()
    {
        InputBinding binding = new ([Command.Activate]) { Source = new View { Id = "test" }, Data = "data" };

        IInputBinding iBinding = binding;

        Assert.Equal (binding.Commands, iBinding.Commands);
        Assert.Equal (binding.Source, iBinding.Source);
        Assert.Equal (binding.Data, iBinding.Data);
    }

    [Fact]
    public void CanBeUsedPolymorphically ()
    {
        IInputBinding binding = new InputBinding ([Command.Accept], new View { Id = "polymorphic" });

        Assert.Single (binding.Commands);
        Assert.Equal (Command.Accept, binding.Commands [0]);
        Assert.Equal ("polymorphic", binding.Source?.Id);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void PatternMatching_CanDistinguishFromKeyBinding ()
    {
        IInputBinding inputBinding = new InputBinding ([Command.Activate]);
        IInputBinding keyBinding = new KeyBinding ([Command.Activate]) { Key = Key.Enter };

        Assert.True (inputBinding is InputBinding);
        Assert.False (inputBinding is KeyBinding);
        Assert.True (keyBinding is KeyBinding);
        Assert.False (keyBinding is InputBinding);
    }

    [Fact]
    public void PatternMatching_CanDistinguishFromMouseBinding ()
    {
        IInputBinding inputBinding = new InputBinding ([Command.Activate]);
        IInputBinding mouseBinding = new MouseBinding ([Command.Activate], MouseFlags.LeftButtonClicked);

        Assert.True (inputBinding is InputBinding);
        Assert.False (inputBinding is MouseBinding);
        Assert.True (mouseBinding is MouseBinding);
        Assert.False (mouseBinding is InputBinding);
    }

    [Fact]
    public void PatternMatching_SwitchExpression_Works ()
    {
        IInputBinding binding = new InputBinding ([Command.Accept], new View { Id = "source" });

        string bindingType = binding switch
                             {
                                 KeyBinding => "key",
                                 MouseBinding => "mouse",
                                 InputBinding => "input",
                                 _ => "unknown"
                             };

        Assert.Equal ("input", bindingType);
    }

    #endregion

    #region CommandContext Integration Tests

    [Fact]
    public void CanBeUsedWithCommandContext ()
    {
        View source = new () { Id = "contextSource" };
        InputBinding binding = new ([Command.Activate], source, "contextData");

        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View>(source), Binding = binding };

        Assert.Equal (Command.Activate, ctx.Command);
        Assert.NotNull (ctx.Source);
        Assert.True (ctx.Source.TryGetTarget (out View? view));
        Assert.Equal (source, view);
        Assert.NotNull (ctx.Binding);

        if (ctx.Binding is InputBinding ib)
        {
            Assert.Equal ("contextData", ib.Data);
        }
        else
        {
            Assert.Fail ("Binding should be InputBinding");
        }
    }

    [Fact]
    public void Binding_Property_ReturnsIInputBinding ()
    {
        InputBinding binding = new ([Command.Accept]);

        CommandContext ctx = new () { Command = Command.Accept, Binding = binding };

        // Binding property (from ICommandContext) returns IInputBinding
        Assert.NotNull (ctx.Binding);
        Assert.IsType<InputBinding> (ctx.Binding);
    }

    [Fact]
    public void PatternMatching_ThroughICommandContext_Works ()
    {
        InputBinding binding = new ([Command.Accept], new View { Id = "test" });
        ICommandContext ctx = new CommandContext { Command = Command.Accept, Binding = binding };

        // Can pattern match the binding from the interface
        if (ctx.Binding is InputBinding ib)
        {
            Assert.Equal ("test", ib.Source?.Id);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match InputBinding from ICommandContext.Binding");
        }
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void RecordEquality_SameValues_AreEqual ()
    {
        Command [] commands = [Command.Activate];
        View source = new () { Id = "source" };

        InputBinding binding1 = new (commands, source, "data");
        InputBinding binding2 = new (commands, source, "data");

        Assert.Equal (binding1, binding2);
    }

    [Fact]
    public void RecordEquality_DifferentCommands_AreNotEqual ()
    {
        InputBinding binding1 = new ([Command.Activate]);
        InputBinding binding2 = new ([Command.Accept]);

        Assert.NotEqual (binding1, binding2);
    }

    [Fact]
    public void RecordEquality_DifferentSources_AreNotEqual ()
    {
        InputBinding binding1 = new ([Command.Activate], new View { Id = "source1" });
        InputBinding binding2 = new ([Command.Activate], new View { Id = "source2" });

        Assert.NotEqual (binding1, binding2);
    }

    #endregion
}
