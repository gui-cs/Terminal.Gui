namespace Terminal.Gui;

public partial struct Point
{
    public static implicit operator System.Drawing.Point (Terminal.Gui.Point tgp) => new (tgp.X, tgp.Y);
    public static implicit operator Point (System.Drawing.Point sdp) => new (sdp.X, sdp.Y);
    public static bool operator != (Point left, System.Drawing.Point right) => new System.Drawing.Point (left.X,left.Y) != right;
    public static bool operator == (Point left, System.Drawing.Point right) => new System.Drawing.Point (left.X,left.Y) == right;
    public static bool operator != (System.Drawing.Point left, Point right) => left != new System.Drawing.Point(right.X,right.Y);
    public static bool operator == (System.Drawing.Point left, Point right) => left == new System.Drawing.Point(right.X,right.Y);
}
