# ANSI Handling Deep Dive

Terminal.Gui's ANSI handling subsystem (`Terminal.Gui.Drivers.AnsiHandling`) provides comprehensive support for parsing and generating ANSI escape sequences. This includes keyboard input, mouse events, terminal queries/responses, and text formatting.

## Overview

When running in a terminal, input arrives as a stream of characters. Beyond regular characters ('a', 'b', 'c'), terminals communicate special input (arrow keys, function keys, mouse events, terminal responses) through **escape sequences** - character sequences beginning with `ESC` (`\x1B`).

The ANSI handling subsystem has two main responsibilities:

1. **Parsing** - Converting escape sequences from the terminal into Terminal.Gui events (`Key`, `Mouse`)
2. **Encoding** - Converting Terminal.Gui events back into escape sequences (primarily for testing)

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Input Stream (chars)                         │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      AnsiResponseParser                             │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ State Machine: Normal → ExpectingEscapeSequence → InResponse │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                 │                                   │
│         ┌───────────────────────┼───────────────────────┐          │
│         ▼                       ▼                       ▼          │
│  ┌──────────────┐     ┌─────────────────┐     ┌────────────────┐   │
│  │AnsiMouseParser│     │AnsiKeyboardParser│     │Expected Response│   │
│  └──────────────┘     └─────────────────┘     │   Matching     │   │
│         │                       │             └────────────────┘   │
│         ▼                       ▼                       │          │
│    Mouse Event             Key Event              Callback         │
└─────────────────────────────────────────────────────────────────────┘
```

## Core Components

### AnsiResponseParser

The central class that filters input streams, distinguishing regular keypresses from escape sequences.

**Two Variants:**
- `AnsiResponseParser` - Simple string-based processing
- `AnsiResponseParser<TInputRecord>` - Preserves metadata alongside characters (e.g., `ConsoleKeyInfo`)

**Parser States:**

| State | Description |
|-------|-------------|
| `Normal` | Processing regular characters, passing them through |
| `ExpectingEscapeSequence` | Encountered `ESC`, waiting to see if a sequence follows |
| `InResponse` | Building a complete escape sequence |

**State Transitions:**

```
Normal ──[ESC]──► ExpectingEscapeSequence ──[valid char]──► InResponse
   ▲                      │                                      │
   │                      │[ESC]                                 │[terminator]
   │                      ▼                                      │
   │              Release + restart                              │
   └─────────────────────────────────────────────────────────────┘
```

### Timing and Ambiguity

A critical challenge: when the parser sees `ESC`, is it:
- A standalone Escape keypress?
- The start of an escape sequence like `ESC[A` (cursor up)?
- The start of `ESC O P` (F1 in SS3 mode)?

The parser accumulates characters, and the **caller** (e.g., `InputProcessor`) decides when enough time has elapsed to call `Release()`, which resolves any pending state.

```csharp
// Parser holds ESC waiting for more input
// After timeout, caller invokes:
string? released = parser.Release();  // Forces resolution of held content
```

### IHeld Interface

Abstracts the storage of accumulated characters:

- `StringHeld` - Simple string accumulation for `AnsiResponseParser`
- `GenericHeld<T>` - Preserves metadata tuples for `AnsiResponseParser<T>`

## Keyboard Parsing

### AnsiKeyboardParser

Matches input against registered patterns and converts to `Key` objects.

**Pattern Priority:**
1. `Ss3Pattern` - SS3 sequences (e.g., `ESC O P` → F1)
2. `CsiKeyPattern` - CSI tilde sequences (e.g., `ESC[3~` → Delete)
3. `CsiCursorPattern` - CSI cursor/function sequences (e.g., `ESC[A` → CursorUp)
4. `EscAsAltPattern` - Escape-as-Alt (e.g., `ESC g` → Alt+G) *[last-minute only]*

### Pattern Types

#### SS3 Pattern (`Ss3Pattern`)
Legacy sequences for F1-F4 and navigation keys:

| Sequence | Key |
|----------|-----|
| `ESC O P` | F1 |
| `ESC O Q` | F2 |
| `ESC O R` | F3 |
| `ESC O S` | F4 |
| `ESC O D` | CursorLeft |
| `ESC O C` | CursorRight |
| `ESC O A` | CursorUp |
| `ESC O B` | CursorDown |
| `ESC O H` | Home |
| `ESC O F` | End |

#### CSI Key Pattern (`CsiKeyPattern`)
Function keys and editing keys with optional modifiers:

Format: `ESC [ <keycode> [; <modifier>] ~`

| Keycode | Key | Example |
|---------|-----|---------|
| 1 | Home | `ESC[1~` |
| 2 | Insert | `ESC[2~` |
| 3 | Delete | `ESC[3~` or `ESC[3;5~` (Ctrl+Delete) |
| 4 | End | `ESC[4~` |
| 5 | PageUp | `ESC[5~` |
| 6 | PageDown | `ESC[6~` |
| 11-15 | F1-F5 | `ESC[15~` (F5) |
| 17-21 | F6-F10 | `ESC[17~` (F6) |
| 23-24 | F11-F12 | `ESC[23~` (F11) |

**Modifier Codes:**
| Code | Modifier |
|------|----------|
| 2 | Shift |
| 3 | Alt |
| 4 | Shift+Alt |
| 5 | Ctrl |
| 6 | Ctrl+Shift |
| 7 | Ctrl+Alt |
| 8 | Ctrl+Shift+Alt |

#### CSI Cursor Pattern (`CsiCursorPattern`)
Arrow keys and navigation with optional modifiers:

Format: `ESC [ [1; <modifier>] <letter>`

| Letter | Key |
|--------|-----|
| A | CursorUp |
| B | CursorDown |
| C | CursorRight |
| D | CursorLeft |
| H | Home |
| F | End |
| P-S | F1-F4 |
| Z | Shift+Tab |

Example: `ESC[1;5A` = Ctrl+CursorUp

#### Escape-as-Alt Pattern (`EscAsAltPattern`)
Interprets `ESC` followed by a character as Alt+character.

Format: `ESC <char>`

- `ESC a` → Alt+A
- `ESC G` → Alt+Shift+G
- `ESC ^A` (Ctrl+A) → Ctrl+Alt+A

**Important:** This pattern is marked `IsLastMinute = true` because it conflicts with longer sequences. It's only applied during `Release()` when no other pattern matches.

## Mouse Parsing

### AnsiMouseParser

Parses SGR (1006) extended mouse format: `ESC[<button;x;y{M|m}`

- `M` = button press
- `m` = button release
- Coordinates are 1-based (converted to 0-based internally)

**Button Codes:**

| Code | Button | Notes |
|------|--------|-------|
| 0, 32 | Left | 32+ = motion with button |
| 1, 33 | Middle | |
| 2, 34 | Right | |
| 35 | None | Motion without button |
| 64 | WheelUp | |
| 65 | WheelDown | |
| 68 | WheelLeft | |
| 69 | WheelRight | |

**Modifier Offsets:**
- +8 = Alt
- +16 = Ctrl
- +4 = Shift (for some codes)

### Mouse Events Flow

```
Click:     Press(M) ─────────────────────────► Release(m)
Drag:      Press(M) ──► Motion(M,32+) ──► ... ──► Release(m)
Move:      Motion(M,35) ──► Motion(M,35) ──► ...
Scroll:    WheelUp(64) or WheelDown(65) [single event, no M/m]
```

## Expected Responses

The parser supports waiting for specific terminal responses (e.g., device attributes, cursor position).

### AnsiEscapeSequenceRequest

```csharp
var request = new AnsiEscapeSequenceRequest
{
    Request = "\x1B[0c",           // Send Device Attributes
    Terminator = "c",              // Response ends with 'c'
    ResponseReceived = response => HandleResponse(response),
    Abandoned = () => HandleTimeout()
};
```

### Expectation Types

1. **One-time expectations** - Removed after first match
2. **Persistent expectations** - Remain active for repeated events (e.g., continuous mouse tracking)
3. **Late responses** - Swallowed without callback when `StopExpecting()` was called

### AnsiRequestScheduler

Manages request throttling and queuing:
- Prevents duplicate requests with same terminator
- Throttles requests (100ms default) to avoid overwhelming the terminal
- Evicts stale requests (1s timeout) that never received responses

```csharp
var scheduler = new AnsiRequestScheduler(parser);
scheduler.SendOrSchedule(driver, request);  // Sends or queues
scheduler.RunSchedule(driver);              // Processes queued requests
```

## Encoding (Test Support)

### AnsiKeyboardEncoder

Converts `Key` objects to escape sequences for input injection:

```csharp
AnsiKeyboardEncoder.Encode(Key.CursorUp);           // Returns "ESC[A"
AnsiKeyboardEncoder.Encode(Key.CursorUp.WithCtrl);  // Returns "ESC[1;5A"
AnsiKeyboardEncoder.Encode(Key.A.WithAlt);          // Returns "ESC a"
```

### AnsiMouseEncoder

Converts `Mouse` events to SGR format:

```csharp
AnsiMouseEncoder.Encode(new Mouse 
{ 
    Flags = MouseFlags.LeftButtonPressed, 
    ScreenPosition = new Point(5, 10) 
});
// Returns "ESC[<0;6;11M" (1-based coordinates)
```

## EscSeqUtils

Static utility class providing ANSI sequence constants and helpers.

### Mouse Tracking Modes

```csharp
// Enable comprehensive mouse tracking
EscSeqUtils.CSI_EnableMouseEvents
// = CSI_EnableAnyEventMouse (1003) + CSI_EnableUrxvtExtModeMouse (1015) + CSI_EnableSgrExtModeMouse (1006)

// Disable all mouse tracking
EscSeqUtils.CSI_DisableMouseEvents
```

| Mode | Description |
|------|-------------|
| 1003 | Any-event tracking (motion with/without buttons) |
| 1006 | SGR format (decimal, unlimited coordinates) |
| 1015 | URXVT format (UTF-8 coordinates, legacy fallback) |

### Screen Buffer

```csharp
EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll   // ESC[?1049h
EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll // ESC[?1049l
EscSeqUtils.CSI_ClearScreen(ClearScreenOptions.EntireScreen)  // ESC[2J
```

### Known Terminators

`EscSeqUtils.KnownTerminators` contains valid ANSI response terminators for CSI sequences, used by the parser to detect sequence completion.

## Hyperlink Support (OSC 8)

### Osc8UrlLinker

Wraps URLs in text with OSC 8 hyperlink sequences:

```
ESC]8;;https://example.com\x07https://example.com ESC]8;;\x07
```

This enables clickable links in terminals that support OSC 8 (Windows Terminal, iTerm2, etc.).

## Debugging

Extensive trace logging is built into the implementation. Enable via the logging system to capture:
- State transitions
- Held content accumulation
- Pattern matching attempts
- Response matching

## See Also

- [Drivers Deep Dive](drivers.md) - How drivers use ANSI handling
- [Keyboard Deep Dive](keyboard.md) - Key event processing
- [Mouse Deep Dive](mouse.md) - Mouse event processing
- [Input Injection](input-injection.md) - Testing with ANSI encoding
