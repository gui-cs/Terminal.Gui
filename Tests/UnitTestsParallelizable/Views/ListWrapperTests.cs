using System.Collections.ObjectModel;

// Copilot

namespace ViewsTests;

public class ListWrapperTests
{
    [Fact]
    public void AspectGetter_Null_UsesToString ()
    {
        ObservableCollection<int> source = [1, 22, 333];
        ListWrapper<int> wrapper = new (source);

        Assert.Equal (3, wrapper.MaxItemLength); // "333".Length
    }

    [Fact]
    public void AspectGetter_WhenSet_UsesDelegate ()
    {
        ObservableCollection<int> source = [1, 22, 333];
        ListWrapper<int> wrapper = new (source)
        {
            AspectGetter = n => $"item-{n}"
        };

        // "item-333" = 8 chars
        Assert.Equal (8, wrapper.MaxItemLength);
    }

    [Fact]
    public void AspectGetter_WhenSet_UpdatesMaxItemLength ()
    {
        ObservableCollection<int> source = [1, 22, 333];
        ListWrapper<int> wrapper = new (source);

        int before = wrapper.MaxItemLength; // "333" = 3

        wrapper.AspectGetter = n => n.ToString ("D6"); // "000333" = 6

        Assert.Equal (3, before);
        Assert.Equal (6, wrapper.MaxItemLength);
    }

    [Fact]
    public void AspectGetter_ClearedToNull_RecalculatesWithToString ()
    {
        ObservableCollection<int> source = [1, 22, 333];
        ListWrapper<int> wrapper = new (source)
        {
            AspectGetter = n => n.ToString ("D6") // "000333" = 6
        };

        wrapper.AspectGetter = null;

        Assert.Equal (3, wrapper.MaxItemLength); // back to "333" = 3
    }
}
