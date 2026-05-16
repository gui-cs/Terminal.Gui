namespace UICatalog.Scenarios;

[ScenarioMetadata ("Code View Demo", "Demonstrates the Code view with theme-aware syntax roles.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Colors")]
public sealed class CodeViewDemo : Scenario
{
    private readonly Dictionary<string, string> _snippets = new (StringComparer.OrdinalIgnoreCase)
    {
        ["cs"] = """
                 #nullable enable
                 using System;

                 // Code CodeComment CodeKeyword CodeString
                 // CodeNumber CodeOperator CodeType CodePreprocessor
                 // CodeIdentifier CodeConstant CodePunctuation
                 // CodeFunctionName CodeAttribute
                 [Obsolete ("CodeAttribute")]
                 public sealed class Person
                 {
                     // CodeComment
                     private const int CodeNumber = 37;
                     public string CodeString { get; init; } = "Ada";
                     public bool CodeConstant => true;
                     public int CodeFunctionName => Math.Max (CodeNumber, 21 + 16);
                 }

                 """,
        ["json"] = """
                   {
                     "roles1": "Code CodeComment CodeKeyword CodeString",
                     "roles2": "CodeNumber CodeOperator CodeType CodePreprocessor",
                     "roles3": "CodeIdentifier CodeConstant CodePunctuation",
                     "roles4": "CodeFunctionName CodeAttribute",
                     "code": "Code",
                     "comment": "CodeComment",
                     "keyword": "CodeKeyword",
                     "string": "CodeString",
                     "number": 37,
                     "operator": "CodeOperator",
                     "type": "CodeType",
                     "preprocessor": "CodePreprocessor",
                     "identifier": "CodeIdentifier",
                     "constant": true,
                     "punctuation": "CodePunctuation",
                     "function": "CodeFunctionName",
                     "attribute": "CodeAttribute"
                   }
                   """,
        ["xml"] = """
                  <?xml version="1.0"?>
                  <!-- Code CodeComment CodeKeyword CodeString -->
                  <!-- CodeNumber CodeOperator CodeType CodePreprocessor -->
                  <!-- CodeIdentifier CodeConstant CodePunctuation -->
                  <!-- CodeFunctionName CodeAttribute -->
                  <person name="Ada" active="true" CodeAttribute="CodeAttribute">
                    <age unit="years">37</age>
                    <roles keyword="CodeKeyword" type="CodeType" function="CodeFunctionName" />
                  </person>
                  """,
        ["md"] = """
                  # Code view

                  `VisualRole.CodeKeyword` follows the active theme.

                  Code CodeComment CodeKeyword CodeString
                  CodeNumber CodeOperator CodeType CodePreprocessor
                  CodeIdentifier CodeConstant CodePunctuation
                  CodeFunctionName CodeAttribute

                  ```cs
                  #nullable enable
                  [Obsolete ("CodeAttribute")]
                  public string CodeString => "Ada"; // CodeComment
                  public int CodeFunctionName => Math.Max (37, 21 + 16);
                  ```

                  """
    };

    /// <inheritdoc/>
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable window = new ();
        window.Title = GetQuitKeyAndName ();
        window.BorderStyle = LineStyle.None;

        string [] languages = ["cs", "json", "xml", "md"];

        OptionSelector languageSelector = new ()
        {
            Title = "_Language",
            Labels = languages,
            BorderStyle = LineStyle.Rounded,
            Width = 16,
            Height = Dim.Auto (),
            Value = 0
        };

        string [] themes = ThemeManager.GetThemeNames ().Select (theme => "_" + theme).ToArray ();

        OptionSelector themeSelector = new ()
        {
            Title = "_Theme",
            Labels = themes,
            BorderStyle = LineStyle.Rounded,
            X = Pos.Right (languageSelector) + 1,
            Width = 24,
            Height = Dim.Auto (),
            Value = ThemeManager.GetThemeNames ().IndexOf (ThemeManager.Theme)
        };

        Code code = new ()
        {
            X = 0,
            Y = Pos.Bottom (themeSelector) + 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Rounded,
            Title = "Code",
            Language = languages [0],
            SyntaxHighlighter = new RoleLegendHighlighter (),
            Text = _snippets [languages [0]]
        };

        languageSelector.ValueChanged += (_, args) =>
                                         {
                                             if (args.NewValue is null)
                                             {
                                                 return;
                                             }

                                             string language = languages [(int)args.NewValue];
                                             code.Language = language;
                                             code.Text = _snippets [language];
                                         };

        themeSelector.ValueChanged += (_, args) =>
                                      {
                                          if (args.NewValue is null)
                                          {
                                              return;
                                          }

                                          string theme = themes [(int)args.NewValue] [1..];
                                          ThemeManager.Theme = theme;
                                          ConfigurationManager.Apply ();
                                      };

        window.Add (languageSelector, themeSelector, code);
        app.Run (window);
    }

    private sealed class RoleLegendHighlighter : ISyntaxHighlighter
    {
        private readonly TextMateSyntaxHighlighter _textMate = new ();

        private static readonly (string Text, VisualRole Role) [] _roleMarkers =
        [
            (nameof (VisualRole.CodePreprocessor), VisualRole.CodePreprocessor),
            (nameof (VisualRole.CodeFunctionName), VisualRole.CodeFunctionName),
            (nameof (VisualRole.CodePunctuation), VisualRole.CodePunctuation),
            (nameof (VisualRole.CodeIdentifier), VisualRole.CodeIdentifier),
            (nameof (VisualRole.CodeAttribute), VisualRole.CodeAttribute),
            (nameof (VisualRole.CodeComment), VisualRole.CodeComment),
            (nameof (VisualRole.CodeKeyword), VisualRole.CodeKeyword),
            (nameof (VisualRole.CodeString), VisualRole.CodeString),
            (nameof (VisualRole.CodeNumber), VisualRole.CodeNumber),
            (nameof (VisualRole.CodeOperator), VisualRole.CodeOperator),
            (nameof (VisualRole.CodeConstant), VisualRole.CodeConstant),
            (nameof (VisualRole.CodeType), VisualRole.CodeType),
            (nameof (VisualRole.Code), VisualRole.Code)
        ];

        public IReadOnlyList<StyledSegment> Highlight (string code, string language)
        {
            List<StyledSegment> segments = [];

            foreach (StyledSegment segment in _textMate.Highlight (code, language))
            {
                AddRoleSegments (segments, segment);
            }

            return segments;
        }

        public void ResetState () => _textMate.ResetState ();

        public string ThemeName => ((ISyntaxHighlighter)_textMate).ThemeName;

        public Color? DefaultBackground => _textMate.DefaultBackground;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => _textMate.GetAttributeForScope (role);

        private static void AddRoleSegments (List<StyledSegment> segments, StyledSegment segment)
        {
            int index = 0;

            while (index < segment.Text.Length)
            {
                (string text, VisualRole role) = FindRoleMarker (segment.Text, index);

                if (text.Length == 0)
                {
                    AddSegment (segments, segment, segment.Text [index..(index + 1)], segment.Role);
                    index++;

                    continue;
                }

                AddSegment (segments, segment, text, role);
                index += text.Length;
            }
        }

        private static (string Text, VisualRole Role) FindRoleMarker (string text, int index)
        {
            foreach ((string marker, VisualRole role) in _roleMarkers)
            {
                if (text.AsSpan (index).StartsWith (marker, StringComparison.Ordinal))
                {
                    return (marker, role);
                }
            }

            return (string.Empty, VisualRole.Code);
        }

        private static void AddSegment (List<StyledSegment> segments, StyledSegment source, string text, VisualRole? role)
        {
            segments.Add (new StyledSegment (text, source.StyleRole, source.Url, source.ImageSource, source.Attribute, role));
        }
    }
}
