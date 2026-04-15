using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

var opts = new RegistryOptions(ThemeName.DarkPlus);
var reg = new Registry(opts);
var theme = reg.GetTheme();

string[][] scopes = [
    ["markup.heading.markdown"],
    ["markup.heading"],
    ["entity.name.section.markdown"],
    ["punctuation.definition.heading.markdown"],
    ["punctuation.definition.heading"],
    ["markup.italic.markdown"],
    ["markup.italic"],
    ["markup.bold.markdown"],
    ["markup.bold"],
    ["markup.inline.raw.string.markdown"],
    ["markup.inline.raw"],
    ["markup.underline.link.markdown"],
    ["markup.underline.link"],
    ["markup.quote.markdown"],
    ["markup.quote"],
    ["punctuation.definition.list.begin.markdown"],
    ["meta.separator.markdown"],
    ["markup.fenced_code.block.markdown"],
    ["markup.strikethrough.markdown"],
    ["markup.strikethrough"],
];

foreach (var s in scopes) {
    var rules = theme.Match(s.ToList());
    var fg = rules.Count > 0 ? theme.GetColor(rules[0].foreground) : "none";
    var bg = rules.Count > 0 ? theme.GetColor(rules[0].background) : "none";
    Console.WriteLine($"{string.Join(",", s)}: rules={rules.Count} fg={fg} bg={bg}");
}
