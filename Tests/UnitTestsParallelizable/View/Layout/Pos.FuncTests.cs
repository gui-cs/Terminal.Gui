using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class PosFuncTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void PosFunc_Equal ()
    {
        Func<int> f1 = () => 0;
        Func<int> f2 = () => 0;

        Pos pos1 = Pos.Func (f1);
        Pos pos2 = Pos.Func (f1);
        Assert.Equal (pos1, pos2);

        f2 = () => 1;
        pos2 = Pos.Func (f2);
        Assert.NotEqual (pos1, pos2);
    }

    [Fact]
    public void PosFunc_SetsValue ()
    {
        var text = "Test";
        Pos pos = Pos.Func (() => text.Length);
        Assert.Equal ("PosFunc(4)", pos.ToString ());

        text = "New Test";
        Assert.Equal ("PosFunc(8)", pos.ToString ());

        text = "";
        Assert.Equal ("PosFunc(0)", pos.ToString ());
    }

    [Fact]
    public void PosFunc_Calculate_ReturnsCorrectValue ()
    {
        var pos = new PosFunc (() => 10);
        int result = pos.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (10, result);
    }
}
