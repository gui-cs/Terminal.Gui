using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Collection ("Global Test Setup")]
public class AdornmentSubViewTests ()
{
    [Fact]
    public void Setting_Thickness_Causes_Adornment_SubView_Layout ()
    {
        var view = new View ();
        var subView = new View ();
        view.Margin.Add (subView);
        view.BeginInit ();
        view.EndInit ();
        var raised = false;

        subView.SubViewLayout += LayoutStarted;
        view.Margin.Thickness = new Thickness (1, 2, 3, 4);
        view.Layout ();
        Assert.True (raised);

        return;
        void LayoutStarted (object sender, LayoutEventArgs e)
        {
            raised = true;
        }
    }
}
