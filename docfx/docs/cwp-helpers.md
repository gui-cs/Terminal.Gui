# Summary of Cancellable Work Pattern Discussions in Terminal.Gui

This document summarizes discussions on the *Cancellable Work Pattern* (CWP) in Terminal.Gui, covering its role, proposed helper classes, integration with the `Scheme` system, enhancements to the `Command` system, and documentation efforts. It reflects the `v2_develop` branch state, focusing on `Command.Activate`, `Handled`, and the proposed `PropagatedCommands` property, as preserved in the **"CWPHelpers_Properties_2025-05-16"** bookmark.

## Cancellable Work Pattern Overview

The CWP is a core design pattern in Terminal.Gui, structuring workflows to execute default actions, allow modification by external code or subclasses, or be cancelled. It prioritizes events for loose coupling, supplemented by virtual methods, and is used across components like scheme management, rendering, input handling, command execution, and property changes. The pattern achieves:
- **Default Execution**: Predictable behavior unless interrupted.
- **Modification**: Customization without deep implementation knowledge.
- **Cancellation**: Control to halt phases or workflows.
- **Decoupling**: Event-driven interactions to reduce inheritance dependencies.

## CWP Helper Classes

To streamline CWP implementation, a set of helper classes was proposed to encapsulate the pattern’s structure (workflow, notifications, cancellation, context, default behavior):
- **CWPPropertyHelper**: Manages property changes, such as `Orientation` in `OrientationHelper`, with pre- and post-change events. It supports scenarios requiring validation or notification of property updates, ensuring consistent handling of cancellation and context.
- **CWPWorkflowHelper**: Handles general workflows, such as `View.Command`’s `RaiseActivating` and `View.Scheme`’s `GetScheme`, supporting virtual methods, events, and default actions. It’s versatile for single-phase workflows with result or non-result outcomes.
- **CWPEventHelper**: Supports event-driven workflows, such as `Application.Keyboard`’s `OnKeyDown`, focusing on event-only scenarios with minimal overhead.

These helpers aim to reduce code duplication, ensure consistency, and simplify CWP adoption across Terminal.Gui components, aligning with the `v2_develop` implementation (`Command.Activate`, `Handled`).

## Scheme System Integration

The `Scheme` system, which maps `VisualRole`s (e.g., `Normal`, `Focus`) to `Attribute`s, uses the CWP for visual appearance customization. The `GetScheme` and `GetAttributeForRole` methods employ the pattern to resolve schemes and attributes, supporting:
- **Inheritance**: Views inherit schemes from superviews, falling back to `SchemeManager`’s `Base` scheme.
- **Explicit Assignment**: Views can set schemes via `Scheme` or `SchemeName` properties.
- **Event-Driven Customization**: `GettingScheme` and `GettingAttributeForRole` events allow superviews or external code to modify or cancel resolution, ensuring flexibility.

This integration enables dynamic styling (e.g., `Dialog` modifying subview schemes) while maintaining decoupling, as detailed in the `View.Scheme` section of `cancellable_work_pattern.md`.

## Command System Enhancements

The `Command` system, responsible for actions like state changes (`Command.Activate`) and confirmations (`Command.Accept`), has been updated in `v2_develop`:
- **Rename**: `Command.Select` was renamed to `Command.Activate` to clarify its role in state changes (e.g., `CheckBox` toggling) and preparatory actions (e.g., `MenuItemv2` focus).
- **Handled**: `CommandEventArgs` uses `Handled` instead of `Cancel` (#3913), indicating command completion.
- **Propagation**: `Command.Activate` is local, limiting hierarchical coordination (e.g., `MenuBarv2` popovers), while `Command.Accept` propagates to default buttons or superviews with view-specific hacks (e.g., `Menuv2`’s `SuperMenuItem`).

A proposed `PropagatedCommands` property (Issue #4050) addresses these limitations by:
- Allowing superviews to opt-in to specific command propagations (e.g., `Command.Activate` for `MenuBarv2`, `Command.Accept` for `Dialog`).
- Defaulting to `[Command.Accept]` to preserve existing behavior.
- Ensuring decoupling via superview-defined command lists, avoiding noise from irrelevant commands.

## Documentation Efforts

Documentation has been a key focus to clarify the CWP and `Command` system:
- **cancellable_work_pattern.md**: Details the CWP’s structure, goals, and implementation across `View.Scheme`, `View.Draw`, `View.Keyboard`, `View.Command`, `Application.Keyboard`, and `OrientationHelper`. The `View.Scheme` section was added to highlight its CWP usage, and examples demonstrate the pattern’s versatility.
- **command_deep_dive.md**: Explores the `Command` system’s implementation, evaluating `Activating`/`Accepting` events, propagation challenges, and the `PropagatedCommands` proposal. It includes recommendations like fixing `FlagSelector`’s event conflation.
- **Issue #4050 Post**: Proposes `PropagatedCommands`, documenting the `Command.Activate` rename, `Handled` change, and propagation enhancements, with actionable TODOs for implementation.

These documents are preserved in the Gist at [https://gist.github.com/tig/2b5c0ec25f0737d5f5665799f9d0cf5b](https://gist.github.com/tig/2b5c0ec25f0737d5f5665799f9d0cf5b), with ongoing updates to reflect new helper classes and proposals.

## Challenges and Recommendations
1. **FlagSelector Conflation**: The `CheckBox.Activating` handler in `FlagSelector` incorrectly triggers `Accepting`, mixing state changes and confirmations. Refactoring to separate these events is recommended.
2. **Propagation Limitations**: Local `Command.Activate` and `Command.Accept` hacks hinder hierarchical coordination. The `PropagatedCommands` property is proposed to standardize propagation.
3. **Documentation Gaps**: The CWP’s phases and `Handled` semantics need clearer documentation to guide developers, particularly for complex workflows like `View.Draw`.
4. **Helper Adoption**: Proposed helpers (`CWPPropertyHelper`, `CWPWorkflowHelper`, `CWPEventHelper`) need validation to ensure they cover all use cases without adding complexity.

## Conclusion
The *Cancellable Work Pattern* is integral to Terminal.Gui, enabling flexible, decoupled workflows across scheme management, rendering, input, commands, and properties. Proposed helper classes streamline its implementation, particularly for property changes like `Orientation`. The `Scheme` system leverages the CWP for dynamic styling, while the `Command` system’s updates (`Command.Activate`, `Handled`, `PropagatedCommands`) enhance clarity and coordination. Ongoing documentation efforts ensure these improvements are well-communicated, supporting robust UI development in `v2_develop`.