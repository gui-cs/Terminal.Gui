This folder generates the API docs for Terminal.Gui

## To Generate the Docs

1. Do a `Release` build on `master`. This will cause all `/// <inheritdoc/>` references to be updated.
2. Change in to the `docfx/` directory.
3. Type `docfx --metadata` to generate metadata
4. Type `docfx --serve` to generate the docs and start a local webserver for testing.

If `docfx` fails with a `Stackoverflow` error. Just run it again. And again. Sometimes it takes a few times. If that doesn't work, create a fresh clone or delete the `docfx/api`, `docfx/obj`, and `docs/` folders and run the steps above again.
