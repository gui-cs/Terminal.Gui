// AI — A Terminal.Gui inline-mode CLI powered by the GitHub Copilot SDK.
//
// Usage:
//   ai                                    Interactive chat (default)
//   ai "What is 2+2?"                     Single-turn: answer and exit
//   ai --model claude-sonnet-4.5 "prompt"  Choose model
//
// Requires: GitHub Copilot CLI installed and authenticated (gh extension install github/gh-copilot)

using GitHub.Copilot.SDK;
using Terminal.Gui.App;

// ── Parse CLI args ───────────────────────────────────────────────────────────

var model = "claude-opus-4.6";
string? prompt = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args [i])
    {
        case "--model" or "-m" when i + 1 < args.Length:
            model = args [++i];

            break;

        case "--help" or "-h":
            Console.WriteLine ("Usage: ai [--model <model>] [\"prompt\"]");
            Console.WriteLine ();
            Console.WriteLine ("  No prompt → interactive chat mode");
            Console.WriteLine ("  With prompt → single-turn answer and exit");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            Console.WriteLine ("  --model, -m <model>  Model to use (default: claude-opus-4.6)");
            Console.WriteLine ("  --help, -h           Show this help");

            return 0;

        default:
            prompt = args [i];

            break;
    }
}

// ── Start Copilot SDK ────────────────────────────────────────────────────────

await using CopilotClient client = new ();
await client.StartAsync ();

// ── Run the appropriate mode ─────────────────────────────────────────────────

Application.AppModel = AppModel.Inline;
IApplication app = Application.Create ().Init ();

if (prompt is not null)
{
    return SingleTurnView.Run (app, client, model, prompt);
}

ChatView chatView = new (app, client, model);
app.Run (chatView);
app.Dispose ();

return chatView.ExitCode;
