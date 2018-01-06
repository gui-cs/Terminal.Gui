
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

## Layout manager

Unclear what to do about that right now.

# Unicode

Needs to move to `ustring` from `NStack.Core` to get full Unicode support.

Should get NStack.Core to move `ustring` to `System`.

# Merge Responder into View

For now it is split, in case we want to introduce formal view controllers.  But the design becomes very ugly.

# Bugs

There is a problem with the high-intensity colors, they are not showing up

# Mouse support

It is still pending.

Should allow for views to be dragged, in particular Window should allow this



