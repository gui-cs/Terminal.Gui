using Xunit.Abstractions;
using static Terminal.Gui.Dim;

namespace Terminal.Gui.LayoutTests;

public class DimCombineTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    [Fact]
    public void DimCombine_Calculate_ReturnsCorrectValue ()
    {
        var dim1 = new DimAbsolute (10);
        var dim2 = new DimAbsolute (20);
        var dim = dim1 + dim2;
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (30, result);
    }

}
