#nullable enable
namespace Terminal.Gui;

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

        public override void OnDrawContent (Rectangle viewport)
        {
            if (!_autoComplete.LastPopupPos.HasValue)
            {
                return;
            }

            _autoComplete.RenderOverlay (_autoComplete.LastPopupPos.Value);
        }

        protected override bool OnMouseEvent (MouseEventArgs mouseEvent) { return _autoComplete.OnMouseEvent (mouseEvent); }
    }
}
