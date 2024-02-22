#nullable enable
namespace Terminal.Gui;

public abstract partial class PopupAutocomplete
{
    private sealed class Popup : View
    {
        private readonly PopupAutocomplete _autoComplete;

        public Popup (PopupAutocomplete autoComplete)
        {
            this._autoComplete = autoComplete;
            CanFocus = true;
            WantMousePositionReports = true;
        }

        public override bool MouseEvent (MouseEvent mouseEvent) { return _autoComplete.MouseEvent (mouseEvent); }

        public override void OnDrawContent (Rectangle contentArea)
        {
            if (!_autoComplete.LastPopupPos.HasValue)
            {
                return;
            }

            _autoComplete.RenderOverlay (_autoComplete.LastPopupPos.Value);
        }
    }
}
