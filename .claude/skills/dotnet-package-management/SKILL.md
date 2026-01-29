---
name: dotnet-package-management
description: Manage NuGet packages using Central Package Management (CPM) and dotnet CLI commands. Never edit XML directly - use dotnet add/remove/list commands.
---

# NuGet Package Management

## When to Use This Skill

Use this skill when:
- Adding, removing, or updating NuGet packages
- Managing package versions across Terminal.Gui projects
- Troubleshooting package conflicts or restore issues

---

## Golden Rule: Never Edit XML Directly

**Always use `dotnet` CLI commands to manage packages.**

```bash
# DO: Use CLI commands
dotnet add package Newtonsoft.Json
dotnet remove package Newtonsoft.Json
dotnet list package --outdated

# DON'T: Edit XML directly
# <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

**Why:**
- CLI validates package exists and resolves correct version
- Handles transitive dependencies correctly
- Works correctly with CPM
- Avoids typos and malformed XML

---

## Central Package Management (CPM)

Terminal.Gui uses CPM - all package versions are in `Directory.Packages.props`.

### Adding Packages

```bash
# Adds to Directory.Packages.props AND project file
dotnet add package Serilog

# Result in Directory.Packages.props:
# <PackageVersion Include="Serilog" Version="4.0.0" />

# Result in project file:
# <PackageReference Include="Serilog" />
```

### Project Files with CPM

Projects reference packages **without versions**:

```xml
<!-- Terminal.Gui.csproj -->
<ItemGroup>
  <PackageReference Include="ColorHelper" />
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  <PackageReference Include="System.IO.Abstractions" />
</ItemGroup>
```

---

## CLI Command Reference

### Adding Packages

```bash
# Add latest stable version
dotnet add package Serilog

# Add specific version
dotnet add package Serilog --version 4.0.0

# Add prerelease
dotnet add package Serilog --prerelease

# Add to specific project
dotnet add Terminal.Gui/Terminal.Gui.csproj package Serilog
```

### Removing Packages

```bash
# Remove from current project
dotnet remove package Serilog

# Remove from specific project
dotnet remove Terminal.Gui/Terminal.Gui.csproj package Serilog
```

### Listing Packages

```bash
# List all packages in solution
dotnet list package

# Show outdated packages
dotnet list package --outdated

# Include transitive dependencies
dotnet list package --include-transitive

# Show vulnerable packages
dotnet list package --vulnerable

# Show deprecated packages
dotnet list package --deprecated
```

### Restore and Clean

```bash
# Restore packages
dotnet restore

# Clear local cache (troubleshooting)
dotnet nuget locals all --clear

# Force restore (ignore cache)
dotnet restore --force
```

---

## Troubleshooting

### Version Conflicts

```bash
# See full dependency tree
dotnet list package --include-transitive

# Find what's pulling in a specific package
dotnet list package --include-transitive | grep -i "PackageName"
```

### Restore Failures

```bash
# Clear all caches
dotnet nuget locals all --clear

# Restore with detailed logging
dotnet restore --verbosity detailed
```

---

## Shared Version Variables

For related packages, use shared version variables in `Directory.Packages.props`:

```xml
<PropertyGroup>
  <XunitVersion>2.9.3</XunitVersion>
</PropertyGroup>

<ItemGroup>
  <PackageVersion Include="xunit" Version="$(XunitVersion)" />
  <PackageVersion Include="xunit.runner.visualstudio" Version="$(XunitVersion)" />
</ItemGroup>
```

**Benefits:**
- Update all related packages by changing one variable
- Prevents version mismatches

---

## Anti-Patterns

### Don't: Edit XML Directly

```xml
<!-- BAD: Manual XML editing -->
<PackageReference Include="Typo.Package" Version="1.0.0" />
<!-- Package might not exist! CLI would catch this. -->
```

### Don't: Inline Versions with CPM

```xml
<!-- BAD: Bypasses CPM -->
<PackageReference Include="Serilog" Version="4.0.0" />

<!-- GOOD: Version comes from Directory.Packages.props -->
<PackageReference Include="Serilog" />
```

### Don't: Forget Shared Variables for Related Packages

```xml
<!-- BAD: Related packages with different versions -->
<PackageVersion Include="xunit" Version="2.9.3" />
<PackageVersion Include="xunit.runner.visualstudio" Version="2.9.2" />  <!-- Mismatch! -->

<!-- GOOD: Use shared variable -->
<PackageVersion Include="xunit" Version="$(XunitVersion)" />
<PackageVersion Include="xunit.runner.visualstudio" Version="$(XunitVersion)" />
```

---

## Quick Reference

| Task | Command |
|------|---------|
| Add package | `dotnet add package <name>` |
| Add specific version | `dotnet add package <name> --version <ver>` |
| Remove package | `dotnet remove package <name>` |
| List packages | `dotnet list package` |
| Show outdated | `dotnet list package --outdated` |
| Show vulnerable | `dotnet list package --vulnerable` |
| Restore | `dotnet restore` |
| Clear cache | `dotnet nuget locals all --clear` |

---

## Attribution

This skill is adapted from [dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) by Aaron Stannard, licensed under Apache-2.0.
