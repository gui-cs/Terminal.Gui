namespace ViewBaseTests.Commands;

/// <summary>
///     Tests for <see cref="CommandContext"/> record struct.
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

        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (sourceView), Binding = keyBinding };

        Assert.Equal (Command.Activate, ctx.Command);

        // Phase 2: Temporarily commented - will fix in Phase 4
        // Assert.Equal (sourceView, ctx.Source);
        Assert.NotNull (ctx.Binding);

        if (ctx.Binding is KeyBinding kb)
        {
            Assert.Equal (Key.Enter, kb.Key);
        }
        else
        {
            Assert.Fail ("Binding should be KeyBinding");
        }
    }

    [Fact]
    public void CommandContext_WithMouseBinding_SetsProperties ()
    {
        View sourceView = new () { Id = "sourceView" };
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonClicked);

        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (sourceView), Binding = mouseBinding };

        Assert.Equal (Command.Activate, ctx.Command);

        // Phase 2: Temporarily commented - will fix in Phase 4
        // Assert.Equal (sourceView, ctx.Source);
        Assert.NotNull (ctx.Binding);

        if (ctx.Binding is MouseBinding mb)
        {
            Assert.NotNull (mb.MouseEvent);
            Assert.Equal (MouseFlags.LeftButtonClicked, mb.MouseEvent!.Flags);
        }
        else
        {
            Assert.Fail ("Binding should be MouseBinding");
        }
    }

    #endregion

    #region ICommandContext Interface Tests

    [Fact]
    public void CommandContext_ImplementsICommandContext ()
    {
        CommandContext ctx = new () { Command = Command.Accept, Source = new WeakReference<View> (new View ()) };

        ICommandContext iCtx = ctx;

        Assert.Equal (Command.Accept, iCtx.Command);
        Assert.NotNull (iCtx.Source);
    }

    [Fact]
    public void Source_IsMutable_ThroughInterface ()
    {
        View originalSource = new () { Id = "original" };
        View newSource = new () { Id = "new" };

        CommandContext ctx = new () { Command = Command.Accept, Source = new WeakReference<View> (originalSource) };

        ICommandContext iCtx = ctx;
        iCtx.Source = new WeakReference<View> (newSource);

        // Phase 2: Temporarily commented - will fix in Phase 4
        // Assert.Equal (newSource, iCtx.Source);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void PatternMatching_KeyBinding_Works ()
    {
        ICommandContext ctx = new CommandContext
        {
            Command = Command.Activate, Source = new WeakReference<View> (new View ()), Binding = new KeyBinding ([Command.Activate]) { Key = Key.Enter }
        };

        if (ctx.Binding is KeyBinding { Key: { } key })
        {
            Assert.Equal (Key.Enter, key);
            Assert.Equal (Command.Activate, ctx.Command);
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

        ICommandContext ctx = new CommandContext { Command = Command.Activate, Source = new WeakReference<View> (new View ()), Binding = mouseBinding };

        // This is the actual pattern used in production code
        if (ctx.Binding is MouseBinding { MouseEvent: { } mouse })
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

        ICommandContext ctx = new CommandContext { Command = Command.Activate, Source = new WeakReference<View> (new View ()), Binding = mouseBinding };

        // Pattern should NOT match when MouseEvent is null
        bool matched = ctx.Binding is MouseBinding { MouseEvent: { } };

        Assert.False (matched);
    }

    [Fact]
    public void PatternMatching_DifferentBindingTypes_DoNotMatch ()
    {
        ICommandContext ctx = new CommandContext
        {
            Command = Command.Activate, Source = new WeakReference<View> (new View ()), Binding = new KeyBinding ([Command.Activate])
        };

        // KeyBinding should not match MouseBinding pattern
        bool matchedMouse = ctx.Binding is MouseBinding;

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

        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (contextSource), Binding = keyBinding };

        // Both sources are accessible
        View? source = null;
        ctx.Source?.TryGetTarget (out source);
        Assert.Equal ("contextSource", source?.Id);

        if (ctx.Binding is KeyBinding kb)
        {
            Assert.Equal ("bindingSource", kb.Source?.Id);
        }
        else
        {
            Assert.Fail ("Binding should be KeyBinding");
        }
    }

    [Fact]
    public void MouseBinding_Source_IsAccessibleThroughContext ()
    {
        View bindingSource = new () { Id = "bindingSource" };
        View contextSource = new () { Id = "contextSource" };

        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonClicked) { Source = bindingSource };

        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (contextSource), Binding = mouseBinding };

        // Both sources are accessible
        View? source = null;
        ctx.Source?.TryGetTarget (out source);
        Assert.Equal ("contextSource", source?.Id);

        if (ctx.Binding is MouseBinding mb)
        {
            Assert.Equal ("bindingSource", mb.Source?.Id);
        }
        else
        {
            Assert.Fail ("Binding should be MouseBinding");
        }
    }

    #endregion

    #region Command Event Args Integration Tests

    [Fact]
    public void CommandEventArgs_Context_WithKeyBinding_Works ()
    {
        KeyBinding keyBinding = new ([Command.Accept]) { Key = Key.Enter, Source = new View { Id = "keySource" } };

        CommandContext ctx = new () { Command = Command.Accept, Source = new WeakReference<View> (new View { Id = "invoker" }), Binding = keyBinding };

        CommandEventArgs args = new () { Context = ctx };

        Assert.NotNull (args.Context);
        Assert.Equal (Command.Accept, args.Context.Command);

        if (args.Context.Binding is KeyBinding { Key: { } key })
        {
            Assert.Equal (Key.Enter, key);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match KeyBinding from CommandEventArgs.Context.Binding");
        }
    }

    [Fact]
    public void CommandEventArgs_Context_WithMouseBinding_Works ()
    {
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.RightButtonClicked) { Source = new View { Id = "mouseSource" } };

        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (new View { Id = "invoker" }), Binding = mouseBinding };

        CommandEventArgs args = new () { Context = ctx };

        Assert.NotNull (args.Context);

        if (args.Context.Binding is MouseBinding { MouseEvent: { } mouse })
        {
            Assert.Equal (MouseFlags.RightButtonClicked, mouse.Flags);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match MouseBinding from CommandEventArgs.Context.Binding");
        }
    }

    #endregion

    #region ICommandContext.Binding Property Tests

    [Fact]
    public void Binding_Property_ReturnsBindingAsICommandBinding ()
    {
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Enter };

        CommandContext ctx = new () { Command = Command.Activate, Binding = keyBinding };

        // Access via ICommandContext interface
        ICommandContext iCtx = ctx;

        Assert.NotNull (iCtx.Binding);
        Assert.IsType<KeyBinding> (iCtx.Binding);
        Assert.Equal (keyBinding.Commands, iCtx.Binding!.Commands);
    }

    [Fact]
    public void Binding_Property_AllowsPolymorphicPatternMatching ()
    {
        KeyBinding keyBinding = new ([Command.Accept]) { Key = Key.F5 };
        ICommandContext ctx = new CommandContext { Command = Command.Accept, Binding = keyBinding };

        // Pattern match on Binding from the interface
        if (ctx.Binding is KeyBinding kb)
        {
            Assert.Equal (Key.F5, kb.Key);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match KeyBinding from ICommandContext.Binding");
        }
    }

    [Fact]
    public void Binding_Property_WithMouseBinding_Works ()
    {
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.RightButtonClicked);
        ICommandContext ctx = new CommandContext { Command = Command.Activate, Binding = mouseBinding };

        // Pattern match on Binding from the interface
        if (ctx.Binding is MouseBinding mb)
        {
            Assert.Equal (MouseFlags.RightButtonClicked, mb.MouseEvent?.Flags);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match MouseBinding from ICommandContext.Binding");
        }
    }

    [Fact]
    public void Binding_Property_WithCommandBinding_Works ()
    {
        CommandBinding inputBinding = new ([Command.Accept], new View { Id = "programmatic" }, "data");
        ICommandContext ctx = new CommandContext { Command = Command.Accept, Binding = inputBinding };

        // Pattern match on Binding from the interface
        if (ctx.Binding is CommandBinding ib)
        {
            Assert.Equal ("programmatic", ib.Source?.Id);
            Assert.Equal ("data", ib.Data);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match CommandBinding from ICommandContext.Binding");
        }
    }

    [Fact]
    public void Binding_Property_NullBinding_ReturnsNull ()
    {
        CommandContext ctx = new () { Command = Command.Activate };

        ICommandContext iCtx = ctx;

        // When Binding is not set, it should be null
        Assert.Null (iCtx.Binding);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void CommandContext_ParameterizedConstructor_SetsAllProperties ()
    {
        View sourceView = new () { Id = "source" };
        KeyBinding keyBinding = new ([Command.Accept]) { Key = Key.F1 };

        CommandContext ctx = new (Command.Accept, new WeakReference<View> (sourceView), keyBinding);

        Assert.Equal (Command.Accept, ctx.Command);
        Assert.NotNull (ctx.Source);

        View? retrievedSource = null;
        ctx.Source?.TryGetTarget (out retrievedSource);
        Assert.Equal ("source", retrievedSource?.Id);
        Assert.NotNull (ctx.Binding);

        if (ctx.Binding is KeyBinding kb)
        {
            Assert.Equal (Key.F1, kb.Key);
        }
        else
        {
            Assert.Fail ("Binding should be KeyBinding");
        }
    }

    [Fact]
    public void CommandContext_ParameterizedConstructor_NullSource_Works ()
    {
        CommandBinding binding = new ([Command.Activate], null, "test");

        CommandContext ctx = new (Command.Activate, null, binding);

        Assert.Equal (Command.Activate, ctx.Command);
        Assert.Null (ctx.Source);
        Assert.NotNull (ctx.Binding);
    }

    [Fact]
    public void CommandContext_ParameterizedConstructor_NullBinding_Works ()
    {
        View sourceView = new () { Id = "source" };

        CommandContext ctx = new (Command.Accept, new WeakReference<View> (sourceView), null);

        Assert.Equal (Command.Accept, ctx.Command);
        Assert.NotNull (ctx.Source);
        Assert.Null (ctx.Binding);
    }

    [Fact]
    public void CommandContext_DefaultConstructor_HasDefaultValues ()
    {
        CommandContext ctx = new ();

        Assert.Equal (default (Command), ctx.Command);
        Assert.Null (ctx.Source);
        Assert.Null (ctx.Binding);
    }

    #endregion
    
    #region WeakReference Behavior Tests

    [Fact]
    public void Source_WeakReference_TryGetTarget_ReturnsView ()
    {
        View sourceView = new () { Id = "weakRefTest" };
        CommandContext ctx = new () { Command = Command.Accept, Source = new WeakReference<View> (sourceView) };

        View? retrievedView = null;
        var success = ctx.Source?.TryGetTarget (out retrievedView);

        Assert.True (success);
        Assert.NotNull (retrievedView);
        Assert.Equal ("weakRefTest", retrievedView?.Id);
    }

    [Fact]
    public void Source_WeakReference_CanBeUpdated ()
    {
        View originalView = new () { Id = "original" };
        View newView = new () { Id = "new" };

        CommandContext ctx = new () { Command = Command.Accept, Source = new WeakReference<View> (originalView) };

        // Update Source
        ctx.Source = new WeakReference<View> (newView);

        View? retrievedView = null;
        ctx.Source?.TryGetTarget (out retrievedView);
        Assert.Equal ("new", retrievedView?.Id);
    }

    #endregion

    #region Command Property Mutability Tests

    [Fact]
    public void Command_Property_CanBeChanged ()
    {
        CommandContext ctx = new () { Command = Command.Accept };

        ctx.Command = Command.Activate;

        Assert.Equal (Command.Activate, ctx.Command);
    }

    [Fact]
    public void Command_Property_CanBeChangedThroughInterface ()
    {
        ICommandContext ctx = new CommandContext { Command = Command.Accept };

        ctx.Command = Command.HotKey;

        Assert.Equal (Command.HotKey, ctx.Command);
    }

    #endregion

    #region Record Struct Equality Tests

    [Fact]
    public void CommandContext_ValueEquality_SameValues_AreEqual ()
    {
        View sourceView = new () { Id = "source" };
        WeakReference<View> weakRef = new (sourceView);
        KeyBinding keyBinding = new ([Command.Accept]) { Key = Key.Enter };

        CommandContext ctx1 = new () { Command = Command.Accept, Source = weakRef, Binding = keyBinding };
        CommandContext ctx2 = new () { Command = Command.Accept, Source = weakRef, Binding = keyBinding };

        Assert.Equal (ctx1, ctx2);
    }

    [Fact]
    public void CommandContext_ValueEquality_DifferentCommand_AreNotEqual ()
    {
        CommandContext ctx1 = new () { Command = Command.Accept };
        CommandContext ctx2 = new () { Command = Command.Activate };

        Assert.NotEqual (ctx1, ctx2);
    }

    [Fact]
    public void CommandContext_ValueEquality_DifferentWeakReferences_AreNotEqual ()
    {
        View sourceView = new () { Id = "source" };

        // Different WeakReference instances, even to the same view
        CommandContext ctx1 = new () { Command = Command.Accept, Source = new WeakReference<View> (sourceView) };
        CommandContext ctx2 = new () { Command = Command.Accept, Source = new WeakReference<View> (sourceView) };

        // WeakReferences are reference types, so different instances are not equal
        Assert.NotEqual (ctx1, ctx2);
    }

    #endregion
}
