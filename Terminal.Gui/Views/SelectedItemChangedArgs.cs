#nullable enable
namespace Terminal.Gui.Views;

/// <summary>Event arguments for the SelectedItemChanged event.</summary>
public class SelectedItemChangedArgs : EventArgs
{
    /// <summary>Initializes a new <see cref="SelectedItemChangedArgs"/> class.</summary>
    /// <param name="selectedItem"></param>
    /// <param name="previousSelectedItem"></param>
    public SelectedItemChangedArgs (int? selectedItem, int? previousSelectedItem)
    {
        PreviousSelectedItem = previousSelectedItem;
        SelectedItem = selectedItem;
    }

    /// <summary>Gets the index of the item that was previously selected. null if there was no previous selection.</summary>
    public int? PreviousSelectedItem { get; }

    /// <summary>Gets the index of the item that is now selected. null if there is no selection.</summary>
    public int? SelectedItem { get; }
}
