# Terminal.Gui v2

Checkout this Discussion: https://github.com/gui-cs/Terminal.Gui/discussions/2448

* *Modern Look & Feel* - Apps built with Terminal.Gui now feel modern thanks to these improvements:
	* *TrueColor support* - 24-bit color support for Windows, Mac, and Linux. Legacy 16-color systems are still supported, automatically. See [TrueColor](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#truecolor) for details.
	* *User Configurable Color Themes* - See [Color Themes](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#color-themes) for details.
	* *Enhanced Unicode/Wide Character support *- Terminal.Gui now supports the full range of Unicode/wide characters. See [Unicode](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#unicode) for details.
	* *Line Canvas* - Terminal.Gui now supports a line canvas enabling high-performance drawing of lines and shapes using box-drawing glyphs. `LineCanvas` provides *auto join*, a smart TUI drawing system that automatically selects the correct line/box drawing glyphs for intersections making drawing complex shapes easy. See [Line Canvas](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#line-canvas) for details.
	* *Enhanced Borders and Padding* - Terminal.Gui now supports a `Border`, `Margin`, and `Padding` property on all views. This simplifies View development and enables a sophisticated look and feel. See [Padding](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#padding) for details.
	* *Modern File Dialog* - Terminal.Gui now supports a modern file dialog that includes icons (in TUI!) for files/folders, search, and a `TreeView``. See [FileDialog](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#filedialog) for details.
* *Configuration Manager* - Terminal.Gui now supports a configuration manager enabling library and app settings to be persisted and loaded from the file system. See [Configuration Manager](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#configuration-manager) for details.
* *Simplified API* - The entire library has been reviewed and simplified. As a result, the API is more consistent and uses modern .NET API standards (e.g. for events). This refactoring resulted in the removal of thousands of lines of code, better unit tests, and higher performance than v1. See [Simplified API](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#simplified-api) for details.

...