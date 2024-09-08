namespace UnitTests.ConsoleDrivers.Windows.Interop;

using Terminal.Gui.ConsoleDrivers.Windows.Interop;

/// <summary>Method signature for binary operators returning bool on <see cref="BOOL"/>.</summary>
/// <remarks>
///     Will actually work on any static method on <see cref="BOOL"/> taking two contravariant types and returning bool,
///     but named this way for self-documenting code.
/// </remarks>
public delegate bool BinaryBoolOperator<in TLeft, in TRight> (ref BOOL instanceOfTypeOwningTheMethod, TLeft left, TRight right);

/// <summary>Method signature for cast operators on <see cref="BOOL"/>.</summary>
/// <remarks>
///     Will actually work on any static method on <see cref="BOOL"/> taking one contravariant type and returning a covariant type,
///     but named this way for self-documenting code.
/// </remarks>
public delegate TTo Cast<in TFrom, out TTo> (ref BOOL instanceOfTypeOwningTheMethod, TFrom from);
