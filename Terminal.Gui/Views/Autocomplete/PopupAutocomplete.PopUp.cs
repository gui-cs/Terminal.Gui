#nullable enable

namespace Terminal.Gui.Views;

public abstract partial class PopupAutocomplete
{
    private sealed class Popup : View
    {
        public Popup (PopupAutocomplete autoComplete)
        {
            _autoComplete = autoComplete;
            CanFocus = true;
            TabStop = TabBehavior.NoStop;
            WantMousePositionReports = true;
        }

        private readonly PopupAutocomplete _autoComplete;

        protected override bool OnDrawingContent ()
        {
            if (!_autoComplete.LastPopupPos.HasValue)
            {
                return true;
            }

            _autoComplete.RenderOverlay (_autoComplete.LastPopupPos.Value);

            return true;
        }

        protected override bool OnMouseEvent (MouseEventArgs mouseEvent) { return _autoComplete.OnMouseEvent (mouseEvent); }
    }
}
