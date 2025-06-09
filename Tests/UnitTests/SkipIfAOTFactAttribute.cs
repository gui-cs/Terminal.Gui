namespace UnitTests;

public class SkipIfAOTFactAttribute : FactAttribute
{
    public SkipIfAOTFactAttribute ()
    {
#if AOT
        Skip = "Test skipped in AOT project due to Moq incompatibility.";
#endif
    }
}
