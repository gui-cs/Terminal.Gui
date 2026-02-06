---
uid: Terminal.Gui.Input
summary: The `Input` namespace provides comprehensive input handling for keyboard, mouse, and command processing.
---

@Terminal.Gui.Input contains the input processing system for Terminal.Gui applications, including keyboard event handling, mouse interaction, and the command execution framework. This namespace defines the core input primitives and event structures used throughout the framework.

The input system provides both low-level input events and high-level command abstractions, enabling applications to handle everything from basic key presses to complex gesture recognition and command routing.

## Key Components

- **Key**: Represents keyboard input with modifier support
- **MouseEventArgs**: Comprehensive mouse event information
- **Command**: Enumeration of standard application commands
- **KeyBinding**: Associates keys with commands
- **MouseBinding**: Associates mouse events with commands

## Example Usage

```csharp
// First, add command handlers
AddCommand(Command.Up, commandContext => Move(commandContext, -16));
AddCommand(Command.Down, commandContext => Move(commandContext, 16));
AddCommand(Command.Accept, HandleAcceptCommand);

// Then bind keys to commands
KeyBindings.Add(Key.CursorUp, Command.Up);
KeyBindings.Add(Key.CursorDown, Command.Down);
KeyBindings.Add(Key.Enter, Command.Accept);

// Then bind mouse events to commands
MouseBindings.Add(MouseFlags.Button1DoubleClicked, Command.Accept);
MouseBindings.Add(MouseFlags.WheeledDown, Command.ScrollDown);
MouseBindings.ReplaceCommands(MouseFlags.Button3Clicked, Command.Context);
```

## Deep Dives

- [Keyboard Input](~/docs/keyboard.md) - Comprehensive keyboard input handling
- [Mouse Input](~/docs/mouse.md) - Comprehensive mouse input handling
- [Commands](~/docs/command.md) - Command execution framework details

