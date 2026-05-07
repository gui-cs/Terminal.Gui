using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Xunit;

namespace ViewsTests.Markdown;

public class TableHeightInvestigation
{
    [Fact]
    public void Recalculate_Height_Matches_RenderedHeight ()
    {
        // Copilot - investigation
        // Create table data similar to clet help
        TableData data = new (
            ["Alias", "Description", "Options"],
            [Alignment.Start, Alignment.Start, Alignment.Start],
            [
                ["select", "Presents a list of options and returns the text of the selected item.", "--options, args ..."],
                ["text", "Prompts for free-form text input and returns the entered string.", ""],
                ["multiline-text, mt", "Prompts for multi-line text input and returns the entered string.", ""],
                ["int", "Prompts for an integer value using a numeric spinner.", "--step"],
                ["decimal", "Prompts for a decimal value using a numeric spinner.", "--step"],
                ["confirm", "Prompts for a yes/no confirmation and returns a boolean", "--prompt"],
                ["date", "Prompts for a date and returns an ISO-8601 date string (YYYY-MM-DD).", ""],
                ["time", "Prompts for a time and returns an ISO-8601 time string (HH:MM:SS).", ""],
                ["duration", "Prompts for a duration and returns an ISO-8601 duration string (e.g. PT1H30M).", ""],
                ["color", "Prompts for a color and returns a hex string (@rrggbb).", ""],
                ["multi-select", "Presents a list of options with checkboxes and returns the selected texts.", "--options, args ..."],
                ["attribute-picker, attribute", "Prompts for text attributes (Foreground, Background, style) and returns a JSON object.", ""],
                ["pick-file, file", "Opens a file picker dialog and returns the selected file path(s).", "--multi, --root, --filter"],
                ["pick-directory, dir", "Opens a directory picker dialog and returns the selected directory path.", "--root"],
                ["linear-range, range", "Presents a LinearRange (single, multi, or bounded range) over a list of labelled options and returns the selection.", "--mode, --options, --orientation, --range-kind, --allow-empty, --hide-legends, args ..."],
                ["md, markdown", "Browse and render Markdown files with link navigation and syntax highlighting.", "--theme, --cat, --no-browse, args"],
                ["help", "Shows help for clet commands.", "args ..."]
            ]);

        MarkdownTable table = new () { TableData = data };

        // After initial Recalculate(80) from the setter
        int heightAt80 = table.RenderedHeight;

        // Now recalculate at width 99
        table.Recalculate (99);
        int heightAt99 = table.RenderedHeight;

        // Recalculate at width 100
        table.Recalculate (100);
        int heightAt100 = table.RenderedHeight;

        // Check Height property matches RenderedHeight
        Assert.Fail ($"H@80={heightAt80}, H@99={heightAt99}, H@100={heightAt100}, " +
                     $"HeightDim={table.Height}");
    }
}
