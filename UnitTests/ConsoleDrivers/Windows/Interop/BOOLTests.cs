#nullable enable
using System.Runtime.CompilerServices;
using Terminal.Gui.ConsoleDrivers.Windows.Interop;

namespace UnitTests.ConsoleDrivers.Windows.Interop;

// ReSharper disable once InconsistentNaming
// ReSharper disable once MemberCanBeFileLocal
[Trait ("Category", "Interop")]
public partial class BOOLTests
{
    public static IEnumerable<object []> CastOperatorTestData
    {
        get
        {
            // Must cast these to a specific delegate type or the implicit method group conversion will make a run-time type for them
            // that we can't cast to a usable typed delegate in the test method.
            yield return [(Cast<BOOL, int>)Get_op_Implicit_ToInt, new BOOL (true), -1];
            yield return [(Cast<BOOL, int>)Get_op_Implicit_ToInt, new BOOL (false), 0];
            yield return [(Cast<int, BOOL>)Get_op_Implicit_FromInt, int.MinValue, new BOOL (true)];
            yield return [(Cast<int, BOOL>)Get_op_Implicit_FromInt, -1, new BOOL (true)];
            yield return [(Cast<int, BOOL>)Get_op_Implicit_FromInt, 1, new BOOL (true)];
            yield return [(Cast<int, BOOL>)Get_op_Implicit_FromInt, int.MaxValue, new BOOL (true)];
            yield return [(Cast<int, BOOL>)Get_op_Implicit_FromInt, 0, new BOOL (false)];
            yield return [(Cast<BOOL, bool>)Get_op_Explicit_ToBool, new BOOL (true), true];
            yield return [(Cast<BOOL, bool>)Get_op_Explicit_ToBool, new BOOL (false), false];
            yield return [(Cast<bool, BOOL>)Get_op_Explicit_FromBool, true, new BOOL (true)];
            yield return [(Cast<bool, BOOL>)Get_op_Explicit_FromBool, false, new BOOL (false)];
        }
    }

    /// <summary>
    ///     This test explicitly calls each of the op_Explicit or op_Implicit cast operators defined in
    ///     <see cref="CastOperatorTestData"/> and verifies the results of the operation are as expected.
    /// </summary>
    /// <typeparam name="TFrom">The type being cast to <typeparamref name="TTo"/>.</typeparam>
    /// <typeparam name="TTo">The resulting type of the cast.</typeparam>
    /// <param name="cast">A delegate of type <see cref="Cast{TFrom,TTo}"/> that represents the cast operator.</param>
    /// <param name="fromValue">The value to cast to <typeparamref name="TTo"/>.</param>
    /// <param name="expectedResult">The expected resulting value of the cast.</param>
    /// <remarks>
    ///     This validates not only that the cast works as expected, but that it exists and is defined exactly as the referenced method.
    ///     <br/>
    ///     This also ensures that implicit type conversion does not happen, as would be possible if the casts were tested by normal
    ///     means.<br/>
    ///     Besides, directly casting isn't an option with unconstrained type arguments anyway.
    /// </remarks>
    [Theory]
    [MemberData (nameof (CastOperatorTestData))]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    [Trait ("Category", "Operators")]
    [SkipLocalsInit]
    public Task Casts_CorrectlyDefinedAndReturnExpectedResult<TFrom, TTo> (
        Cast<TFrom, TTo> cast,
        TFrom fromValue,
        TTo expectedResult
    )
    {
        Unsafe.SkipInit (out BOOL justForReference);
        Assert.Equal (expectedResult, cast (ref justForReference, fromValue));

        return Task.CompletedTask;
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Operators")]
    public Task CompareTo_BOOL_ProperOrdering ([CombinatorialRange (-1, 1, 1)] int left, [CombinatorialRange (-1, 1, 1)] int right)
    {
        int expected = (left != 0).CompareTo (right != 0);
        Assert.Equal (expected, new BOOL (left).CompareTo (new BOOL (right)));
        expected = (right != 0).CompareTo (left != 0);
        Assert.Equal (expected, new BOOL (right).CompareTo (new BOOL (left)));

        return Task.CompletedTask;
    }

    [Theory]
    [CombinatorialData]
    public Task Constructor_Boolean_KnowsTheTruth (bool value)
    {
        BOOL testBOOL = new (value);
        Assert.Equal (value, GetIsTrueProperty (ref testBOOL));

        return Task.CompletedTask;
    }

    [Theory]
    [CombinatorialData]
    public Task Constructor_Int32_KnowsTheTruth ([CombinatorialValues (int.MinValue, -1, 0, 1, int.MaxValue)] int value)
    {
        bool intTruth = value != 0;
        BOOL testBOOL = new (value);
        Assert.Equal (intTruth, GetIsTrueProperty (ref testBOOL));

        return Task.CompletedTask;
    }

    [Theory]
    [MemberData (nameof (EqualityOperatorTestData))]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    [Trait ("Category", "Operators")]
    [SkipLocalsInit]
    public Task EqualityOperators_ReturnExpectedResult<TLeft, TRight> (
        BinaryBoolOperator<TLeft, TRight> equalityOperator,
        TLeft left,
        TRight right,
        bool expectedResult
    )
    {
        Unsafe.SkipInit (out BOOL justForReference);
        Assert.Equal (expectedResult, equalityOperator (ref justForReference, left, right));

        return Task.CompletedTask;
    }

    public static IEnumerable<object []> EqualityOperatorTestData
    {
        get
        {
            // BOOL(true) == x
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Equality, new BOOL (true), new BOOL (true), true];
            yield return [(BinaryBoolOperator<BOOL, bool>)Get_op_Equality, new BOOL (true), true, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (true), int.MinValue, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (true), -1, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (true), 1, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (true), int.MaxValue, true];
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Equality, new BOOL (true), new BOOL (false), false];
            yield return [(BinaryBoolOperator<BOOL, bool>)Get_op_Equality, new BOOL (true), false, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (true), 0, false];

            // BOOL(false) == x
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Equality, new BOOL (false), new BOOL (true), false];
            yield return [(BinaryBoolOperator<BOOL, bool>)Get_op_Equality, new BOOL (false), true, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (false), int.MinValue, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (false), -1, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (false), 1, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (false), int.MaxValue, false];
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Equality, new BOOL (false), new BOOL (false), true];
            yield return [(BinaryBoolOperator<BOOL, bool>)Get_op_Equality, new BOOL (false), false, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Equality, new BOOL (false), 0, true];

            // x == BOOL(true)
            yield return [(BinaryBoolOperator<bool, BOOL>)Get_op_Equality, true, new BOOL (true), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, int.MinValue, new BOOL (true), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, -1, new BOOL (true), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, 1, new BOOL (true), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, int.MaxValue, new BOOL (true), true];
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Equality, new BOOL (false), new BOOL (true), false];
            yield return [(BinaryBoolOperator<bool, BOOL>)Get_op_Equality, false, new BOOL (true), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, 0, new BOOL (true), false];

            // x == BOOL(false)
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Equality, new BOOL (true), new BOOL (false), false];
            yield return [(BinaryBoolOperator<bool, BOOL>)Get_op_Equality, true, new BOOL (false), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, int.MinValue, new BOOL (false), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, -1, new BOOL (false), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, 1, new BOOL (false), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, int.MaxValue, new BOOL (false), false];
            yield return [(BinaryBoolOperator<bool, BOOL>)Get_op_Equality, false, new BOOL (false), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Equality, 0, new BOOL (false), true];

            // BOOL(true) != x
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Inequality, new BOOL (true), new BOOL (true), false];
            yield return [(BinaryBoolOperator<BOOL, bool>)Get_op_Inequality, new BOOL (true), true, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (true), int.MinValue, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (true), -1, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (true), 1, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (true), int.MaxValue, false];
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Inequality, new BOOL (true), new BOOL (false), true];
            yield return [(BinaryBoolOperator<BOOL, bool>)Get_op_Inequality, new BOOL (true), false, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (true), 0, true];

            // BOOL(false) != x
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Inequality, new BOOL (false), new BOOL (true), true];
            yield return [(BinaryBoolOperator<BOOL, bool>)Get_op_Inequality, new BOOL (false), true, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (false), int.MinValue, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (false), -1, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (false), 1, true];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (false), int.MaxValue, true];
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Inequality, new BOOL (false), new BOOL (false), false];
            yield return [(BinaryBoolOperator<BOOL, bool>)Get_op_Inequality, new BOOL (false), false, false];
            yield return [(BinaryBoolOperator<BOOL, int>)Get_op_Inequality, new BOOL (false), 0, false];

            // x != BOOL(true)
            yield return [(BinaryBoolOperator<bool, BOOL>)Get_op_Inequality, true, new BOOL (true), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, int.MinValue, new BOOL (true), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, -1, new BOOL (true), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, 1, new BOOL (true), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, int.MaxValue, new BOOL (true), false];
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Inequality, new BOOL (false), new BOOL (true), true];
            yield return [(BinaryBoolOperator<bool, BOOL>)Get_op_Inequality, false, new BOOL (true), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, 0, new BOOL (true), true];

            // x != BOOL(false)
            yield return [(BinaryBoolOperator<BOOL, BOOL>)Get_op_Inequality, new BOOL (true), new BOOL (false), true];
            yield return [(BinaryBoolOperator<bool, BOOL>)Get_op_Inequality, true, new BOOL (false), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, int.MinValue, new BOOL (false), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, -1, new BOOL (false), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, 1, new BOOL (false), true];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, int.MaxValue, new BOOL (false), true];
            yield return [(BinaryBoolOperator<bool, BOOL>)Get_op_Inequality, false, new BOOL (false), false];
            yield return [(BinaryBoolOperator<int, BOOL>)Get_op_Inequality, 0, new BOOL (false), false];
        }
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    [Trait ("Category", "Operators")]
    public void IsTrue_IsFalse_AreOpposite (bool value)
    {
        BOOL testBOOL = new (value);
        Assume.Equal (value, GetIsTrueProperty (ref testBOOL));
        Assume.NotEqual (value, GetIsFalseProperty (ref testBOOL));

        Assert.NotEqual (GetIsTrueProperty (ref testBOOL), GetIsFalseProperty (ref testBOOL));
    }
}
