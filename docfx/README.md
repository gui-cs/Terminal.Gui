This folder generates the API docs for Terminal.Gui. 

The API documentation is generated via a GitHub Action (`.github/workflows/api-docs.yml`) using [DocFX](https://github.com/dotnet/docfx). The Action publishes the docs to the `gh-pages` branch, which gets published to https://gui-cs.github.io/Terminal.Gui/.

NOTE: the v2 are generated from another repository (https://github.com/gui-cs/Terminal.GuiV2Docs) and are published here: https://gui-cs.github.io/Terminal.GuiV2Docs/. 

## To Generate the Docs Locally

0. Install DotFX https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html
1. Change to the `./docfx` folder and run `./build.ps1`
2. Browse to http://localhost:8080 and verify everything looks good.
3. Hit ctrl-c to stop the script.


