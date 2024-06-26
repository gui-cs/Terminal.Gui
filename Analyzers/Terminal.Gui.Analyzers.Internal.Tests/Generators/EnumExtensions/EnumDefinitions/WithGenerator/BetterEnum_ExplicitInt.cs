using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui.Analyzers.Internal.Tests.Generators.EnumExtensions.EnumDefinitions;

/// <summary>
///     Same as <see cref="BasicEnum_ExplicitInt"/>, but with <see cref="GenerateEnumExtensionMethodsAttribute"/> applied.
/// </summary>
[GenerateEnumExtensionMethods]
[SuppressMessage ("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Naming is intentional.")]
[SuppressMessage ("Roslynator", "RCS1154:Sort enum members", Justification = "Order is intentional.")]
public enum BetterEnum_ExplicitInt
{
    Bit31 = BasicEnum_ExplicitInt.Bit31,
    Bit30 = BasicEnum_ExplicitInt.Bit30,
    Bit29 = BasicEnum_ExplicitInt.Bit29,
    Bit28 = BasicEnum_ExplicitInt.Bit28,
    Bit27 = BasicEnum_ExplicitInt.Bit27,
    Bit26 = BasicEnum_ExplicitInt.Bit26,
    Bit25 = BasicEnum_ExplicitInt.Bit25,
    Bit24 = BasicEnum_ExplicitInt.Bit24,
    Bit23 = BasicEnum_ExplicitInt.Bit23,
    Bit22 = BasicEnum_ExplicitInt.Bit22,
    Bit21 = BasicEnum_ExplicitInt.Bit21,
    Bit20 = BasicEnum_ExplicitInt.Bit20,
    Bit19 = BasicEnum_ExplicitInt.Bit19,
    Bit18 = BasicEnum_ExplicitInt.Bit18,
    Bit17 = BasicEnum_ExplicitInt.Bit17,
    Bit16 = BasicEnum_ExplicitInt.Bit16,
    Bit15 = BasicEnum_ExplicitInt.Bit15,
    Bit14 = BasicEnum_ExplicitInt.Bit14,
    Bit13 = BasicEnum_ExplicitInt.Bit13,
    Bit12 = BasicEnum_ExplicitInt.Bit12,
    Bit11 = BasicEnum_ExplicitInt.Bit11,
    Bit10 = BasicEnum_ExplicitInt.Bit10,
    Bit09 = BasicEnum_ExplicitInt.Bit09,
    Bit08 = BasicEnum_ExplicitInt.Bit08,
    Bit07 = BasicEnum_ExplicitInt.Bit07,
    Bit06 = BasicEnum_ExplicitInt.Bit06,
    Bit05 = BasicEnum_ExplicitInt.Bit05,
    Bit04 = BasicEnum_ExplicitInt.Bit04,
    Bit03 = BasicEnum_ExplicitInt.Bit03,
    Bit02 = BasicEnum_ExplicitInt.Bit02,
    Bit01 = BasicEnum_ExplicitInt.Bit01,
    Bit00 = BasicEnum_ExplicitInt.Bit00,
    All_0 = BasicEnum_ExplicitInt.All_0,
    All_1 = BasicEnum_ExplicitInt.All_1,
    Alternating_01 = BasicEnum_ExplicitInt.Alternating_01,
    Alternating_10 = BasicEnum_ExplicitInt.Alternating_10,
    EvenBytesHigh = BasicEnum_ExplicitInt.EvenBytesHigh,
    OddBytesHigh = BasicEnum_ExplicitInt.OddBytesHigh
}
