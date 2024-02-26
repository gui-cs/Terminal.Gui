namespace Terminal.Gui;

public partial struct Size
{
    public static implicit operator Size (System.Drawing.Size sds) => new (sds.Width, sds.Height);
    public static implicit operator System.Drawing.Size (Size tgs) => new (tgs.Width, tgs.Height);
    public static bool operator != (Size left, System.Drawing.Size right) => new System.Drawing.Size (left.Width,left.Height) != right;
    public static bool operator == (Size left, System.Drawing.Size right) => new System.Drawing.Size (left.Width,left.Height) == right;
    public static bool operator != (System.Drawing.Size left, Size right) => left != new System.Drawing.Size(right.Width,right.Height);
    public static bool operator == (System.Drawing.Size left, Size right) => left == new System.Drawing.Size(right.Width,right.Height);
}
