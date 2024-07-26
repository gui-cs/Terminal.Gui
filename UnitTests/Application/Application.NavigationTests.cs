using Moq;
using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests;

public class ApplicationNavigationTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void GetDeepestFocusedSubview_ShouldReturnNull_WhenViewIsNull ()
    {
        // Act
        var result = ApplicationNavigation.GetDeepestFocusedSubview (null);

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void GetDeepestFocusedSubview_ShouldReturnSameView_WhenNoSubviewsHaveFocus ()
    {
        // Arrange
        var view = new View () { Id = "view", CanFocus = true };;

        // Act
        var result = ApplicationNavigation.GetDeepestFocusedSubview (view);

        // Assert
        Assert.Equal (view, result);
    }

    [Fact]
    public void GetDeepestFocusedSubview_ShouldReturnFocusedSubview ()
    {
        // Arrange
        var parentView = new View () { Id = "parentView", CanFocus = true };;
        var childView1 = new View () { Id = "childView1", CanFocus = true };;
        var childView2 = new View () { Id = "childView2", CanFocus = true };;
        var grandChildView = new View () { Id = "grandChildView", CanFocus = true };;

        parentView.Add (childView1, childView2);
        childView2.Add (grandChildView);

        grandChildView.SetFocus ();

        // Act
        var result = ApplicationNavigation.GetDeepestFocusedSubview (parentView);

        // Assert
        Assert.Equal (grandChildView, result);
    }

    [Fact]
    public void GetDeepestFocusedSubview_ShouldReturnDeepestFocusedSubview ()
    {
        // Arrange
        var parentView = new View () { Id = "parentView", CanFocus = true };;
        var childView1 = new View () { Id = "childView1", CanFocus = true };;
        var childView2 = new View () { Id = "childView2", CanFocus = true };;
        var grandChildView = new View () { Id = "grandChildView", CanFocus = true };;
        var greatGrandChildView = new View () { Id = "greatGrandChildView", CanFocus = true };;

        parentView.Add (childView1, childView2);
        childView2.Add (grandChildView);
        grandChildView.Add (greatGrandChildView);

        grandChildView.SetFocus();

        // Act
        var result = ApplicationNavigation.GetDeepestFocusedSubview (parentView);

        // Assert
        Assert.Equal (greatGrandChildView, result);

        // Arrange
        greatGrandChildView.CanFocus = false;
        grandChildView.SetFocus ();

        // Act
        result = ApplicationNavigation.GetDeepestFocusedSubview (parentView);

        // Assert
        Assert.Equal (grandChildView, result);
    }
}
