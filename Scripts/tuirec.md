# Recording Terminal.Gui Apps with `tuirec`

Use this guide when an issue or PR asks for a GIF/video capture of a Terminal.Gui app or scenario.

## Goals

- Record a **visible, stable** terminal session.
- Convert the recording to a GIF artifact for docs.
- Validate output before committing.

## Recommended Workflow

1. Build first.
   - To avoid recording startup/build noise, build before launching recording.
2. Launch the target app/scenario with a dedicated entry point.
   - To reduce timing and startup variability, prefer a scenario runner or focused runner over `dotnet run` of a large app shell.
3. Start `tuirec` recording and drive the UI interactions.
   - Keep the terminal size fixed for the whole recording.
   - Move slowly enough for frames to be readable.
4. Export a GIF from the recording.
5. Validate the output artifact before commit.
   - Confirm the GIF is not blank.
   - Confirm expected UI text appears.
   - Confirm the interaction sequence is complete.

## Practical Guidance for Agents

- To avoid blank captures, do not rely on first-run restore/build while recording.
- To keep captures deterministic, use a dedicated runner that goes directly to the target scenario.
- To verify quality, inspect the output and confirm key labels are visible before replacing docs assets.
- To update docs assets, write GIFs to `docfx/images/` with a descriptive name.

## Recording UICatalog Scenarios

(TBD - detailed instructions coming soon)

## Recording Individual View Sub-classes with EnableForDesign

(TBD - detailed instructions coming soon)

## Recording Standalone Example Apps

(TBD - detailed instructions coming soon)

## Validation Checklist

- [ ] The app content is visible (not empty/black).
- [ ] The expected scenario title/controls are readable.
- [ ] The capture shows the intended interaction.
- [ ] The final GIF path and filename are correct.

## Notes

- `tuirec` CLI options can change over time. To find current syntax, run `tuirec --help`.
- If a capture fails, retry with a dedicated runner and pre-built binaries before attempting other changes.
