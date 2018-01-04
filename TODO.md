
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

# Focus

When SetFocus is called, it need to ensure that the chain up the views is
focused as well, something that we got for free in the old Container/Widget
model, but needs revisiting in the new model.

# Bugs

On the demo, press tab twice, instead of selecting Ok, the first tab
does nothing, the second tab clears the screen.

	=> Explanation: the Window gets a NeedsDisplay, so it displays
	   tiself, but the contentView does not have NeedsDisplay
	   set recursively, so it does not render any of the subviews

# Merge Responder into View

# Make HasFocus implicitly call SetNeedsDisplay
