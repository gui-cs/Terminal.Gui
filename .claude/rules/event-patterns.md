# Event Patterns

## Lambda Parameters

**Replace unused parameters with discards `_`:**

```csharp
// CORRECT
textField.TextChanged += (_, _) => { /* ... */ };
checkbox.CheckedStateChanging += (_, args) => checkbox.Enabled = args.Result == CheckState.Checked;

// WRONG - unused parameters
textField.TextChanged += (s, prev) => { /* ... */ };
textField.TextChanged += (sender, e) => { /* ... */ };
```

## Captured Variable Closures

**When a variable is captured and later modified, use sender:**

```csharp
// WRONG - textField captured, then reassigned later
textField.TextChanged += (_, _) => { label.Text = textField.Text; };
textField = new () { ... };  // Bug: closure references wrong instance

// CORRECT - use sender to get the actual firing instance
textField.TextChanged += (sender, _) => { label.Text = ((TextField)sender!).Text; };
```

**If sender won't work, disable the warning:**

```csharp
// ReSharper disable once AccessToModifiedClosure
textField.TextChanged += (_, _) => { label.Text = textField.Text; };
```

## Event Handler Nullability

**Use `object?` for sender parameter:**

```csharp
// CORRECT
void OnTextChanged (object? sender, EventArgs e) { }

// WRONG
void OnTextChanged (object sender, EventArgs e) { }
```

## Local Function Naming

**Use camelCase for local functions:**

```csharp
// CORRECT
void textViewDrawContent (object? sender, DrawEventArgs e) { }

// WRONG
void TextView_DrawContent (object? sender, DrawEventArgs e) { }
void TextViewDrawContent (object? sender, DrawEventArgs e) { }
```
