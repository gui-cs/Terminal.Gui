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
                 public sealed class Person
                 {
                     public string Name { get; init; } = "Ada";
                     public int Age => 37;
                 }
                 """,
        ["json"] = """
                   {
                     "name": "Ada",
                     "age": 37,
                     "active": true
                   }
                   """,
        ["xml"] = """
                  <person name="Ada">
                    <age>37</age>
                  </person>
                  """,
        ["md"] = """
                # Code view

                `VisualRole.CodeKeyword` follows the active theme.
                """
    };

    /// <inheritdoc/>
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable window = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

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
}
