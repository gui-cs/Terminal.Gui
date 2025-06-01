This folder generates the API docs for Terminal.Gui. 

The API documentation is generated via a GitHub Action (`.github/workflows/api-docs.yml`) using [DocFX](https://github.com/dotnet/docfx). 

## To Generate the Docs Locally

0. Install DotFX https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html
1. Run `./docfx/scripts/Build.ps1`
2. Browse to http://localhost:8080 and verify everything looks good.
3. Hit ctrl-c to stop the script.

## To update `views.md`

0. Switch to the `./docfx` folder
1. Run `./scripts/generate-views-doc.ps1`
2. Commit the changes to `docs/views.md`
