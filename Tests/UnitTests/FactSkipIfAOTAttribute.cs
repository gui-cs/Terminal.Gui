namespace UnitTests;

public class FactSkipIfAOTAttribute : FactAttribute
{
    public FactSkipIfAOTAttribute ()
    {
        if (IsAOTEnvironment ())
        {
            base.Skip = "Test skipped in AOT project due to Moq incompatibility.";
        }
    }

    private static bool IsAOTEnvironment ()
    {
        return Type.GetType ("System.Runtime.CompilerServices.RuntimeFeature")?.GetProperty ("IsDynamicCodeSupported")?.GetValue (null) is bool and false;
    }
}
