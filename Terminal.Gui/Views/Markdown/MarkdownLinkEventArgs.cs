namespace Terminal.Gui.Views;

public class MarkdownLinkEventArgs : EventArgs
{
    public MarkdownLinkEventArgs (string url)
    {
        Url = url;
    }

    public string Url { get; }
    public bool Handled { get; set; }
}
