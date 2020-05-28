# Contributing to Terminal.Gui

We welcome contributions from the community. See [Issues](https://github.com/migueldeicaza/gui.cs/issues) for a list of open [bugs](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Abug) and [enhancements](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Aenhancement). Contributors looking for something fun to work on should look at issues taged as:

- [good first issue](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3A%22good+first+issue%22)
- [up for grabs](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs)
- [help wanted](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs)

## Coding Style

**Terminal.Gui** follows the [Mono Coding Guidelines](https://www.mono-project.com/community/contributing/coding-guidelines/). `../.editorconfig` uses Visual Studio to help enforce these.

## User Experience Tenets

**Terminal.Gui**, as a UI framework, heavily influences how console graphical user interfaces (GUIs) work. We use the following [tenets](https://ceklog.kindel.com/2020/02/10/tenets/) to guide us:

*NOTE: Like all tenets, these are up for debate. If you disagree, have questions, or suggestions about these tenets and guideliens submit an Issue using the [design](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Adesign) tag.*

1. **Honor What's Come Before**. The Mac and Windows OS's have well established GUI idioms that are mostly consistent. We adhere to these versus inventing new ways for users to do things. For example, **Terminal.Gui** adopts the `ctrl/command-c`, `ctrl/command-v`, and `ctrl/command-x` keyboard shortcuts for cut, copy, and paste versus defining new shortcuts.
2. **Consistency Matters**. Common UI idioms should be consistent across the GUI framework. For example, `ctrl/command-q` quits/exits all modal views. See [Issue #456](https://github.com/migueldeicaza/gui.cs/issues/456) as a counter example that should be fixed.
3. **Honor the OS, but Work Everywhere**. **Terminal.Gui** is cross-platform, but we support taking advantage of a platform's unique advantages. For example the Windows Console API is richer than the Unix API in terms of keyboard handling. Thus, in Windows pressing the `alt` key in a **Terminal.Gui** app will activate the `MenuBar`, but in Unix the user has to press the full hotkey (e.g. `alt-f`) or `F9`. 
4. **Keyboard first, Mouse also**. Users use consoles primarily with the keyboard; **Terminal.Gui** is optimized for getting stuff done without using the mouse. However, as a GUI framework, the mouse is essential, thus we strive to ensure that everything also works via the mouse.

## Public API Tenets & Guidelines

**Terminal.Gui** provides an API that is used by many. As the project evolves contributors should follow these [tenets](https://ceklog.kindel.com/2020/02/10/tenets/) to ensure consistency and backwards compatabiltiy.

*NOTE: Like all tenets, these are up for debate. If you disagree, have questions, or suggestions about these tenets and guideliens submit an Issue using the [design](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Adesign) tag.*

1. **Stand on the shoulders of giants.** Follow the [Microsoft .NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/) where appropriate. 
2. **Don't Break Existing Stuff.** Avoid breaking changes to user behavior or the public API; instead, figure out how to implement new functionality in a parallel way. If a breaking change can't be avoided, follow the guidelines below.
3. **Fail-fast.** Fail-fast makes bugs and failures appear sooner, leading to a higher-quality framework and API.
4. **Standards Reduce Complexity**. We strive to adopt standard API idoms because doing so reduces complexity for users of the API. For example, see Tenet #1 above. A counter example is [Issue #447](https://github.com/migueldeicaza/gui.cs/issues/447).

## Breaking Changes to User Behavior or the Public API

- Tag all pull requests that cause breaking changes to user behavior or the public API with the [breaking-change](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Abreaking-change) tag. This will help project maintainers track and document these.
- Add a `<remark></remark>` to the XML Documentation to the code describing the breaking change. These will get picked up in the [API Documentation](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.html).

## Examples & Tests

**Terminal.Gui** does an automated unit or regression test suite ([please feel free to help build one](https://github.com/migueldeicaza/gui.cs/issues/566)). Until such a suite is available, the best we have is the [UICatalog](https://github.com/migueldeicaza/gui.cs/tree/master/UICatalog) sample app. 

When adding new functionality, fixing bugs, or changing things, please either add a new `Scenario` to **UICatalog** or update an existing `Scenario` to fully illustrate your work and provide a test-case.
