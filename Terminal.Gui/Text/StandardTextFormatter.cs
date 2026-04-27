#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Terminal.Gui.Text;

/// <summary>
///     Standard implementation of <see cref="ITextFormatter"/> that provides the same functionality
///     as the original TextFormatter but with proper separation of concerns.
/// </summary>
public class StandardTextFormatter : ITextFormatter
{
    private string _text = string.Empty;
    private Size? _constrainToSize;
    private Alignment _alignment = Alignment.Start;
    private Alignment _verticalAlignment = Alignment.Start;
    private TextDirection _direction = TextDirection.LeftRight_TopBottom;
    private bool _wordWrap = true;
    private bool _multiLine = true;
    private Rune _hotKeySpecifier = (Rune)0xFFFF;
    private int _tabWidth = 4;
    private bool _preserveTrailingSpaces = false;

    // Caching
    private FormattedText? _cachedResult;
    private int _cacheHash;

    /// <inheritdoc />
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public Size? ConstrainToSize
    {
        get => _constrainToSize;
        set
        {
            if (_constrainToSize != value)
            {
                _constrainToSize = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public Alignment VerticalAlignment
    {
        get => _verticalAlignment;
        set
        {
            if (_verticalAlignment != value)
            {
                _verticalAlignment = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public TextDirection Direction
    {
        get => _direction;
        set
        {
            if (_direction != value)
            {
                _direction = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public bool WordWrap
    {
        get => _wordWrap;
        set
        {
            if (_wordWrap != value)
            {
                _wordWrap = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public bool MultiLine
    {
        get => _multiLine;
        set
        {
            if (_multiLine != value)
            {
                _multiLine = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public Rune HotKeySpecifier
    {
        get => _hotKeySpecifier;
        set
        {
            if (_hotKeySpecifier.Value != value.Value)
            {
                _hotKeySpecifier = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public int TabWidth
    {
        get => _tabWidth;
        set
        {
            if (_tabWidth != value)
            {
                _tabWidth = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public bool PreserveTrailingSpaces
    {
        get => _preserveTrailingSpaces;
        set
        {
            if (_preserveTrailingSpaces != value)
            {
                _preserveTrailingSpaces = value;
                InvalidateCache();
            }
        }
    }

    /// <inheritdoc />
    public FormattedText Format()
    {
        // Check cache first
        int currentHash = GetSettingsHash();
        if (_cachedResult != null && _cacheHash == currentHash)
        {
            return _cachedResult;
        }

        // Perform formatting
        var result = DoFormat();
        
        // Update cache
        _cachedResult = result;
        _cacheHash = currentHash;
        
        return result;
    }

    /// <inheritdoc />
    public Size GetFormattedSize()
    {
        return Format().RequiredSize;
    }

    private void InvalidateCache()
    {
        _cachedResult = null;
    }

    private int GetSettingsHash()
    {
        var hash = new HashCode();
        hash.Add(_text);
        hash.Add(_constrainToSize);
        hash.Add(_alignment);
        hash.Add(_verticalAlignment);
        hash.Add(_direction);
        hash.Add(_wordWrap);
        hash.Add(_multiLine);
        hash.Add(_hotKeySpecifier.Value);
        hash.Add(_tabWidth);
        hash.Add(_preserveTrailingSpaces);
        return hash.ToHashCode();
    }

    private FormattedText DoFormat()
    {
        if (string.IsNullOrEmpty(_text))
        {
            return new FormattedText(Array.Empty<FormattedLine>(), Size.Empty);
        }

        // Process HotKey
        var processedText = _text;
        var hotKey = Key.Empty;
        var hotKeyPos = -1;

        if (_hotKeySpecifier.Value != 0xFFFF && TextFormatter.FindHotKey(_text, _hotKeySpecifier, out hotKeyPos, out hotKey))
        {
            processedText = TextFormatter.RemoveHotKeySpecifier(_text, hotKeyPos, _hotKeySpecifier);
        }

        // Get constraints
        int width = _constrainToSize?.Width ?? int.MaxValue;
        int height = _constrainToSize?.Height ?? int.MaxValue;

        // Handle zero constraints
        if (width == 0 || height == 0)
        {
            return new FormattedText(Array.Empty<FormattedLine>(), Size.Empty, hotKey, hotKeyPos);
        }

        // Format the text using existing TextFormatter static methods
        List<string> lines;

        if (TextFormatter.IsVerticalDirection(_direction))
        {
            int colsWidth = TextFormatter.GetSumMaxCharWidth(processedText, 0, 1, _tabWidth);
            lines = TextFormatter.Format(
                processedText,
                height,
                _verticalAlignment == Alignment.Fill,
                width > colsWidth && _wordWrap,
                _preserveTrailingSpaces,
                _tabWidth,
                _direction,
                _multiLine
            );

            colsWidth = TextFormatter.GetMaxColsForWidth(lines, width, _tabWidth);
            if (lines.Count > colsWidth)
            {
                lines.RemoveRange(colsWidth, lines.Count - colsWidth);
            }
        }
        else
        {
            lines = TextFormatter.Format(
                processedText,
                width,
                _alignment == Alignment.Fill,
                height > 1 && _wordWrap,
                _preserveTrailingSpaces,
                _tabWidth,
                _direction,
                _multiLine
            );

            if (lines.Count > height)
            {
                lines.RemoveRange(height, lines.Count - height);
            }
        }

        // Convert to FormattedText structure
        var formattedLines = new List<FormattedLine>();
        
        foreach (string line in lines)
        {
            var runs = new List<FormattedRun>();
            
            // For now, create simple runs - we can enhance this later for HotKey highlighting
            if (!string.IsNullOrEmpty(line))
            {
                // Check if this line contains the HotKey
                if (hotKeyPos >= 0 && hotKey != Key.Empty)
                {
                    // Simple implementation - just mark the whole line for now
                    // TODO: Implement proper HotKey run detection
                    runs.Add(new FormattedRun(line, false));
                }
                else
                {
                    runs.Add(new FormattedRun(line, false));
                }
            }
            
            int lineWidth = TextFormatter.IsVerticalDirection(_direction) 
                ? TextFormatter.GetColumnsRequiredForVerticalText(new List<string> { line }, 0, 1, _tabWidth)
                : line.GetColumns();
                
            formattedLines.Add(new FormattedLine(runs, lineWidth));
        }

        // Calculate required size
        Size requiredSize;
        if (TextFormatter.IsVerticalDirection(_direction))
        {
            requiredSize = new Size(
                TextFormatter.GetColumnsRequiredForVerticalText(lines, 0, lines.Count, _tabWidth),
                lines.Max(line => line.Length)
            );
        }
        else
        {
            requiredSize = new Size(
                lines.Max(line => line.GetColumns()),
                lines.Count
            );
        }

        return new FormattedText(formattedLines, requiredSize, hotKey, hotKeyPos);
    }
}