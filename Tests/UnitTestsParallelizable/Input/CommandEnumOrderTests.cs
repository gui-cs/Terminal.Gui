// Copilot

namespace InputTests;

/// <summary>
///     Verifies that <see cref="Command.StartSelection"/> and <see cref="Command.StartRectangleSelection"/>
///     are appended at the tail of the enum (after existing members) and do not renumber existing entries.
/// </summary>
public class CommandEnumOrderTests
{
    [Fact]
    public void StartSelection_IsAfter_InsertCaretBelow ()
    {
        // StartSelection must have a higher ordinal than all pre-existing members
        // to avoid renumbering. InsertCaretBelow was the last member before this PR.
        Assert.True ((int)Command.StartSelection > (int)Command.InsertCaretBelow);
    }

    [Fact]
    public void StartRectangleSelection_IsAfter_StartSelection ()
    {
        Assert.True ((int)Command.StartRectangleSelection > (int)Command.StartSelection);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void ImageViewCommands_AreAppendedAfterExistingCommands ()
    {
        Assert.True ((int)Command.Home > (int)Command.StartRectangleSelection);
        Assert.True ((int)Command.Center > (int)Command.Home);
        Assert.True ((int)Command.ZoomIn > (int)Command.Center);
        Assert.True ((int)Command.ZoomOut > (int)Command.ZoomIn);
    }

    // Copilot - GPT-5.5
    [Fact]
    public void End_Ordinal_IsUnchanged ()
    {
        Assert.Equal (15, (int)Command.End);
    }

    [Fact]
    public void KillWordRight_Ordinal_IsUnchanged ()
    {
        // KillWordRight was immediately after ToggleExtend in the Selection region.
        // If StartSelection/StartRectangleSelection were inserted before it, its
        // ordinal would shift. This test guards against that.
        Assert.True ((int)Command.KillWordRight > (int)Command.ToggleExtend);

        // KillWordRight should immediately follow ToggleExtend (no gap)
        Assert.Equal ((int)Command.ToggleExtend + 1, (int)Command.KillWordRight);
    }
}
