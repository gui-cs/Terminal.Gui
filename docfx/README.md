This folder generates the API docs for Terminal.Gui. 

The API documentation is generated via a GitHub Action (`.github/workflows/api-docs.yml`) using [DocFX](https://github.com/dotnet/docfx). 

## To Generate the Docs Locally

0. Install DocFX: https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html
1. Run `./docfx/scripts/build.ps1`
2. Browse to http://localhost:8080 and verify everything looks good.
3. Hit Ctrl-C to stop the script.

## To Update `views.md`

0. Switch to the `./docfx` folder
1. Run `./scripts/generate-views-doc.ps1`
2. Commit the changes to `docs/views.md`

## API Documentation Overview

The API documentation for Terminal.Gui is a critical resource for developers, providing detailed information on classes, methods, properties, and events within the library. This documentation is hosted at [gui-cs.github.io/Terminal.Gui](https://gui-cs.github.io/Terminal.Gui) and includes both auto-generated API references and conceptual guides. For a broader overview of the Terminal.Gui project, including project structure and contribution guidelines, refer to the main [Terminal.Gui README](https://github.com/gui-cs/Terminal.Gui/blob/v2_develop/README.md).

### Scripts for Documentation Generation

The `scripts` folder contains PowerShell scripts to assist in generating and updating documentation:

- **`Build.ps1`**: Main build script that generates the documentation site locally. Running this script with DocFX installed will:
  1. Generate the AI agent source index (see below)
  2. Build the documentation site at `http://localhost:8080`

- **`Generate-SourceIndex.ps1`**: Generates Vercel-style file path indices for AI agent retrieval-led reasoning. This script:
  - Scans `./Terminal.Gui/` for `.cs` source files, grouped by directory
  - Scans `./docfx/docs/` for `.md` documentation files
  - Outputs to `.tg-docs/INDEX.md`
  - Injects the source index into `AGENTS.md` between markers

- **`generate-views-doc.ps1`**: Updates the `views.md` file in the `docs` directory. This script automates the process of documenting the various view classes in Terminal.Gui.

- **`OutputView/`**: Directory for storing intermediate files related to the documentation generation process.

These scripts streamline the process of maintaining up-to-date documentation, ensuring that contributors can easily generate and verify documentation changes locally before committing them.

## AI Agent Source Index

The `Generate-SourceIndex.ps1` script creates a file path index using the **Vercel-style format** for AI agent "retrieval-led reasoning". This approach is inspired by Vercel's work on optimizing LLM context for large codebases.

### Background

This implementation is based on Vercel's blog post: [How we compressed our JS SDK documentation by 80%](https://vercel.com/blog/how-we-compressed-our-js-sdk-docs-by-80-percent-for-llms)

The key insight is that instead of embedding full descriptions in the context (which can become stale), we point AI agents to actual source files. The agents then read the files they need using "retrieval-led reasoning" rather than relying on "pre-training-led reasoning" from potentially outdated embedded content.

### Format

```
[Index Name]|root: ./path/to/root
|IMPORTANT: Prefer retrieval-led reasoning over pre-training-led reasoning. Read files when needed.
|directory:{file1.cs,file2.cs,...}
|subdirectory:{file3.cs,file4.cs,...}
```

### Files Generated

| File | Description |
|------|-------------|
| `.tg-docs/INDEX.md` | Complete auto-generated index with both source and docs indices |
| `AGENTS.md` | Source index injected between `<!-- BEGIN AUTO-GENERATED-SOURCE-INDEX -->` and `<!-- END AUTO-GENERATED-SOURCE-INDEX -->` markers |

### Benefits

1. **Always in sync** - Index is regenerated on every build
2. **No manual maintenance** - File paths are discovered automatically
3. **Retrieval-led** - AI reads actual files, not stale summaries
4. **~80% smaller** - File paths only, no embedded content
