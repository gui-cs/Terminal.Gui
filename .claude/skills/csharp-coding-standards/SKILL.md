---
name: csharp-coding-standards
description: Modern C# coding standards for high-performance code. Covers records, pattern matching, value objects, async/await, Span<T>/Memory<T>, and API design. Adapted from dotnet-skills with Terminal.Gui-specific overrides.
---

# Modern C# Coding Standards

> **Note:** Terminal.Gui has specific coding rules in `.claude/rules/` that take precedence over general .NET practices. Key differences:
> - **No `var`** except for built-in types (int, string, bool, etc.)
> - **Use `new ()`** instead of `new TypeName()`
> - **Use `[...]`** collection expressions instead of `new () { ... }`
> - **SubView/SuperView** terminology for view containment

## When to Use This Skill

Use this skill when:
- Writing new C# code or refactoring existing code
- Designing public APIs for the library
- Optimizing performance-critical code paths
- Implementing domain models with strong typing
- Building async/await-heavy applications

## Core Principles

1. **Immutability by Default** - Use `record` types and `init`-only properties
2. **Type Safety** - Leverage nullable reference types and value objects
3. **Modern Pattern Matching** - Use `switch` expressions and patterns extensively
4. **Async Everywhere** - Prefer async APIs with proper cancellation support
5. **Zero-Allocation Patterns** - Use `Span<T>` and `Memory<T>` for performance-critical code
6. **Composition Over Inheritance** - Avoid abstract base classes, prefer composition

---

## Records for Immutable Data (C# 9+)

Use `record` types for DTOs, messages, events, and domain entities.

```csharp
// Simple immutable DTO
public record CustomerDto(string Id, string Name, string Email);

// Record with validation in constructor
public record EmailAddress
{
    public string Value { get; init; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new ArgumentException("Invalid email address", nameof(value));

        Value = value;
    }
}

// Records with collections - use IReadOnlyList
public record ShoppingCart(
    string CartId,
    string CustomerId,
    IReadOnlyList<CartItem> Items
)
{
    public decimal Total => Items.Sum(item => item.Price * item.Quantity);
}
```

---

## Value Objects as readonly record struct

Value objects should **always be `readonly record struct`** for performance and value semantics.

```csharp
// Single-value object
public readonly record struct OrderId(string Value)
{
    public OrderId(string value) : this(
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("OrderId cannot be empty", nameof(value)))
    {
    }

    public override string ToString() => Value;
}

// Multi-value object
public readonly record struct Money(decimal Amount, string Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} to {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }
}
```

**CRITICAL: NO implicit conversions.** Implicit operators defeat type safety.

---

## Pattern Matching (C# 8-12)

```csharp
// Switch expressions with patterns
public string GetPaymentMethodDescription(PaymentMethod payment) => payment switch
{
    { Type: PaymentType.CreditCard, Last4: var last4 } => $"Credit card ending in {last4}",
    { Type: PaymentType.BankTransfer, AccountNumber: var account } => $"Bank transfer from {account}",
    { Type: PaymentType.Cash } => "Cash payment",
    _ => "Unknown payment method"
};

// Property patterns
public decimal CalculateDiscount(Order order) => order switch
{
    { Total: > 1000m } => order.Total * 0.15m,
    { Total: > 500m } => order.Total * 0.10m,
    { Total: > 100m } => order.Total * 0.05m,
    _ => 0m
};

// Relational and logical patterns
public string ClassifyTemperature(int temp) => temp switch
{
    < 0 => "Freezing",
    >= 0 and < 10 => "Cold",
    >= 10 and < 20 => "Cool",
    >= 20 and < 30 => "Warm",
    >= 30 => "Hot",
    _ => throw new ArgumentOutOfRangeException(nameof(temp))
};
```

---

## Nullable Reference Types (C# 8+)

```csharp
// Pattern matching with null checks
public decimal GetDiscount(Customer? customer) => customer switch
{
    null => 0m,
    { IsVip: true } => 0.20m,
    { OrderCount: > 10 } => 0.10m,
    _ => 0.05m
};

// Null-coalescing patterns
public string GetDisplayName(User? user) =>
    user?.PreferredName ?? user?.Email ?? "Guest";

// Guard clauses with ArgumentNullException.ThrowIfNull (C# 11+)
public void ProcessOrder(Order? order)
{
    ArgumentNullException.ThrowIfNull(order);
    // order is now non-nullable in this scope
}
```

---

## Async/Await Best Practices

```csharp
// Always accept CancellationToken
public async Task<Order> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
{
    Order order = await _repository.GetAsync(orderId, cancellationToken);
    Customer customer = await _customerService.GetCustomerAsync(order.CustomerId, cancellationToken);
    return order;
}

// ValueTask for cached/synchronous paths
public ValueTask<User?> GetUserAsync(UserId id)
{
    if (_cache.TryGetValue(id, out User? user))
    {
        return ValueTask.FromResult<User?>(user);  // No allocation
    }

    return new ValueTask<User?>(FetchUserAsync(id));
}

// IAsyncEnumerable for streaming
public async IAsyncEnumerable<Order> StreamOrdersAsync(
    string customerId,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (Order order in _repository.StreamAllAsync(cancellationToken))
    {
        if (order.CustomerId == customerId)
            yield return order;
    }
}
```

---

## Span<T> and Memory<T> for Zero-Allocation Code

```csharp
// Span<T> for synchronous, zero-allocation operations
public int ParseOrderId(ReadOnlySpan<char> input)
{
    if (!input.StartsWith("ORD-"))
        throw new FormatException("Invalid order ID format");

    ReadOnlySpan<char> numberPart = input.Slice(4);
    return int.Parse(numberPart);
}

// Memory<T> for async operations (Span can't cross await)
public async Task<int> ReadDataAsync(Memory<byte> buffer, CancellationToken cancellationToken)
{
    return await _stream.ReadAsync(buffer, cancellationToken);
}

// ArrayPool for temporary large buffers
public async Task ProcessLargeFileAsync(Stream stream, CancellationToken cancellationToken)
{
    byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
    try
    {
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            ProcessChunk(buffer.AsSpan(0, bytesRead));
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
```

---

## Result Type Pattern

For expected errors, use a `Result<T, TError>` type instead of exceptions.

```csharp
public readonly record struct Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;
    private readonly bool _isSuccess;

    private Result(TValue value) { _value = value; _error = default; _isSuccess = true; }
    private Result(TError error) { _value = default; _error = error; _isSuccess = false; }

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;

    public TValue Value => _isSuccess ? _value! : throw new InvalidOperationException("Cannot access Value of a failed result");
    public TError Error => !_isSuccess ? _error! : throw new InvalidOperationException("Cannot access Error of a successful result");

    public static Result<TValue, TError> Success(TValue value) => new (value);
    public static Result<TValue, TError> Failure(TError error) => new (error);

    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure)
        => _isSuccess ? onSuccess(_value!) : onFailure(_error!);
}
```

---

## Avoid Reflection-Based Metaprogramming

**Prefer statically-typed, explicit code over reflection-based "magic" libraries.**

| Library | Problem |
|---------|---------|
| **AutoMapper** | Reflection magic, hidden mappings, runtime failures |
| **Mapster** | Same issues as AutoMapper |

### Use Explicit Mapping Methods Instead

```csharp
public static class UserMappings
{
    public static UserDto ToDto(this UserEntity entity) => new (
        Id: entity.Id.ToString(),
        Name: entity.FullName,
        Email: entity.EmailAddress);
}

// Usage - explicit and traceable
UserDto dto = entity.ToDto();
```

---

## Anti-Patterns to Avoid

```csharp
// DON'T: Use mutable DTOs
public class CustomerDto { public string Id { get; set; } }  // BAD
public record CustomerDto(string Id, string Name);            // GOOD

// DON'T: Use classes for value objects
public class OrderId { public string Value { get; } }         // BAD
public readonly record struct OrderId(string Value);          // GOOD

// DON'T: Block on async code
Order result = GetOrderAsync().Result;  // DEADLOCK RISK!
Order result = await GetOrderAsync();   // GOOD

// DON'T: Forget CancellationToken
public async Task<Order> GetOrderAsync(OrderId id) { }                              // BAD
public async Task<Order> GetOrderAsync(OrderId id, CancellationToken ct = default)  // GOOD
```

---

## Attribution

This skill is adapted from [dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) by Aaron Stannard, licensed under Apache-2.0.
