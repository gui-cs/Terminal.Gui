# Terminal.Gui.KeySequences

`Terminal.Gui.KeySequences` is an add-on package for leader-key and multi-key command sequences in Terminal.Gui apps.

```csharp
using Terminal.Gui.Input;
using Terminal.Gui.KeySequences;
using Terminal.Gui.Views;

TextView editor = new ();

IDisposable registration = editor.UseKeySequences (bindings =>
{
    bindings.Add ("; m <count> k", context =>
    {
        for (int i = 0; i < context.Count; i++)
        {
            context.Target.InvokeCommand (Command.Up);
        }

        return true;
    });
});
```

The add-on uses public Terminal.Gui keyboard events. It does not add APIs to the core `Terminal.Gui` assembly.
