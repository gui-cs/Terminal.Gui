
namespace Terminal.Gui.ViewsTests;

public class FlagSelectorTests
{
    [Fact]
    public void Initialization_ShouldSetDefaults()
    {
        var flagSelector = new FlagSelector();

        Assert.True(flagSelector.CanFocus);
        Assert.Equal(Dim.Auto(DimAutoStyle.Content), flagSelector.Width);
        Assert.Equal(Dim.Auto(DimAutoStyle.Content), flagSelector.Height);
        Assert.Equal(Orientation.Vertical, flagSelector.Orientation);
    }

    [Fact]
    public void SetFlags_WithDictionary_ShouldSetFlags()
    {
        var flagSelector = new FlagSelector();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags(flags);

        Assert.Equal(flags, flagSelector.Flags);
    }

    [Fact]
    public void SetFlags_WithEnum_ShouldSetFlags()
    {
        var flagSelector = new FlagSelector();

        flagSelector.SetFlags<FlagSelectorStyles>();

        var expectedFlags = Enum.GetValues<FlagSelectorStyles>()
                                .ToDictionary(f => Convert.ToUInt32(f), f => f.ToString());

        Assert.Equal(expectedFlags, flagSelector.Flags);
    }

    [Fact]
    public void SetFlags_WithEnumAndCustomNames_ShouldSetFlags()
    {
        var flagSelector = new FlagSelector();

        flagSelector.SetFlags<FlagSelectorStyles>(f => f switch
        {
            FlagSelectorStyles.ShowNone => "Show None Value",
            FlagSelectorStyles.ShowValueEdit => "Show Value Editor",
            FlagSelectorStyles.All => "Everything",
            _ => f.ToString()
        });

        var expectedFlags = Enum.GetValues<FlagSelectorStyles>()
                                .ToDictionary(f => Convert.ToUInt32(f), f => f switch
                                {
                                    FlagSelectorStyles.ShowNone => "Show None Value",
                                    FlagSelectorStyles.ShowValueEdit => "Show Value Editor",
                                    FlagSelectorStyles.All => "Everything",
                                    _ => f.ToString()
                                });

        Assert.Equal(expectedFlags, flagSelector.Flags);
    }

    [Fact]
    public void Value_Set_ShouldUpdateCheckedState()
    {
        var flagSelector = new FlagSelector();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags(flags);
        flagSelector.Value = 1;

        var checkBox = flagSelector.SubViews.OfType<CheckBox>().First(cb => (uint)cb.Data == 1);
        Assert.Equal(CheckState.Checked, checkBox.CheckedState);

        checkBox = flagSelector.SubViews.OfType<CheckBox>().First(cb => (uint)cb.Data == 2);
        Assert.Equal(CheckState.UnChecked, checkBox.CheckedState);
    }

    [Fact]
    public void Styles_Set_ShouldCreateSubViews()
    {
        var flagSelector = new FlagSelector();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags(flags);
        flagSelector.Styles = FlagSelectorStyles.ShowNone;

        Assert.Contains(flagSelector.SubViews, sv => sv is CheckBox cb && cb.Title == "None");
    }

    [Fact]
    public void ValueChanged_Event_ShouldBeRaised()
    {
        var flagSelector = new FlagSelector();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags(flags);
        bool eventRaised = false;
        flagSelector.ValueChanged += (sender, args) => eventRaised = true;

        flagSelector.Value = 1;

        Assert.True(eventRaised);
    }
}
