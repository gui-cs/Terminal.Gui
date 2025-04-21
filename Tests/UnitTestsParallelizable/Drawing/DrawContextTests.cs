namespace Terminal.Gui.DrawingTests;

public class DrawContextTests
{
    [Fact (Skip = "Region Union is broken")]
    public void AddDrawnRectangle_Unions ()
    {
        DrawContext drawContext = new DrawContext ();

        drawContext.AddDrawnRectangle (new (0, 0, 1, 1));
        drawContext.AddDrawnRectangle (new (1, 0, 1, 1));

        Assert.Equal (new Rectangle (0, 0, 2, 1), drawContext.GetDrawnRegion ().GetBounds ());
        Assert.Equal (2, drawContext.GetDrawnRegion ().GetRectangles ().Length);

        drawContext.AddDrawnRectangle (new (0, 0, 4, 1));
        Assert.Equal (new Rectangle (0, 1, 4, 1), drawContext.GetDrawnRegion ().GetBounds ());
        Assert.Single (drawContext.GetDrawnRegion ().GetRectangles ());
    }
}