# Summary of Cancellable Work Pattern Discussions in Terminal.Gui

The *[Cancellable Work Pattern](cancellable-work-pattern.md)* is a core design pattern in Terminal.Gui and is prevalent across various components of Terminal.Gui,

This document summarizes proposals for helpers that aim to reduce code duplication, ensure consistency, and simplify CWP adoption across Terminal.Gui components, aligning with the `v2_develop` implementation (`Command.Activate`, `Handled`).

## CWP Helper Classes

To streamline CWP implementation, a set of helper classes is proposed to encapsulate the pattern’s structure (workflow, notifications, cancellation, context, default behavior):

- **CWPPropertyHelper**: Manages property changes, such as `Orientation` in `OrientationHelper`, with pre- and post-change events. It supports scenarios requiring validation or notification of property updates, ensuring consistent handling of cancellation and context.
- **CWPWorkflowHelper**: Handles general workflows, such as `View.Command`’s `RaiseActivating` and `View.Scheme`’s `GetScheme`, supporting virtual methods, events, and default actions. It’s versatile for single-phase workflows with result or non-result outcomes.
- **CWPEventHelper**: Supports event-driven workflows, such as `Application.Keyboard`’s `OnKeyDown`, focusing on event-only scenarios with minimal overhead.

## CWPPropertyHelper

## CWPWorkflowHelper

## CWPEventHelper