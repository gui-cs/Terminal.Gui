using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui.ConsoleDrivers;
class WindowsDriverKeyPairer
{
    private InputRecord? _heldDownEvent = null; // To hold the "down" event

    // Process a single input record at a time
    public IEnumerable<InputRecord []> ProcessInput (InputRecord record)
    {
        // If it's a "down" event, store it as a held event
        if (IsKeyDown (record))
        {
            return HandleKeyDown (record);
        }
        // If it's an "up" event, try to match it with the held "down" event
        else if (IsKeyUp (record))
        {
            return HandleKeyUp (record);
        }
        else
        {
            // If it's not a key event, just pass it through
            return new [] { new [] { record } };
        }
    }

    private IEnumerable<InputRecord []> HandleKeyDown (InputRecord record)
    {
        // If we already have a held "down" event, release it (unmatched)
        if (_heldDownEvent != null)
        {
            // Release the previous "down" event since there's a new "down"
            var previousDown = _heldDownEvent.Value;
            _heldDownEvent = record; // Hold the new "down" event
            return new [] { new [] { previousDown } };
        }

        // Hold the new "down" event
        _heldDownEvent = record;
        return Enumerable.Empty<InputRecord []> ();
    }

    private IEnumerable<InputRecord []> HandleKeyUp (InputRecord record)
    {
        // If we have a held "down" event that matches this "up" event, release both
        if (_heldDownEvent != null && IsMatchingKey (record, _heldDownEvent.Value))
        {
            var downEvent = _heldDownEvent.Value;
            _heldDownEvent = null; // Clear the held event
            return new [] { new [] { downEvent, record } };
        }
        else
        {
            // No match, release the "up" event by itself
            return new [] { new [] { record } };
        }
    }

    private bool IsKeyDown (InputRecord record)
    {
        return record.KeyEvent.bKeyDown;
    }

    private bool IsKeyUp (InputRecord record)
    {
        return !record.KeyEvent.bKeyDown;
    }

    private bool IsMatchingKey (InputRecord upEvent, InputRecord downEvent)
    {
        return upEvent.KeyEvent.UnicodeChar == downEvent.KeyEvent.UnicodeChar;
    }
}
