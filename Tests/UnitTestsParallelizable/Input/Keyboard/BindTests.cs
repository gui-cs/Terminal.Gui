namespace InputTests;

// Claude - Opus 4.6
public class BindTests
{
    [Fact]
    public void Bind_All_SingleKey_ReturnsPlatformKeyBinding ()
    {
        PlatformKeyBinding result = Bind.All ("CursorLeft");

        Assert.Equal (["CursorLeft"], result.All!);
        Assert.Null (result.Windows);
        Assert.Null (result.Linux);
        Assert.Null (result.Macos);
    }

    [Fact]
    public void Bind_All_MultipleKeys ()
    {
        PlatformKeyBinding result = Bind.All ("Home", "Ctrl+Home");

        Assert.Equal (["Home", "Ctrl+Home"], result.All!);
    }

    [Fact]
    public void Bind_AllPlus_NonWindowsKeys ()
    {
        PlatformKeyBinding result = Bind.AllPlus ("Delete", ["Ctrl+D"]);

        Assert.Equal (["Delete"], result.All!);
        Assert.Null (result.Windows);
        Assert.Equal (["Ctrl+D"], result.Linux!);
        Assert.Equal (["Ctrl+D"], result.Macos!);
    }

    [Fact]
    public void Bind_AllPlus_WindowsKeys ()
    {
        PlatformKeyBinding result = Bind.AllPlus ("X", windows: ["Ctrl+X"]);

        Assert.Equal (["X"], result.All!);
        Assert.Equal (["Ctrl+X"], result.Windows!);
        Assert.Null (result.Linux);
        Assert.Null (result.Macos);
    }

    [Fact]
    public void Bind_AllPlus_NullPlatforms_LeavesNull ()
    {
        PlatformKeyBinding result = Bind.AllPlus ("Delete");

        Assert.Equal (["Delete"], result.All!);
        Assert.Null (result.Windows);
        Assert.Null (result.Linux);
        Assert.Null (result.Macos);
    }

    [Fact]
    public void Bind_NonWindows_SetsLinuxAndMacos ()
    {
        PlatformKeyBinding result = Bind.NonWindows ("Ctrl+Z");

        Assert.Null (result.All);
        Assert.Null (result.Windows);
        Assert.Equal (["Ctrl+Z"], result.Linux!);
        Assert.Equal (["Ctrl+Z"], result.Macos!);
    }

    [Fact]
    public void Bind_Platform_LinuxOnly ()
    {
        PlatformKeyBinding result = Bind.Platform (linux: ["Ctrl+Z"]);

        Assert.Null (result.All);
        Assert.Null (result.Windows);
        Assert.Equal (["Ctrl+Z"], result.Linux!);
        Assert.Null (result.Macos);
    }

    [Fact]
    public void Bind_Platform_WindowsAndMacos ()
    {
        PlatformKeyBinding result = Bind.Platform (["Ctrl+Z"], macos: ["Cmd+Z"]);

        Assert.Null (result.All);
        Assert.Equal (["Ctrl+Z"], result.Windows!);
        Assert.Null (result.Linux);
        Assert.Equal (["Cmd+Z"], result.Macos!);
    }

    [Fact]
    public void Bind_Platform_AllNulls_ReturnsEmpty ()
    {
        PlatformKeyBinding result = Bind.Platform ();

        Assert.Null (result.All);
        Assert.Null (result.Windows);
        Assert.Null (result.Linux);
        Assert.Null (result.Macos);
    }

    [Fact]
    public void GetCurrentPlatformName_ReturnsValidName ()
    {
#pragma warning disable CS0618 // Obsolete
        string name = PlatformDetection.GetCurrentPlatformName ();
#pragma warning restore CS0618
        string [] validNames = ["windows", "linux", "macos"];

        Assert.Contains (name, validNames);
    }

    [Fact]
    public void GetCurrentPlatform_ReturnsValidPlatform ()
    {
        TuiPlatform platform = PlatformDetection.GetCurrentPlatform ();

        Assert.True (Enum.IsDefined (platform));
    }
}
