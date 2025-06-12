# Contributing to Terminal.Gui

We welcome contributions from the community. See [Issues](https://github.com/gui-cs/Terminal.Gui/issues) for a list of open [bugs](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Abug) and [enhancements](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Aenhancement). Contributors looking for something fun to work on should look at issues tagged as:

- [good first issue](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3A%22good+first+issue%22)
- [up for grabs](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs)
- [help wanted](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs)

## Forking and Submitting Changes

Terminal.Gui uses the [GitFlow](https://nvie.com/posts/a-successful-git-branching-model/) branching model. 

* The `v1_release` and `v2_release` branches are always stable, and always match the most recently released Nuget package.
* The `v1_develop` and `v2_develop` branches are where new development and bug-fixes happen. `v2_develop` is the default Github branch.

### Forking Terminal.Gui

1. Use GitHub to fork the `Terminal.Gui` repo to your account (https://github.com/gui-cs/Terminal.Gui/fork).

2. Clone your fork to your local machine

```
git clone https://github.com/<yourID>/Terminal.Gui
```

Now, your local repo will have an `origin` remote pointing to `https://github.com/<yourID>/Terminal.Gui`.

3. Add a remote for `upstream`: 
```
git remote add upstream https://github.com/gui-cs/Terminal.Gui
```
You now have your own fork and a local repo that references it as `origin`. Your local repo also now references the orignal Terminal.Gui repo as `upstream`. 

### Starting to Make a Change

Ensure your local `v1_develop` (for v1) or `v2_develop` (for v2) branch is up-to-date with `upstream` (`github.com/gui-cs/Terminal.Gui`):
```powershell
cd ./Terminal.Gui
git checkout v2_develop
git pull upstream v2_develop
```

Create a new local branch:
```powershell
git checkout -b my_new_branch
```

### Making Changes
Follow all the guidelines below.

* [Coding Style](#TerminalGui-Coding-Style)
* [Unit Tests](#Unit-Tests)
* [Sample Code](#Sample-Code)
* API Documentation
* etc...

When you're ready, commit your changes:

```powershell
git add .
git commit -m "Fixes #1234. Some bug"
```

### Submitting a Pull Request

1. Push your local branch to your fork (`origin`):

```powershell
git push --set-upstream origin my_new_branch
```

2. Create the Pull Request:

In the output of the `git push` command you'll see instructions with a link to the Pull Request:

```powershell
 $ git push --set-upstream origin my_new_branch
Enumerating objects: 8, done.
...
remote:
remote: Create a pull request for 'my_new_branch' on GitHub by visiting:
remote:      https://github.com/<yourID>/Terminal.Gui/pull/new/more_doc_fixes
remote:
...
```

3. Go to that URL and create the Pull Request:

(in Windows Terminal, just CTRL-Click on the URL)

Follow the template instructions found on Github.

## Tenets for [gui-cs](www.github.com/gui-cs) Code Style (Unless you have better ones)

* **Six-Year-Old Reading Level** - Our code style is biased towards code readability and away from terseness. This is *Systems Software* and needs to stand the test of time. Code should be structured and use variable names that make it readable by a 6-year-old, and comments in code are encouraged. 
* **Consistency, Consistency, Consistency** - We adopt and document our standards for code style and then enforce them ruthlessly. For example, we require code reviews to pay attention to code style, not just functionality. 
* **Don't be Weird** - Like all developers we have opinions, but our opinions on code style are tempered by existing standards. We are biased towards code style that used by Microsoft and other leading dotnet developers. For example, we choose 4 spaces for indentation instead of 8.
* **Set and Forget** - We embrace and encourage the use of technology that makes it easy for contributors to apply best-practice code-style, such as ReSharper. As we do so we are mindful that tools can cause hidden issues and merge hell.
* **Documentation is the Spec** - We care deeply about providing delightful developer documentation and are sticklers for grammar and clarity. If the code and the docs conflict, we are biased to believe what we wrote in the API documentation. This drives a virtuous cycle of clear thinking.

**Terminal.Gui** uses a derivative of the [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions), with any deviations from those (somewhat older) conventions codified in the .editorconfig for the solution, as well as even more specific definitions in team-shared dotsettings files, used by ReSharper and Rider.\
Before you commit code, please run the formatting rules on **only the code file(s) you have modified**, in one of the following ways, in order of most preferred to least preferred:

 1. `Ctrl-E-C` if using ReSharper or Rider
 2. Running the free [CleanupCode](https://www.jetbrains.com/help/resharper/CleanupCode.html) tool from JetBrains (this applies the same formatting rules as if you had used ReSharper or Rider, but is free for all users, if you don't have a license for those products)
     - Run at the command line, from the solution root directory, as: `cleanupcode.exe relative/path/to/your/file.cs`
 3. If you are unable to use either of those options, the last resort is to use `Ctrl-K-D` in Visual Studio (with default C# developer key bindings), to apply the subset of the formatting rules that Visual Studio can apply.

## User Experience Tenets

**Terminal.Gui**, as a UI framework, heavily influences how console graphical user interfaces (GUIs) work. We use the following [tenets](https://ceklog.kindel.com/2020/02/10/tenets/) to guide us:

*NOTE: Like all tenets, these are up for debate. If you disagree, have questions, or suggestions about these tenets and guidelines submit an Issue using the [design](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Adesign) tag.*

1. **Honor What's Come Before**. The Mac and Windows OS's have well-established GUI idioms that are mostly consistent. We adhere to these versus inventing new ways for users to do things. For example, **Terminal.Gui** adopts the `ctrl/command-c`, `ctrl/command-v`, and `ctrl/command-x` keyboard shortcuts for cut, copy, and paste versus defining new shortcuts.
2. **Consistency Matters**. Common UI idioms should be consistent across the GUI framework. For example, `ctrl/command-q` quits/exits all modal views. See [Issue #456](https://github.com/gui-cs/Terminal.Gui/issues/456) as a counter-example that should be fixed.
3. **Honor the OS, but Work Everywhere**. **Terminal.Gui** is cross-platform, but we support taking advantage of a platform's unique advantages. For example, the Windows Console API is richer than the Unix API in terms of keyboard handling. Thus, in Windows pressing the `alt` key in a **Terminal.Gui** app will activate the `MenuBar`, but in Unix, the user has to press the full hotkey (e.g. `alt-f`) or `F9`. 
4. **Keyboard first, Mouse also**. Users use consoles primarily with the keyboard; **Terminal.Gui** is optimized for getting stuff done without using the Mouse. However, as a GUI framework, the Mouse is essential thus we strive to ensure that everything also works via the Mouse.

## Public API Tenets & Guidelines

**Terminal.Gui** provides an API that is used by many. As the project evolves, contributors should follow these [tenets](https://ceklog.kindel.com/2020/02/10/tenets/) to ensure Consistency and backward compatibility.

*NOTE: Like all tenets, these are up for debate. If you disagree, have questions, or suggestions about these tenets and guidelines submit an Issue using the [design](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Adesign) tag.*

1. **Stand on the shoulders of giants.** Follow the [Microsoft .NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/) where appropriate. 
2. **Don't Break Existing Stuff.** Avoid breaking changes to user behavior or the public API; instead, figure out how to implement new functionality in a similar way. If a breaking change can't be avoided, follow the guidelines below.
3. **Fail-fast.** Fail-fast makes bugs and failures appear sooner, leading to a higher-quality framework and API.
4. **Standards Reduce Complexity**. We strive to adopt standard API idoms because doing so reduces complexity for users of the API. For example, see Tenet #1 above. A counterexample is [Issue #447](https://github.com/gui-cs/Terminal.Gui/issues/447).

### Include API Documentation

Great care has been provided thus far in ensuring **Terminal.Gui** has great [API Documentation](https://gui-cs.github.io/Terminal.Gui). Contributors have the responsibility of continuously improving the API Documentation.

- All public APIs must have clear, concise, and complete documentation in the form of [XML Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/).
- Keep the `<summary></summary>` terse.
- Use `<see cref=""/>` liberally to cross-link topics.
- Use `<remarks></remarks>` to add more context and explanation.
- For complex topics, provide conceptual documentation in the `docfx/articles` folder as a `.md` file. It will automatically get picked up and be added to [Conceptual Documentation](https://gui-cs.github.io/Terminal.Gui/docs/index.html).
- Use proper English and good grammar.

### Defining Events

See https://gui-cs.github.io/Terminal.Gui/docs/events.html


### Defining new `View` classes

- Support parameterless constructors (see [Issue 102](Parameterless constructors #102)). Do not require callers to use a parameterized constructor except when forcing `Absolute Layout`).
- Avoid doing initialization via constructors. Instead use a property so consumers can use object initialization (e.g. `var foo = new Foo() { a = b };`).
- Ensure the `UICatalog` demo for the new class illustrates both `Absolutle Layout` and `Computed Layout`.

## Breaking Changes to User Behavior or the Public API

- Tag all pull requests that cause breaking changes to user behavior or the public API with the [breaking-change](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Abreaking-change) tag. This will help project maintainers track and document these.
- Add a `<remark></remark>` to the XML Documentation to the code describing the breaking change. These will get picked up in the [API Documentation](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui.html).

## Unit Tests

PRs should never cause code coverage to go down. Ideally, every PR will get the project closer to 100%. PRs that include new functionality (e.g. a new control) should have at least 70% code coverage for the new functionality. 

**Terminal.Gui** has an automated unit or regression test suite. See the [Testing wiki](https://github.com/gui-cs/Terminal.Gui/wiki/Testing).

We analyze unit tests and code coverage on each PR push. 

The code coverage of the latest released build (on NuGet) is shown as a badge at the top of `README.md`. Here as well:

![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/migueldeicaza/90ef67a684cb71db1817921a970f8d27/raw/code-coverage.json)

The project uses Fine Code Coverage to allow easy access to code coverage info on a per-component basis.

Use the following command to generate the same CC info that the Publish Github Action uses to publish the results to the badge:

```
dotnet test --no-restore --verbosity normal --collect:"XPlat Code Coverage"  --settings UnitTests/coverlet.runsettings
```

Then open up the resulting `coverage.opencover.xml` file and you'll see the `sequenceCoverage` value:

```xml
<?xml version="1.0" encoding="utf-8"?>
<CoverageSession>
  <Summary numSequencePoints="15817" visitedSequencePoints="7249" numBranchPoints="9379" visitedBranchPoints="3640" sequenceCoverage="45.83" branchCoverage="38.81" maxCyclomaticComplexity="10276" minCyclomaticComplexity="10276" visitedClasses="105" numClasses="141" visitedMethods="965" numMethods="1751" />
 
```

## Sample Code

[UI Catalog](https://github.com/gui-cs/Terminal.Gui/tree/master/UICatalog) is a great sample app for manual testing.

When adding new functionality, fixing bugs, or changing things, please either add a new `Scenario` to **UICatalog** or update an existing `Scenario` to fully illustrate your work and provide a test-case.
