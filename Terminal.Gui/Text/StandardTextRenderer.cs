#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui.Text;

/// <summary>
///     Standard implementation of <see cref="ITextRenderer"/> that renders formatted text to the console.
/// </summary>
public class StandardTextRenderer : ITextRenderer
{
    /// <inheritdoc />
    public void Draw(
        FormattedText formattedText,
        Rectangle screen,
        Attribute normalColor,
        Attribute hotColor,
        bool fillRemaining = false,
        Rectangle maximum = default,
        IConsoleDriver? driver = null)
    {
        if (driver is null)
        {
            driver = Application.Driver;
        }

        if (driver is null || formattedText.Lines.Count == 0)
        {
            return;
        }

        driver.SetAttribute(normalColor);

        // Calculate effective drawing area
        Rectangle maxScreen = CalculateMaxScreen(screen, maximum);

        if (maxScreen.Width == 0 || maxScreen.Height == 0)
        {
            return;
        }

        // TODO: Implement alignment using the Aligner engine instead of custom logic
        // For now, use simplified alignment
        
        int startY = screen.Y;
        int lineIndex = 0;

        foreach (var line in formattedText.Lines)
        {
            if (lineIndex >= maxScreen.Height)
            {
                break;
            }

            int y = startY + lineIndex;
            if (y >= maxScreen.Bottom || y < maxScreen.Top)
            {
                lineIndex++;
                continue;
            }

            int x = screen.X;
            
            // Draw each run in the line
            foreach (var run in line.Runs)
            {
                if (string.IsNullOrEmpty(run.Text))
                {
                    continue;
                }

                // Set appropriate color
                driver.SetAttribute(run.IsHotKey ? hotColor : normalColor);
                
                // Draw the run text
                driver.Move(x, y);
                
                foreach (var rune in run.Text.EnumerateRunes())
                {
                    if (x >= maxScreen.Right)
                    {
                        break;
                    }
                    
                    if (x >= maxScreen.Left)
                    {
                        driver.AddRune(rune);
                    }
                    
                    x += Math.Max(rune.GetColumns(), 1);
                }
            }

            // Fill remaining space if requested
            if (fillRemaining && x < maxScreen.Right)
            {
                driver.SetAttribute(normalColor);
                while (x < maxScreen.Right)
                {
                    driver.Move(x, y);
                    driver.AddRune(' ');
                    x++;
                }
            }

            lineIndex++;
        }
    }

    /// <inheritdoc />
    public Region GetDrawRegion(
        FormattedText formattedText,
        Rectangle screen,
        Rectangle maximum = default)
    {
        var region = new Region();

        if (formattedText.Lines.Count == 0)
        {
            return region;
        }

        Rectangle maxScreen = CalculateMaxScreen(screen, maximum);

        if (maxScreen.Width == 0 || maxScreen.Height == 0)
        {
            return region;
        }

        int startY = screen.Y;
        int lineIndex = 0;

        foreach (var line in formattedText.Lines)
        {
            if (lineIndex >= maxScreen.Height)
            {
                break;
            }

            int y = startY + lineIndex;
            if (y >= maxScreen.Bottom || y < maxScreen.Top)
            {
                lineIndex++;
                continue;
            }

            int x = screen.X;
            int lineWidth = 0;
            
            // Calculate total width of the line
            foreach (var run in line.Runs)
            {
                if (!string.IsNullOrEmpty(run.Text))
                {
                    lineWidth += run.Text.GetColumns();
                }
            }

            if (lineWidth > 0 && x < maxScreen.Right)
            {
                int rightBound = Math.Min(x + lineWidth, maxScreen.Right);
                region.Union(new Rectangle(x, y, rightBound - x, 1));
            }

            lineIndex++;
        }

        return region;
    }

    private static Rectangle CalculateMaxScreen(Rectangle screen, Rectangle maximum)
    {
        if (maximum == default)
        {
            return screen;
        }

        return new Rectangle(
            Math.Max(maximum.X, screen.X),
            Math.Max(maximum.Y, screen.Y),
            Math.Max(Math.Min(maximum.Width, maximum.Right - screen.Left), 0),
            Math.Max(Math.Min(maximum.Height, maximum.Bottom - screen.Top), 0)
        );
    }
}