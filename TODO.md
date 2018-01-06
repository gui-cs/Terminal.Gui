
# Things missing

## Color System

Topics to debate.

Given that we need pairs of foreground/background to be set when
operating on a view, should we surface the values independently, or
should we surface the attribute?

Currently views hardcode the colors to Colors.Base.SOmething for
example, perhaps these should be set with styles instead, or even
inheriting them.

The reason why the Colors definition is useful is because it comes with
defaults that work for both color and black and white and other limited
terminals.  Setting foreground/background independently tends to break
the black and white scenarios.

## Color and Dialogs

Perhaps dialog containers need to set a general style for the child widgets,
so that when we set a dialog, or error box, all the children added get the
right set of default colors.

Should include another theme, like the TurboPascal 6 theme

Replaces `Colors.Base.Normal` with `Attributes.Normal`, and perhaps attributes
points to the container.

## Views

Checkbox, ListView, Menu.

Wanted:
- Function Bar
- ScrollView
- Multi-line text editing
- Radio buttons
- DateTime widgets
- Shell/Process?
- Submenus in menus.
- Popup menus
- Make windows draggable


## Layout manager

Unclear what to do about that right now.  Perhaps use Flex?

# Unicode

Needs to move to `ustring` from `NStack.Core` to get full Unicode support.

The reason for ustring is that we need proper measuring of characters,
as we need to mirror what curses is showing it is a lot easier to go
with ustring/rune than to manually add support for surrogate
characters everywhere


# Merge Responder into View

For now it is split, in case we want to introduce formal view
controllers.  But the design becomes very ugly.

# Bugs

There is a problem with the high-intensity colors, they are not showing up

Add resizing support (still needs layout as well)

# Mouse support

It is still pending.

Should allow for views to be dragged, in particular Window should allow this

# Mono-Curses

The only missing feature in Mono-Curses that still relies on a native library
is to fetch the OS SIGTSTP signal, we could hardcode this value if we had
a way of detecting the host operating system and architecture, and just hardcode
the value based on this.