namespace InputTests;

/// <summary>
///     Tests for <see cref="CommandContext{TBindingType}"/> record struct.
/// </summary>
/// <remarks>
///     Copilot generated.
/// </remarks>
public class CommandContextTests
{
    #region Basic Property Tests

    [Fact]
    public void CommandContext_WithKeyBinding_SetsProperties ()
    {
        View sourceView = new () { Id = "sourceView" };
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Enter };

        CommandContext<KeyBinding> ctx = new () { Command = Command.Activate, Source = sourceView, Binding = keyBinding };

        Assert.Equal (Command.Activate, ctx.Command);
        Assert.Equal (sourceView, ctx.Source);
        Assert.Equal (keyBinding, ctx.Binding);
        Assert.Equal (Key.Enter, ctx.Binding.Key);
    }

    [Fact]
    public void CommandContext_WithMouseBinding_SetsProperties ()
    {
        View sourceView = new () { Id = "sourceView" };
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonClicked);

        CommandContext<MouseBinding> ctx = new () { Command = Command.Activate, Source = sourceView, Binding = mouseBinding };

        Assert.Equal (Command.Activate, ctx.Command);
        Assert.Equal (sourceView, ctx.Source);
        Assert.Equal (mouseBinding, ctx.Binding);
        Assert.NotNull (ctx.Binding.MouseEvent);
        Assert.Equal (MouseFlags.LeftButtonClicked, ctx.Binding.MouseEvent!.Flags);
    }

    #endregion

    #region ICommandContext Interface Tests

    [Fact]
    public void CommandContext_ImplementsICommandContext ()
    {
        CommandContext<KeyBinding> ctx = new () { Command = Command.Accept, Source = new View () };

        ICommandContext iCtx = ctx;

        Assert.Equal (Command.Accept, iCtx.Command);
        Assert.NotNull (iCtx.Source);
    }

    [Fact]
    public void Source_IsMutable_ThroughInterface ()
    {
        View originalSource = new () { Id = "original" };
        View newSource = new () { Id = "new" };

        CommandContext<KeyBinding> ctx = new () { Command = Command.Accept, Source = originalSource };

        ICommandContext iCtx = ctx;
        iCtx.Source = newSource;

        Assert.Equal (newSource, iCtx.Source);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void PatternMatching_KeyBinding_Works ()
    {
        ICommandContext? ctx = new CommandContext<KeyBinding>
        {
            Command = Command.Activate, Source = new View (), Binding = new KeyBinding ([Command.Activate]) { Key = Key.Enter }
        };

        if (ctx is CommandContext<KeyBinding> { Binding.Key: { } key } keyCtx)
        {
            Assert.Equal (Key.Enter, key);
            Assert.Equal (Command.Activate, keyCtx.Command);
        }
        else
        {
            Assert.Fail ("Pattern matching should have succeeded");
        }
    }

    [Fact]
    public void PatternMatching_MouseBinding_WithMouseEvent_Works ()
    {
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonClicked) { Source = new View { Id = "mouseSource" } };
        mouseBinding.MouseEvent = new Mouse { Flags = MouseFlags.LeftButtonClicked, Position = new Point (10, 20) };

        ICommandContext? ctx = new CommandContext<MouseBinding> { Command = Command.Activate, Source = new View (), Binding = mouseBinding };

        // This is the actual pattern used in production code after rename
        if (ctx is CommandContext<MouseBinding> { Binding.MouseEvent: { } mouse } mouseCtx)
        {
            Assert.Equal (MouseFlags.LeftButtonClicked, mouse.Flags);
            Assert.Equal (new Point (10, 20), mouse.Position);
        }
        else
        {
            Assert.Fail ("Pattern matching should have succeeded");
        }
    }

    [Fact]
    public void PatternMatching_MouseBinding_NullMouseEvent_DoesNotMatch ()
    {
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonClicked)
        {
            MouseEvent = null // Explicitly set to null
        };

        ICommandContext? ctx = new CommandContext<MouseBinding> { Command = Command.Activate, Source = new View (), Binding = mouseBinding };

        // Pattern should NOT match when MouseEvent is null
        bool matched = ctx is CommandContext<MouseBinding> { Binding.MouseEvent: { } };

        Assert.False (matched);
    }

    [Fact]
    public void PatternMatching_DifferentBindingTypes_DoNotMatch ()
    {
        ICommandContext? ctx = new CommandContext<KeyBinding>
        {
            Command = Command.Activate, Source = new View (), Binding = new KeyBinding ([Command.Activate])
        };

        // KeyBinding context should not match MouseBinding pattern
        bool matchedMouse = ctx is CommandContext<MouseBinding>;

        Assert.False (matchedMouse);
    }

    #endregion

    #region Binding Source Property Tests

    [Fact]
    public void KeyBinding_Source_IsAccessibleThroughContext ()
    {
        View bindingSource = new () { Id = "bindingSource" };
        View contextSource = new () { Id = "contextSource" };

        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.A, Source = bindingSource };

        CommandContext<KeyBinding> ctx = new () { Command = Command.Activate, Source = contextSource, Binding = keyBinding };

        // Both sources are accessible
        Assert.Equal ("contextSource", ctx.Source?.Id);
        Assert.Equal ("bindingSource", ctx.Binding.Source?.Id);
    }

    [Fact]
    public void MouseBinding_Source_IsAccessibleThroughContext ()
    {
        View bindingSource = new () { Id = "bindingSource" };
        View contextSource = new () { Id = "contextSource" };

        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonClicked) { Source = bindingSource };

        CommandContext<MouseBinding> ctx = new () { Command = Command.Activate, Source = contextSource, Binding = mouseBinding };

        // Both sources are accessible
        Assert.Equal ("contextSource", ctx.Source?.Id);
        Assert.Equal ("bindingSource", ctx.Binding.Source?.Id);
    }

    #endregion

    #region Command Event Args Integration Tests

    [Fact]
    public void CommandEventArgs_Context_WithKeyBinding_Works ()
    {
        KeyBinding keyBinding = new ([Command.Accept]) { Key = Key.Enter, Source = new View { Id = "keySource" } };

        CommandContext<KeyBinding> ctx = new () { Command = Command.Accept, Source = new View { Id = "invoker" }, Binding = keyBinding };

        CommandEventArgs args = new () { Context = ctx };

        Assert.NotNull (args.Context);
        Assert.Equal (Command.Accept, args.Context.Command);

        if (args.Context is CommandContext<KeyBinding> { Binding.Key: { } key })
        {
            Assert.Equal (Key.Enter, key);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match from CommandEventArgs.Context");
        }
    }

    [Fact]
    public void CommandEventArgs_Context_WithMouseBinding_Works ()
    {
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.RightButtonClicked) { Source = new View { Id = "mouseSource" } };

        CommandContext<MouseBinding> ctx = new () { Command = Command.Activate, Source = new View { Id = "invoker" }, Binding = mouseBinding };

        CommandEventArgs args = new () { Context = ctx };

        Assert.NotNull (args.Context);

        if (args.Context is CommandContext<MouseBinding> { Binding.MouseEvent: { } mouse })
        {
            Assert.Equal (MouseFlags.RightButtonClicked, mouse.Flags);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match MouseBinding from CommandEventArgs.Context");
        }
    }

    #endregion
}
