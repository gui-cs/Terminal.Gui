// AI — A Terminal.Gui inline-mode CLI powered by the GitHub Copilot SDK.
//
// Requires: GitHub Copilot CLI installed and authenticated (gh extension install github/gh-copilot)

using System.CommandLine;
using System.CommandLine.Help;
using AI;
using GitHub.Copilot.SDK;
using Terminal.Gui.App;

Option<string> modelOption = new ("--model") { Description = "The Copilot model to use.", DefaultValueFactory = _ => "claude-opus-4.6" };
modelOption.Aliases.Add ("-m");

Argument<string?> promptArgument = new ("prompt") { Description = "Prompt text. If omitted, interactive chat mode starts.", DefaultValueFactory = _ => null };
promptArgument.Arity = ArgumentArity.ZeroOrOne;

RootCommand rootCommand = new ("Terminal.Gui inline-mode Copilot chat") { modelOption, promptArgument };

// Capture parsed values — SetAction runs synchronously, so we store and act after.
var parsedModel = "claude-opus-4.6";
string? parsedPrompt = null;

rootCommand.SetAction (context =>
                       {
                           parsedModel = context.GetRequiredValue (modelOption);
                           parsedPrompt = context.GetValue (promptArgument);
                       });

ParseResult parseResult = rootCommand.Parse (args);

if (parseResult.Errors.Count > 0 || parseResult.Action is HelpAction)
{
    parseResult.Invoke ();

    return parseResult.Errors.Count > 0 ? 1 : 0;
}

parseResult.Invoke ();

// ── Start Copilot SDK and run ────────────────────────────────────────────────

await using CopilotClient client = new ();
await client.StartAsync ();

Application.AppModel = AppModel.Inline;
IApplication app = Application.Create ().Init ();

if (parsedPrompt is { })
{
    return SingleTurnView.Run (app, client, parsedModel, parsedPrompt);
}

ChatView chatView = new (app, client, parsedModel);
app.Run (chatView);
app.Dispose ();

return chatView.ExitCode;
