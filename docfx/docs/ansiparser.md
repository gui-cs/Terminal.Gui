# AnsiResponseParser

## Background
Terminals send input to the running process as a stream of characters. In addition to regular characters ('a','b','c' etc), the terminal needs to be able to communicate more advanced concepts ('Alt+x', 'Mouse Moved', 'Terminal resized' etc). This is done through the use of 'terminal sequences'.

All terminal sequences start with Esc (`'\x1B'`) and are then followed by specific characters per the event to which they correspond. For example:

| Input Sequence  | Meaning                               |
|----------------|-------------------------------------------|
| `<Esc>[A`       | Up Arrow Key                              |
| `<Esc>[B`       | Down Arrow Key                            |
| `<Esc>[5~`      | Page Up Key                               |
| `<Esc>[6~`      | Page Down Key                             |

Most sequences begin with what is called a `CSI` which is just `\x1B[` (`<Esc>[`). But some terminals send older sequences such as:

| Input Sequence  | Meaning                               |
|----------------|-------------------------------------------|
| `<Esc>OR`     | F3 (SS3 Pattern)                         |
| `<Esc>OD`       | Left Arrow Key (SS3 Pattern)             |
| `<Esc>g`      | Alt+G (Escape as alt)                      |
| `<Esc>O`      |  Alt+Shift+O  (Escape as alt)|

When using the windows driver, this is mostly already dealt with automatically by the relevant native APIs (e.g. `ReadConsoleInputW` from kernel32).  In contrast the net driver operates in 'raw mode' and processes input almost exclusively using these sequences.

Regardless of the driver used, some escape codes will always be processed e.g. responses to Device Attributes Requests etc.

## Role of AnsiResponseParser

This class is responsible for filtering the input stream to distinguish between regular user key presses and terminal sequences. 

## Timing 
Timing is a critical component of interpreting the input stream. For example if the stream serves the escape (Esc), the parser must decide whether it's a standalone keypress or the start of a sequence. Similarly seeing the sequence `<Esc>O` could be Alt+Upper Case O, or the beginning of an SS3 pattern for F3 (were R to follow).

Because it is such a critical component, it is abstracted away from the parser itself. Instead the host class (e.g. InputProcessor) must decide when a suitable time has elapsed, after which the `Release` method should be invoked which will forcibly resolve any waiting state.

This can be controlled through the `_escTimeout` field. This approach is consistent with other terminal interfaces e.g. bash.

## State and Held

The parser has 3 states:

| State  | Meaning                               |
|----------------|-------------------------------------------|
| `Normal`     | We are encountering regular keys and letting them pass through unprocessed|
| `ExpectingEscapeSequence`       | We encountered an `Esc` and are holding it to see if a sequence follows |
| `InResponse`      | The `Esc` was followed by more keys that look like they will make a full terminal sequence (hold them all) |

Extensive trace logging is built into the implementation, to allow for reproducing corner cases and/or rare environments. See the logging article for more details on how to set this up.

