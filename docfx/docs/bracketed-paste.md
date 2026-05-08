# Bracketed Paste

Terminal.Gui supports the ANSI [bracketed paste mode](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-Functions-using-CSI-_-ordered-by-the-final-character-s) (`DECSET 2004`). When the terminal has bracketed paste enabled, the terminal wraps clipboard pastes with the marker pair `ESC[200~` and `ESC[201~`. This lets the application distinguish a paste from typed input — useful for performance (a 10 KB paste arrives as one event instead of 10,000 keystrokes) and for security (the application does not have to interpret pasted text as commands or shortcuts).

Bracketed paste mode is enabled automatically by the driver when the application initializes and disabled on shutdown. Applications do not need to opt in.

## Receiving paste events

Subscribe to <xref:Terminal.Gui.App.IApplication.Paste> to receive paste payloads at the application level:

```csharp
app.Paste += (sender, args) =>
              {
                  myLog.AppendLine ($"Pasted {args.Text.Length} characters");
                  // Set args.Handled = true to stop further dispatch.
              };
```

The <xref:Terminal.Gui.Input.PasteEventArgs.Text> property contains the raw text the terminal delivered between the start and end markers. The bracketing markers themselves are stripped by the parser.

If the application event is not handled, the paste is dispatched to the focused view via <xref:Terminal.Gui.ViewBase.View.NewPasteEvent(Terminal.Gui.Input.PasteEventArgs)>. Views can override <xref:Terminal.Gui.ViewBase.View.OnPasted(Terminal.Gui.Input.PasteEventArgs)> to provide default paste handling, or subscribe to the <xref:Terminal.Gui.ViewBase.View.Pasted> event. Unhandled pastes bubble up to the SuperView.

## Default behavior in TextField / TextView

`TextField` is a single-line control. When it receives a paste it takes the first line only (splitting on `\r` or `\n`) and strips C0 / C1 control characters except tab. This matches the behavior of the existing clipboard-paste command.

`TextView` is multi-line. It accepts the full payload, normalizing `\r` and `\r\n` line breaks to `\n`, and strips C0 / C1 control characters except tab and newline. This mirrors [Windows Terminal's `FilterStringForPaste`](https://github.com/microsoft/terminal/blob/main/src/types/utils.cpp).

To pass the raw payload through unmodified, subscribe to <xref:Terminal.Gui.ViewBase.View.Pasted> instead of relying on the default `OnPasted` — the event is raised after the virtual method, so a subscriber that sets `Handled = true` after the default insertion will only stop further bubbling, not undo the insertion. To override the default, subclass the view and override `OnPasted`.

## Terminals without bracketed paste support

On terminals that do not support bracketed paste mode (older versions of `xterm` without the feature compiled in, or terminals where the user has explicitly disabled it), the markers never arrive. The paste flows through as ordinary key events and the `Paste` event never fires. No probing or fallback is needed.

## Stranded pastes

If the terminal sends `ESC[200~` but the matching `ESC[201~` is dropped (broken connection, terminated remote shell), the parser would otherwise hold the buffered paste content forever. Terminal.Gui flushes the partial buffer as a `Paste` event after a 5-second idle timeout so input flow resumes. There is also a hard cap of 1 MiB on the paste buffer size; payloads exceeding it are truncated.

## Security considerations

Terminal.Gui trusts the *terminal* to sanitize the paste payload. Terminals such as xterm, Windows Terminal, Alacritty, and kitty already strip dangerous control sequences from the clipboard before bracketing — see [`xterm` `allowPasteControls`](https://invisible-island.net/xterm/xterm-paste64.html), [Windows Terminal `FilterStringForPaste`](https://github.com/microsoft/terminal/blob/main/src/types/utils.cpp), and [Alacritty's pre-bracket filter](https://github.com/alacritty/alacritty/blob/master/alacritty/src/event.rs).

For defense in depth, Terminal.Gui's default `OnPasted` implementations on `TextField` and `TextView` strip C0 / C1 control characters before inserting. Applications that consume the raw `Application.Paste` event are responsible for their own sanitization.

## Disabling bracketed paste

To opt out, intercept the driver setup and skip the enable sequence. The relevant constant is <xref:Terminal.Gui.Drivers.EscSeqUtils.CSI_EnableBracketedPaste>. There is no built-in application-level toggle; if you have a need for one, file a feature request.

## Related

- <xref:Terminal.Gui.App.IApplication.Paste> — application-level event
- <xref:Terminal.Gui.ViewBase.View.Pasted> — view-level event
- <xref:Terminal.Gui.ViewBase.View.OnPasted(Terminal.Gui.Input.PasteEventArgs)> — view virtual method
- <xref:Terminal.Gui.Input.PasteEventArgs> — event arguments
- <xref:Terminal.Gui.Drivers.EscSeqUtils.CSI_EnableBracketedPaste> / <xref:Terminal.Gui.Drivers.EscSeqUtils.CSI_DisableBracketedPaste> — driver-level constants
