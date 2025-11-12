using Xunit;

namespace UnitTests_Parallelizable;

/// <summary>
/// Fact attribute that skips the test if not running on Windows.
/// </summary>
public sealed class SkipOnNonWindowsFactAttribute : FactAttribute
{
    public SkipOnNonWindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Test requires Windows platform";
        }
    }
}

/// <summary>
/// Theory attribute that skips the test if not running on Windows.
/// </summary>
public sealed class SkipOnNonWindowsTheoryAttribute : TheoryAttribute
{
    public SkipOnNonWindowsTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Test requires Windows platform";
        }
    }
}
