
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

Replaces `Colors.Base.Normal` with `Attributes.Normal`, and perhaps attributes
points to the container.

## Views

Wanted:
- HotLabels (should be labelsw ith a hotkey that take a focus view as an argument)
- Shell/Process?
- Submenus in menus.
- Make windows draggable
- View + Attribute for SolidFills?

Should Views support Padding/Margin/Border?   Would make it simpler for Forms backend and perhaps
adopt the Forms CSS as-is

## Layout manager

Unclear what to do about that right now.  Perhaps use Flex?

Will at least need the protocol for sizing 

# Merge Responder into View

For now it is split, in case we want to introduce formal view
controllers.  But the design becomes very ugly.

