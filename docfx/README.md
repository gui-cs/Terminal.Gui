This folder generates the API docs for Terminal.Gui. 

The API documentation is generated using the [DocFX tool](https://github.com/dotnet/docfx). The output of docfx gets put into the `./docs` folder which is then checked in. The `./docs` folder is then picked up by Github Pages and published to Miguel's Github Pages (https://migueldeicaza.github.io/gui.cs/).

## To Generate the Docs

1. Do a `Release` build on `master`. This will cause all `/// <inheritdoc/>` references to be updated.
2. Change in to the `docfx/` directory.
3. Type `docfx --metadata` to generate metadata
4. Type `docfx --serve` to generate the docs and start a local webserver for testing.

If `docfx` fails with a `Stackoverflow` error. Just run it again. And again. Sometimes it takes a few times. If that doesn't work, create a fresh clone or delete the `docfx/api`, `docfx/obj`, and `docs/` folders and run the steps above again.
