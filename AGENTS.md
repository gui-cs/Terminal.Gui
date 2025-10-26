# Terminal.Gui - AI Agent Instructions

> **📘 For complete contributor guidelines (humans and AI agents), see [CONTRIBUTING.md](CONTRIBUTING.md).**

This repository uses [CONTRIBUTING.md](CONTRIBUTING.md) as the single source of truth for code style, testing, CI/CD, and contribution workflow. GitHub Copilot and other AI coding agents should also refer to [.github/copilot-instructions.md](.github/copilot-instructions.md) for a curated summary of non-negotiable rules.

**Key highlights for AI agents:**
- Always use explicit types (no `var` except for built-in simple types)
- Always use target-typed `new()` syntax
- Add new tests to `Tests/UnitTestsParallelizable/` when possible
- Never decrease code coverage
- Follow `.editorconfig` and `Terminal.sln.DotSettings` for formatting

See [CONTRIBUTING.md](CONTRIBUTING.md) for complete details.
