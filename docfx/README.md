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

The API documentation for Terminal.Gui is a critical resource for developers, providing detailed information on classes, methods, properties, and events within the library. This documentation is hosted at [gui-cs.github.io/Terminal.Gui](https://gui-cs.github.io/Terminal.Gui) and includes both auto-generated API references and conceptual guides. For a broader overview of the Terminal.Gui project, including project structure and contribution guidelines, refer to the main [Terminal.Gui README](../Terminal.Gui/README.md).

### Scripts for Documentation Generation

The `scripts` folder contains PowerShell scripts to assist in generating and updating documentation:
- `build.ps1`: A script to build the documentation locally. Running this script with DocFX installed will generate the documentation site, which can be viewed at `http://localhost:8080`.
- `generate-views-doc.ps1`: A script specifically for updating the `views.md` file in the `docs` directory. This script automates the process of documenting the various view classes in Terminal.Gui, ensuring that the documentation remains current with the codebase.
- `OutputView/`: A directory likely used for storing output or intermediate files related to the documentation generation process.

These scripts streamline the process of maintaining up-to-date documentation, ensuring that contributors can easily generate and verify documentation changes locally before committing them.
