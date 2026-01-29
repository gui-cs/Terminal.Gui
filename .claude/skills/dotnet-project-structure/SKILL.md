---
name: dotnet-project-structure
description: Modern .NET project structure including Directory.Build.props, central package management, SourceLink, and SDK pinning. Reference for understanding Terminal.Gui's build configuration.
---

# .NET Project Structure and Build Configuration

## When to Use This Skill

Use this skill when:
- Understanding Terminal.Gui's build configuration
- Adding new projects to the solution
- Configuring centralized build properties
- Setting up SourceLink for debugging

---

## Terminal.Gui Project Structure

```
Terminal.Gui/
├── .claude/                        # AI agent guidance
│   ├── rules/                      # Coding rules
│   ├── skills/                     # Skills (this file)
│   ├── tasks/                      # Task checklists
│   └── workflows/                  # Workflow guides
├── .config/
│   └── dotnet-tools.json           # Local .NET tools
├── Terminal.Gui/                   # Core library
│   └── Terminal.Gui.csproj
├── Terminal.Gui.Analyzers/         # Roslyn analyzers
├── Tests/
│   ├── UnitTests/                  # Tests requiring static state
│   ├── UnitTestsParallelizable/    # Parallel-safe tests (preferred)
│   └── IntegrationTests/
├── Examples/
│   └── UICatalog/                  # Demo application
├── docfx/                          # Documentation
│   └── docs/
├── Directory.Build.props           # Centralized build config
├── Directory.Packages.props        # Central package versions
├── global.json                     # SDK version pinning
└── Terminal.sln                    # Solution file
```

---

## Directory.Build.props

Provides centralized build configuration for all projects.

### Key Settings in Terminal.Gui

```xml
<Project>
  <!-- C# Language Settings -->
  <PropertyGroup>
    <LangVersion>14</LangVersion>           <!-- C# 14 -->
    <Nullable>enable</Nullable>              <!-- Nullable reference types -->
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <!-- AOT/Trimming Compatibility -->
  <PropertyGroup>
    <IsTrimmable>true</IsTrimmable>
    <IsAotCompatible>true</IsAotCompatible>
  </PropertyGroup>

  <!-- SourceLink Configuration -->
  <PropertyGroup>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

### Reusable Target Framework Properties

```xml
<!-- Define once in Directory.Build.props -->
<PropertyGroup>
  <NetLibVersion>net10.0</NetLibVersion>
  <NetTestVersion>net10.0</NetTestVersion>
</PropertyGroup>

<!-- Reference in project files -->
<PropertyGroup>
  <TargetFramework>$(NetLibVersion)</TargetFramework>
</PropertyGroup>
```

---

## Central Package Management (CPM)

All package versions are centralized in `Directory.Packages.props`.

### How It Works

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
  </ItemGroup>
</Project>

<!-- Project files reference packages WITHOUT versions -->
<ItemGroup>
  <PackageReference Include="xunit" />
  <PackageReference Include="FluentAssertions" />
</ItemGroup>
```

### Benefits

1. **Single source of truth** - All versions in one file
2. **No version drift** - All projects use same versions
3. **Easy updates** - Change once, applies everywhere

---

## global.json - SDK Version Pinning

Ensures consistent builds across all environments.

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### Roll Forward Policies

| Policy | Behavior |
|--------|----------|
| `disable` | Exact version required |
| `patch` | Same major.minor, latest patch |
| `latestFeature` | Same major, latest feature band |
| `minor` | Same major, latest minor |

---

## SourceLink

Enables step-through debugging of Terminal.Gui from NuGet packages.

### Requirements

```xml
<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
</ItemGroup>
```

### How Users Benefit

Users can step into Terminal.Gui source code in their debugger:
1. Enable "Enable Source Link support" in Visual Studio
2. Disable "Enable Just My Code"
3. Step into Terminal.Gui methods - source loads automatically

---

## Adding a New Project

1. Create the project in the appropriate directory:
   ```bash
   dotnet new classlib -n Terminal.Gui.NewFeature -o Terminal.Gui.NewFeature
   ```

2. Add to solution:
   ```bash
   dotnet sln add Terminal.Gui.NewFeature/Terminal.Gui.NewFeature.csproj
   ```

3. The project automatically inherits:
   - Build properties from `Directory.Build.props`
   - Package versions from `Directory.Packages.props`
   - SDK version from `global.json`

4. Reference packages **without versions**:
   ```xml
   <ItemGroup>
     <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
   </ItemGroup>
   ```

---

## InternalsVisibleTo

Terminal.Gui exposes internals to test projects:

```xml
<!-- In Terminal.Gui.csproj -->
<ItemGroup>
  <InternalsVisibleTo Include="UnitTests" />
  <InternalsVisibleTo Include="UnitTests.Parallelizable" />
  <InternalsVisibleTo Include="IntegrationTests" />
</ItemGroup>
```

This allows tests to access `internal` members without making them `public`.

---

## Attribution

This skill is adapted from [dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) by Aaron Stannard, licensed under Apache-2.0.
