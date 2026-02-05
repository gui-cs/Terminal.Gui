// ASCII Cline - A Terminal.Gui implementation of an AI coding assistant UI
// Inspired by Cline (VS Code extension)

using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

IApplication app = Application.Create ().Init ();
app.Run<ClineWindow> ();
app.Dispose ();

/// <summary>
/// Main window for the ASCII Cline AI coding assistant.
/// </summary>
public sealed class ClineWindow : Runnable
{
    private readonly ListView _chatView;
    private readonly ObservableCollection<ChatMessage> _messages;
    private readonly TextView _inputArea;
    private readonly Button _modeButton;
    private readonly Label _modeLabel;
    private readonly ListView _contextList;
    private readonly ObservableCollection<ContextItem> _contextItems;
    private readonly Label _tokenLabel;
    private readonly ProgressBar _taskProgress;
    private readonly Label _taskLabel;
    private bool _isPlanMode = true;
    private int _tokenCount;

    public ClineWindow ()
    {
        Title = "ASCII Cline - AI Coding Assistant (Esc to quit)";

        // Initialize collections
        _messages = [];
        _contextItems = [];

        // Create main layout panels
        FrameView contextPanel = CreateContextPanel ();
        FrameView chatPanel = CreateChatPanel ();
        FrameView inputPanel = CreateInputPanel ();
        StatusBar statusBar = CreateStatusBar ();

        Add (contextPanel, chatPanel, inputPanel, statusBar);

        // Add sample data
        LoadSampleData ();
    }

    private FrameView CreateContextPanel ()
    {
        FrameView panel = new ()
        {
            Title = "@ Context",
            X = 0,
            Y = 0,
            Width = 25,
            Height = Dim.Fill (4)
        };

        Label contextLabel = new ()
        {
            Text = "Add context with @:",
            X = 0,
            Y = 0,
            Width = Dim.Fill ()
        };

        _contextList = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (3),
            Source = new ListWrapper<ContextItem> (_contextItems)
        };

        Button addFileBtn = new ()
        {
            Text = "@File",
            X = 0,
            Y = Pos.AnchorEnd (2)
        };

        Button addErrorBtn = new ()
        {
            Text = "@Error",
            X = Pos.Right (addFileBtn) + 1,
            Y = Pos.AnchorEnd (2)
        };

        Button addUrlBtn = new ()
        {
            Text = "@URL",
            X = 0,
            Y = Pos.AnchorEnd (1)
        };

        addFileBtn.Accepting += (_, e) =>
        {
            ShowAddContextDialog ("file");
            e.Handled = true;
        };

        addErrorBtn.Accepting += (_, e) =>
        {
            AddContextItem (new ContextItem ("Error", "TS2304: Cannot find name", ContextType.Error));
            e.Handled = true;
        };

        addUrlBtn.Accepting += (_, e) =>
        {
            ShowAddContextDialog ("url");
            e.Handled = true;
        };

        panel.Add (contextLabel, _contextList, addFileBtn, addErrorBtn, addUrlBtn);

        return panel;
    }

    private FrameView CreateChatPanel ()
    {
        FrameView panel = new ()
        {
            Title = "Chat",
            X = 25,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (8)
        };

        _chatView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Source = new ChatMessageSource (_messages)
        };

        _taskLabel = new ()
        {
            Text = "",
            X = 0,
            Y = Pos.AnchorEnd (2),
            Width = Dim.Fill (),
            Visible = false
        };

        _taskProgress = new ()
        {
            X = 0,
            Y = Pos.AnchorEnd (1),
            Width = Dim.Fill (),
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous,
            Visible = false
        };

        panel.Add (_chatView, _taskLabel, _taskProgress);

        return panel;
    }

    private FrameView CreateInputPanel ()
    {
        FrameView panel = new ()
        {
            Title = "Message",
            X = 25,
            Y = Pos.AnchorEnd (8),
            Width = Dim.Fill (),
            Height = 7
        };

        // Mode toggle row
        _modeLabel = new ()
        {
            Text = "Mode:",
            X = 0,
            Y = 0
        };

        _modeButton = new ()
        {
            Text = _isPlanMode ? "[PLAN]" : "[ACT]",
            X = Pos.Right (_modeLabel) + 1,
            Y = 0
        };

        Label modeDesc = new ()
        {
            X = Pos.Right (_modeButton) + 1,
            Y = 0,
            Width = Dim.Fill (),
            Text = _isPlanMode ? "Planning mode - gathering info, asking questions" : "Act mode - executing the plan"
        };

        _modeButton.Accepting += (_, e) =>
        {
            _isPlanMode = !_isPlanMode;
            _modeButton.Text = _isPlanMode ? "[PLAN]" : "[ACT]";
            modeDesc.Text = _isPlanMode ? "Planning mode - gathering info, asking questions" : "Act mode - executing the plan";
            e.Handled = true;
        };

        // Input text area
        _inputArea = new ()
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill (10),
            Height = 3,
            Text = ""
        };

        // Token counter
        _tokenLabel = new ()
        {
            Text = "Tokens: 0",
            X = Pos.AnchorEnd (12),
            Y = 2
        };

        _inputArea.TextChanged += (_, _) =>
        {
            _tokenCount = _inputArea.Text.Length / 4; // Rough estimate
            _tokenLabel.Text = $"Tokens: {_tokenCount}";
        };

        // Send button
        Button sendButton = new ()
        {
            Text = "Send",
            X = Pos.AnchorEnd (10),
            Y = 3,
            IsDefault = true
        };

        sendButton.Accepting += (_, e) =>
        {
            SendMessage ();
            e.Handled = true;
        };

        panel.Add (_modeLabel, _modeButton, modeDesc, _inputArea, _tokenLabel, sendButton);

        return panel;
    }

    private StatusBar CreateStatusBar ()
    {
        StatusBar statusBar = new ()
        {
            Y = Pos.AnchorEnd (1)
        };

        statusBar.Add (
            new Shortcut
            {
                Title = "Plan/Act",
                Key = Key.F1,
                Action = () =>
                {
                    _isPlanMode = !_isPlanMode;
                    _modeButton.Text = _isPlanMode ? "[PLAN]" : "[ACT]";
                }
            },
            new Shortcut
            {
                Title = "New Chat",
                Key = Key.F2,
                Action = () =>
                {
                    _messages.Clear ();
                    _contextItems.Clear ();
                    _chatView.SetNeedsDraw ();
                    _contextList.SetNeedsDraw ();
                }
            },
            new Shortcut
            {
                Title = "Add File",
                Key = Key.F3,
                Action = () => ShowAddContextDialog ("file")
            },
            new Shortcut
            {
                Title = "Settings",
                Key = Key.F4,
                Action = ShowSettings
            },
            new Shortcut
            {
                Title = "Quit",
                Key = Key.Esc,
                Action = () => App!.RequestStop ()
            }
        );

        return statusBar;
    }

    private void ShowAddContextDialog (string type)
    {
        Dialog dialog = new ()
        {
            Title = type == "file" ? "Add File" : "Add URL",
            Width = 50,
            Height = 8,
            Buttons = [new Button ("Add"), new Button ("Cancel")]
        };

        Label label = new ()
        {
            Text = type == "file" ? "File path:" : "URL:",
            X = 1,
            Y = 1
        };

        TextField textField = new ()
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill (1),
            Text = type == "file" ? "src/index.ts" : "https://docs.example.com"
        };

        dialog.Add (label, textField);

        dialog.Buttons [0].Accepting += (_, e) =>
        {
            ContextType contextType = type == "file" ? ContextType.File : ContextType.Url;
            AddContextItem (new ContextItem (type == "file" ? "File" : "URL", textField.Text, contextType));
            App!.RequestStop ();
            e.Handled = true;
        };

        dialog.Buttons [1].Accepting += (_, e) =>
        {
            App!.RequestStop ();
            e.Handled = true;
        };

        App!.Run (dialog);
    }

    private void ShowSettings ()
    {
        Dialog dialog = new ()
        {
            Title = "Settings",
            Width = 60,
            Height = 16,
            Buttons = [new Button ("OK"), new Button ("Cancel")]
        };

        Label modelLabel = new () { Text = "Model:", X = 1, Y = 1 };
        ComboBox modelCombo = new ()
        {
            X = 15,
            Y = 1,
            Width = 30,
            Height = 5,
            Source = new ListWrapper<string> (["claude-opus-4-5-20251101", "claude-sonnet-4-20250514", "gpt-4o", "gpt-4o-mini"])
        };
        modelCombo.SelectedItem = 0;

        Label apiKeyLabel = new () { Text = "API Key:", X = 1, Y = 3 };
        TextField apiKeyField = new ()
        {
            X = 15,
            Y = 3,
            Width = 30,
            Secret = true,
            Text = "sk-ant-..."
        };

        Label autoApproveLabel = new () { Text = "Auto-approve:", X = 1, Y = 5 };
        CheckBox autoApproveCheck = new ()
        {
            X = 15,
            Y = 5,
            Text = "File writes"
        };

        CheckBox autoApproveCmd = new ()
        {
            X = 15,
            Y = 6,
            Text = "Commands"
        };

        Label maxTokensLabel = new () { Text = "Max tokens:", X = 1, Y = 8 };
        NumericUpDown<int> maxTokensField = new ()
        {
            X = 15,
            Y = 8,
            Value = 8192
        };

        dialog.Add (modelLabel, modelCombo, apiKeyLabel, apiKeyField, autoApproveLabel, autoApproveCheck, autoApproveCmd, maxTokensLabel, maxTokensField);

        dialog.Buttons [0].Accepting += (_, e) =>
        {
            App!.RequestStop ();
            e.Handled = true;
        };

        dialog.Buttons [1].Accepting += (_, e) =>
        {
            App!.RequestStop ();
            e.Handled = true;
        };

        App!.Run (dialog);
    }

    private void AddContextItem (ContextItem item)
    {
        _contextItems.Add (item);
        _contextList.SetNeedsDraw ();
    }

    private void SendMessage ()
    {
        string text = _inputArea.Text.Trim ();

        if (string.IsNullOrEmpty (text))
        {
            return;
        }

        // Add user message
        _messages.Add (new ChatMessage ("You", text, MessageType.User));
        _inputArea.Text = "";
        _chatView.MoveEnd ();
        _chatView.SetNeedsDraw ();

        // Simulate AI thinking
        SimulateAiResponse (text);
    }

    private void SimulateAiResponse (string userMessage)
    {
        _taskLabel.Visible = true;
        _taskProgress.Visible = true;
        _taskLabel.Text = "Thinking...";
        _taskProgress.Fraction = 0;

        int step = 0;
        string[] responses =
        [
            $"I'll help you with that. Let me {(_isPlanMode ? "plan" : "implement")} this.",
            GetContextualResponse (userMessage)
        ];

        App!.AddTimeout (TimeSpan.FromMilliseconds (500), () =>
        {
            step++;
            _taskProgress.Fraction = step / 10f;

            if (step == 3)
            {
                _taskLabel.Text = "Reading files...";
            }
            else if (step == 5)
            {
                _taskLabel.Text = "Analyzing code...";
            }
            else if (step == 7)
            {
                _taskLabel.Text = "Generating response...";
            }
            else if (step >= 10)
            {
                _taskLabel.Visible = false;
                _taskProgress.Visible = false;

                foreach (string response in responses)
                {
                    _messages.Add (new ChatMessage ("Cline", response, MessageType.Assistant));
                }

                if (_isPlanMode)
                {
                    _messages.Add (new ChatMessage ("Cline", "Switch to [ACT] mode when ready to execute.", MessageType.System));
                }

                _chatView.MoveEnd ();
                _chatView.SetNeedsDraw ();

                return false;
            }

            return true;
        });
    }

    private string GetContextualResponse (string userMessage)
    {
        string lowerMessage = userMessage.ToLower ();

        if (lowerMessage.Contains ("bug") || lowerMessage.Contains ("fix") || lowerMessage.Contains ("error"))
        {
            return _isPlanMode
                ? "Plan:\n1. Identify the root cause\n2. Review related code\n3. Implement fix\n4. Write tests\n5. Verify fix works"
                : "I'll fix this by modifying the affected files. Here are the changes I'll make...";
        }

        if (lowerMessage.Contains ("feature") || lowerMessage.Contains ("add") || lowerMessage.Contains ("create"))
        {
            return _isPlanMode
                ? "Plan:\n1. Design the component structure\n2. Create necessary files\n3. Implement core logic\n4. Add styling\n5. Write tests"
                : "Creating the new feature now. I'll update these files...";
        }

        if (lowerMessage.Contains ("refactor") || lowerMessage.Contains ("improve"))
        {
            return _isPlanMode
                ? "Plan:\n1. Analyze current implementation\n2. Identify improvements\n3. Plan refactoring steps\n4. Make changes incrementally\n5. Ensure tests pass"
                : "Refactoring in progress. The changes maintain backward compatibility...";
        }

        return _isPlanMode
            ? "I understand your request. Let me gather more information.\n\nWhat specific files or areas should I focus on?"
            : "I'll work on that now. Let me analyze the codebase and make the necessary changes.";
    }

    private void LoadSampleData ()
    {
        // Sample context items
        _contextItems.Add (new ContextItem ("File", "src/main.ts", ContextType.File));
        _contextItems.Add (new ContextItem ("File", "package.json", ContextType.File));

        // Sample chat messages
        _messages.Add (new ChatMessage ("System", "Welcome to ASCII Cline! I'm your AI coding assistant.", MessageType.System));
        _messages.Add (new ChatMessage ("System", "Use @ buttons to add context, then describe what you need.", MessageType.System));
        _messages.Add (new ChatMessage ("System", "Toggle Plan/Act mode with F1 or click the mode button.", MessageType.System));
    }
}

/// <summary>
/// Represents a chat message in the conversation.
/// </summary>
public record ChatMessage (string Sender, string Content, MessageType Type)
{
    public override string ToString ()
    {
        string prefix = Type switch
        {
            MessageType.User => "> ",
            MessageType.Assistant => "< ",
            MessageType.System => "* ",
            _ => "  "
        };

        return $"{prefix}[{Sender}] {Content}";
    }
}

/// <summary>
/// Type of chat message.
/// </summary>
public enum MessageType
{
    User,
    Assistant,
    System
}

/// <summary>
/// Represents a context item added via @ mention.
/// </summary>
public record ContextItem (string Type, string Value, ContextType ContextType)
{
    public override string ToString ()
    {
        string icon = ContextType switch
        {
            ContextType.File => "[F]",
            ContextType.Url => "[U]",
            ContextType.Error => "[!]",
            _ => "[ ]"
        };

        return $"{icon} {Value}";
    }
}

/// <summary>
/// Type of context item.
/// </summary>
public enum ContextType
{
    File,
    Url,
    Error
}

/// <summary>
/// Custom data source for chat messages with proper formatting.
/// </summary>
public class ChatMessageSource : IListDataSource
{
    private readonly ObservableCollection<ChatMessage> _messages;

    public ChatMessageSource (ObservableCollection<ChatMessage> messages)
    {
        _messages = messages;
        _messages.CollectionChanged += (_, _) => { };
    }

    public int Count => _messages.Count;

    public int Length => _messages.Count > 0 ? _messages.Max (m => m.ToString ().Length) : 0;

    public bool SuspendCollectionChangedEvent { get; set; }

    public void Render (
        ListView container,
        ConsoleDriver driver,
        bool selected,
        int item,
        int col,
        int line,
        int width,
        int start = 0)
    {
        if (item < 0 || item >= _messages.Count)
        {
            return;
        }

        ChatMessage message = _messages [item];
        string text = message.ToString ();

        if (start > 0)
        {
            text = text.Length > start ? text [start..] : "";
        }

        if (text.Length > width)
        {
            text = text [..width];
        }

        driver.AddStr (text.PadRight (width));
    }

    public bool IsMarked (int item) => false;

    public void SetMark (int item, bool value) { }

    public object ToObject (int index) => index >= 0 && index < _messages.Count ? _messages [index] : null!;

    public IList ToList () => _messages;

    public void Dispose () { }
}
