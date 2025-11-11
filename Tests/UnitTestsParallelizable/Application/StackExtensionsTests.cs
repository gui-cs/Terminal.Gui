using UnitTests;

namespace UnitTests_Parallelizable.ApplicationTests;

public class StackExtensionsTests : FakeDriverBase
{
    [Fact]
    public void Stack_topLevels_Contains ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();
        var comparer = new ToplevelEqualityComparer ();

        Assert.True (topLevels.Contains (new Window { Id = "w2" }, comparer));
        Assert.False (topLevels.Contains (new Toplevel { Id = "top2" }, comparer));
    }

    [Fact]
    public void Stack_topLevels_CreatetopLevels ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();

        int index = topLevels.Count - 1;

        foreach (Toplevel top in topLevels)
        {
            if (top.GetType () == typeof (Toplevel))
            {
                Assert.Equal ("Top", top.Id);
            }
            else
            {
                Assert.Equal ($"w{index}", top.Id);
            }

            index--;
        }

        Toplevel [] tops = topLevels.ToArray ();

        Assert.Equal ("w4", tops [0].Id);
        Assert.Equal ("w3", tops [1].Id);
        Assert.Equal ("w2", tops [2].Id);
        Assert.Equal ("w1", tops [3].Id);
        Assert.Equal ("Top", tops [^1].Id);
    }

    [Fact]
    public void Stack_topLevels_FindDuplicates ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();
        var comparer = new ToplevelEqualityComparer ();

        topLevels.Push (new Toplevel { Id = "w4" });
        topLevels.Push (new Toplevel { Id = "w1" });

        Toplevel [] dup = topLevels.FindDuplicates (comparer).ToArray ();

        Assert.Equal ("w4", dup [0].Id);
        Assert.Equal ("w1", dup [^1].Id);
    }

    [Fact]
    public void Stack_topLevels_MoveNext ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();

        topLevels.MoveNext ();

        Toplevel [] tops = topLevels.ToArray ();

        Assert.Equal ("w3", tops [0].Id);
        Assert.Equal ("w2", tops [1].Id);
        Assert.Equal ("w1", tops [2].Id);
        Assert.Equal ("Top", tops [3].Id);
        Assert.Equal ("w4", tops [^1].Id);
    }

    [Fact]
    public void Stack_topLevels_MovePrevious ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();

        topLevels.MovePrevious ();

        Toplevel [] tops = topLevels.ToArray ();

        Assert.Equal ("Top", tops [0].Id);
        Assert.Equal ("w4", tops [1].Id);
        Assert.Equal ("w3", tops [2].Id);
        Assert.Equal ("w2", tops [3].Id);
        Assert.Equal ("w1", tops [^1].Id);
    }

    [Fact]
    public void Stack_topLevels_MoveTo ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();

        var valueToMove = new Window { Id = "w1" };
        var comparer = new ToplevelEqualityComparer ();

        topLevels.MoveTo (valueToMove, 1, comparer);

        Toplevel [] tops = topLevels.ToArray ();

        Assert.Equal ("w4", tops [0].Id);
        Assert.Equal ("w1", tops [1].Id);
        Assert.Equal ("w3", tops [2].Id);
        Assert.Equal ("w2", tops [3].Id);
        Assert.Equal ("Top", tops [^1].Id);
    }

    [Fact]
    public void Stack_topLevels_MoveTo_From_Last_To_Top ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();

        var valueToMove = new Window { Id = "Top" };
        var comparer = new ToplevelEqualityComparer ();

        topLevels.MoveTo (valueToMove, 0, comparer);

        Toplevel [] tops = topLevels.ToArray ();

        Assert.Equal ("Top", tops [0].Id);
        Assert.Equal ("w4", tops [1].Id);
        Assert.Equal ("w3", tops [2].Id);
        Assert.Equal ("w2", tops [3].Id);
        Assert.Equal ("w1", tops [^1].Id);
    }

    [Fact]
    public void Stack_topLevels_Replace ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();

        var valueToReplace = new Window { Id = "w1" };
        var valueToReplaceWith = new Window { Id = "new" };
        var comparer = new ToplevelEqualityComparer ();

        topLevels.Replace (valueToReplace, valueToReplaceWith, comparer);

        Toplevel [] tops = topLevels.ToArray ();

        Assert.Equal ("w4", tops [0].Id);
        Assert.Equal ("w3", tops [1].Id);
        Assert.Equal ("w2", tops [2].Id);
        Assert.Equal ("new", tops [3].Id);
        Assert.Equal ("Top", tops [^1].Id);
    }

    [Fact]
    public void Stack_topLevels_Swap ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();

        var valueToSwapFrom = new Window { Id = "w3" };
        var valueToSwapTo = new Window { Id = "w1" };
        var comparer = new ToplevelEqualityComparer ();
        topLevels.Swap (valueToSwapFrom, valueToSwapTo, comparer);

        Toplevel [] tops = topLevels.ToArray ();

        Assert.Equal ("w4", tops [0].Id);
        Assert.Equal ("w1", tops [1].Id);
        Assert.Equal ("w2", tops [2].Id);
        Assert.Equal ("w3", tops [3].Id);
        Assert.Equal ("Top", tops [^1].Id);
    }

    [Fact]
    public void ToplevelEqualityComparer_GetHashCode ()
    {
        Stack<Toplevel> topLevels = CreatetopLevels ();

        // Only allows unique keys
        HashSet<int> hCodes = new ();

        foreach (Toplevel top in topLevels)
        {
            Assert.True (hCodes.Add (top.GetHashCode ()));
        }
    }

    private Stack<Toplevel> CreatetopLevels ()
    {
        Stack<Toplevel> topLevels = new ();

        topLevels.Push (new Toplevel { Id = "Top" });
        topLevels.Push (new Window { Id = "w1" });
        topLevels.Push (new Window { Id = "w2" });
        topLevels.Push (new Window { Id = "w3" });
        topLevels.Push (new Window { Id = "w4" });

        return topLevels;
    }
}
