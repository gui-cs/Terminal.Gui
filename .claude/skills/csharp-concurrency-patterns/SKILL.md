---
name: csharp-concurrency-patterns
description: Choosing the right concurrency abstraction in .NET - from async/await for I/O to Channels for producer/consumer. Relevant for Terminal.Gui's main loop, event handling, and background operations.
---

# .NET Concurrency: Choosing the Right Tool

> **Terminal.Gui Context:** Terminal.Gui has a main loop that handles input events and UI updates. Background operations must marshal back to the main thread using `Application.Invoke()`.

## When to Use This Skill

Use this skill when:
- Deciding how to handle concurrent operations
- Evaluating whether to use async/await, Channels, or other abstractions
- Working with Terminal.Gui's main loop and background tasks
- Managing state across multiple concurrent operations

## The Philosophy

**Start simple, escalate only when needed.**

Most concurrency problems can be solved with `async/await`. Only reach for more sophisticated tools when you have a specific need.

**Try to avoid shared mutable state.** The best way to handle concurrency is to design it away. Immutable data, message passing, and isolated state eliminate entire categories of bugs.

---

## Decision Tree

```
What are you trying to do?
|
|-> Wait for I/O (HTTP, file, database)?
|   -> Use async/await
|
|-> Process a collection in parallel (CPU-bound)?
|   -> Use Parallel.ForEachAsync
|
|-> Producer/consumer pattern (work queue)?
|   -> Use System.Threading.Channels
|
|-> Update UI from background thread?
|   -> Use Application.Invoke()
|
|-> Coordinate multiple async operations?
|   -> Use Task.WhenAll / Task.WhenAny
|
|-> None of the above fits?
    -> Ask: "Do I really need shared mutable state?"
        -> Yes -> Redesign to avoid it or use Channels
```

---

## Level 1: async/await (Default Choice)

**Use for:** I/O-bound operations, non-blocking waits, most everyday concurrency.

```csharp
// Simple async I/O
public async Task<string> LoadConfigAsync(CancellationToken cancellationToken = default)
{
    string content = await File.ReadAllTextAsync(_configPath, cancellationToken);
    return content;
}

// Parallel async operations (when independent)
public async Task<Dashboard> LoadDashboardAsync(CancellationToken cancellationToken = default)
{
    Task<List<Order>> ordersTask = _orderService.GetRecentOrdersAsync(cancellationToken);
    Task<List<Notification>> notificationsTask = _notificationService.GetUnreadAsync(cancellationToken);

    await Task.WhenAll(ordersTask, notificationsTask);

    return new Dashboard(
        Orders: await ordersTask,
        Notifications: await notificationsTask);
}
```

**Key principles:**
- Always accept `CancellationToken`
- Don't block on async code (no `.Result` or `.Wait()`)

---

## Level 2: Terminal.Gui Main Loop Integration

**Use for:** Updating UI from background operations.

```csharp
// Background operation that updates UI
public async Task LoadDataAsync()
{
    // Run on background thread
    List<Item> items = await Task.Run(async () =>
    {
        return await _dataService.FetchItemsAsync();
    });

    // Marshal back to main thread for UI update
    Application.Invoke(() =>
    {
        _listView.SetSource(items);
        _statusLabel.Text = $"Loaded {items.Count} items";
    });
}

// With progress reporting
public async Task ProcessItemsAsync(IProgress<int> progress)
{
    for (var i = 0; i < _items.Count; i++)
    {
        await ProcessItemAsync(_items[i]);

        // Report progress - will be marshaled to main thread
        progress.Report((i + 1) * 100 / _items.Count);
    }
}

// Usage with Terminal.Gui
Progress<int> progress = new (percent =>
{
    Application.Invoke(() => _progressBar.Fraction = percent / 100f);
});

_ = ProcessItemsAsync(progress);
```

---

## Level 3: Parallel.ForEachAsync (CPU-Bound Parallelism)

**Use for:** Processing collections in parallel when work is CPU-bound.

```csharp
// Process items with controlled parallelism
public async Task ProcessFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken)
{
    await Parallel.ForEachAsync(
        filePaths,
        new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        },
        async (path, token) =>
        {
            await ProcessFileAsync(path, token);
        });
}
```

**When NOT to use:**
- Pure I/O operations (async/await is sufficient)
- When order matters (Parallel doesn't preserve order)

---

## Level 4: System.Threading.Channels (Producer/Consumer)

**Use for:** Work queues, producer/consumer patterns, decoupling producers from consumers.

```csharp
// Basic producer/consumer for background processing
public class BackgroundProcessor
{
    private readonly Channel<WorkItem> _channel;

    public BackgroundProcessor()
    {
        // Bounded channel provides backpressure
        _channel = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    // Producer - call from UI
    public async Task EnqueueAsync(WorkItem item, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(item, cancellationToken);
    }

    // Consumer - run as background task
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await foreach (WorkItem item in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            await ProcessItemAsync(item, cancellationToken);

            // Update UI with result
            Application.Invoke(() => UpdateUI(item));
        }
    }

    public void Complete() => _channel.Writer.Complete();
}
```

---

## Anti-Patterns: What to Avoid

### Locks for Business Logic

```csharp
// BAD: Using locks to protect shared state
private readonly object _lock = new ();
private Dictionary<string, View> _views = new ();

public void UpdateView(string id, Action<View> update)
{
    lock (_lock)
    {
        if (_views.TryGetValue(id, out View? view))
        {
            update(view);
        }
    }
}

// GOOD: Use Application.Invoke to serialize UI updates
Application.Invoke(() =>
{
    if (_views.TryGetValue(id, out View? view))
    {
        update(view);
    }
});
```

### Blocking in Async Code

```csharp
// BAD: Blocking on async
string result = GetDataAsync().Result; // Deadlock risk!
GetDataAsync().Wait();                  // Also bad

// GOOD: Async all the way
string result = await GetDataAsync();
```

### Shared Mutable State Without Protection

```csharp
// BAD: Multiple tasks mutating shared state
List<Result> results = [];
await Parallel.ForEachAsync(items, async (item, ct) =>
{
    Result result = await ProcessAsync(item, ct);
    results.Add(result); // Race condition!
});

// GOOD: Use ConcurrentBag or collect results differently
ConcurrentBag<Result> results = [];
await Parallel.ForEachAsync(items, async (item, ct) =>
{
    Result result = await ProcessAsync(item, ct);
    results.Add(result); // Thread-safe
});
```

---

## Quick Reference

| Need | Tool | Example |
|------|------|---------|
| Wait for I/O | `async/await` | HTTP calls, file operations |
| Parallel CPU work | `Parallel.ForEachAsync` | Image processing |
| Work queue | `Channel<T>` | Background job processing |
| Update UI from background | `Application.Invoke()` | Progress updates |
| Fire multiple async ops | `Task.WhenAll` | Loading dashboard data |
| Race multiple async ops | `Task.WhenAny` | Timeout with fallback |
| Periodic work | `PeriodicTimer` | Health checks, polling |

---

## The Escalation Path

```
async/await (start here)
    |
    |-> Need parallelism? -> Parallel.ForEachAsync
    |
    |-> Need producer/consumer? -> Channel<T>
    |
    |-> Need UI updates from background? -> Application.Invoke()
```

**Only escalate when you have a concrete need.**

---

## Attribution

This skill is adapted from [dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) by Aaron Stannard, licensed under Apache-2.0.
