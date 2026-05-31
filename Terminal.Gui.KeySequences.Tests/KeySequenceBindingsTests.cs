namespace Terminal.Gui.KeySequences.Tests;

public class KeySequenceBindingsTests
{
    [Fact]
    public void ProcessKey_Matches_Count_Motion ()
    {
        KeySequenceBindings bindings = new ();
        int moveCount = 0;

        bindings.Add ("; m <count> k", context =>
        {
            moveCount = context.Count;
            return true;
        });

        View target = new ();

        Assert.Equal (KeySequenceResult.Started, bindings.ProcessKey (target, ';'));
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, 'm'));
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, '4'));
        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'k'));
        Assert.Equal (4, moveCount);
        Assert.False (bindings.IsCapturing);
    }

    [Fact]
    public void ProcessKey_Matches_Operator_Count_Motion ()
    {
        KeySequenceBindings bindings = new ();
        int deleteCount = 0;
        Key? operatorKey = null;
        Key? motionKey = null;

        bindings.Add ("; d <count> d", context =>
        {
            deleteCount = context.Count;
            operatorKey = context.OperatorKey;
            motionKey = context.MotionKey;
            return true;
        });

        View target = new ();

        bindings.ProcessKey (target, ';');
        bindings.ProcessKey (target, 'd');
        bindings.ProcessKey (target, '2');

        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'd'));
        Assert.Equal (2, deleteCount);
        Assert.Equal (Key.D, operatorKey);
        Assert.Equal (Key.D, motionKey);
    }

    [Fact]
    public void ProcessKey_Uses_Default_Count_When_Count_Is_Omitted ()
    {
        KeySequenceBindings bindings = new ();
        int moveCount = 0;

        bindings.Add ("; m <count> k", context =>
        {
            moveCount = context.Count;
            return true;
        });

        View target = new ();

        bindings.ProcessKey (target, ';');
        bindings.ProcessKey (target, 'm');

        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'k'));
        Assert.Equal (1, moveCount);
    }

    [Fact]
    public void ProcessKey_Cancels_With_Escape ()
    {
        KeySequenceBindings bindings = new ();
        bindings.Add ("; m <count> k", _ => true);

        View target = new ();

        bindings.ProcessKey (target, ';');

        Assert.Equal (KeySequenceResult.Canceled, bindings.ProcessKey (target, Key.Esc));
        Assert.False (bindings.IsCapturing);
    }

    [Fact]
    public void Add_Rejects_Duplicate_Pattern ()
    {
        KeySequenceBindings bindings = new ();
        bindings.Add ("; m <count> k", _ => true);

        Assert.Throws<InvalidOperationException> (() => bindings.Add ("; m <count> k", _ => true));
    }
}
