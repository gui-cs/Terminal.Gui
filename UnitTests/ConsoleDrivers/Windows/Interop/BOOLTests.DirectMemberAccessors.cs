#nullable enable
namespace UnitTests.ConsoleDrivers.Windows.Interop;

using System.Runtime.CompilerServices;
using Terminal.Gui.ConsoleDrivers.Windows.Interop;

// These are direct accessors for methods defined on BOOL.
// These should be used in tests instead of the normal language constructs for accessing them,
// to guarantee that the intended members are called, avoiding implicit typ conversions by the compiler.
public partial class BOOLTests
{
    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Equality")]
    private static extern bool Get_op_Equality (ref BOOL justForTypeReference, BOOL left, BOOL right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Equality")]
    private static extern bool Get_op_Equality (ref BOOL justForTypeReference, BOOL left, bool right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Equality")]
    private static extern bool Get_op_Equality (ref BOOL justForTypeReference, bool left, BOOL right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Equality")]
    private static extern bool Get_op_Equality (ref BOOL justForTypeReference, BOOL left, int right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Equality")]
    private static extern bool Get_op_Equality (ref BOOL justForTypeReference, int left, BOOL right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Explicit")]
    private static extern BOOL Get_op_Explicit_FromBool (ref BOOL justForTypeReference, bool input);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Explicit")]
    private static extern bool Get_op_Explicit_ToBool (ref BOOL justForTypeReference, BOOL from);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Implicit")]
    private static extern BOOL Get_op_Implicit_FromInt (ref BOOL justForTypeReference, int input);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Implicit")]
    private static extern int Get_op_Implicit_ToInt (ref BOOL justForTypeReference, BOOL input);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Inequality")]
    private static extern bool Get_op_Inequality (ref BOOL justForTypeReference, BOOL left, BOOL right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Inequality")]
    private static extern bool Get_op_Inequality (ref BOOL justForTypeReference, BOOL left, bool right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Inequality")]
    private static extern bool Get_op_Inequality (ref BOOL justForTypeReference, BOOL left, int right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Inequality")]
    private static extern bool Get_op_Inequality (ref BOOL justForTypeReference, bool left, BOOL right);

    [UnsafeAccessor (UnsafeAccessorKind.StaticMethod, Name = "op_Inequality")]
    private static extern bool Get_op_Inequality (ref BOOL justForTypeReference, int left, BOOL right);

    [UnsafeAccessor (UnsafeAccessorKind.Method, Name = "get_IsFalse")]
    private static extern bool GetIsFalseProperty (ref BOOL value);

    [UnsafeAccessor (UnsafeAccessorKind.Method, Name = "get_IsTrue")]
    private static extern bool GetIsTrueProperty (ref BOOL value);
}
