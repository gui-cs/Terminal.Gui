## CI/CD Workflows

The repository uses multiple GitHub Actions workflows. What runs and when:

> **Note:** Tests use xUnit v3 with Microsoft Testing Platform (MTP). The test runner is
> configured in `global.json` via `"test": { "runner": "Microsoft.Testing.Platform" }`.
> MTP runs test projects as standalone executables, so each OS must build its own binaries.

### 1) Build Validation (`.github/workflows/build-validation.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`)
- **Runner/timeout**: `ubuntu-latest`, 10 minutes
- **Steps**:
- Checkout and setup .NET 10.x GA
- `dotnet restore`
- Build Debug: `dotnet build --configuration Debug --no-restore -property:NoWarn=0618%3B0612`
- Build Release (library): `dotnet build Terminal.Gui/Terminal.Gui.csproj --configuration Release --no-incremental --force -property:NoWarn=0618%3B0612`
- Pack Release: `dotnet pack Terminal.Gui/Terminal.Gui.csproj --configuration Release --output ./local_packages -property:NoWarn=0618%3B0612`
- Restore NativeAot/SelfContained examples, then restore solution again
- Build Release for `Examples/NativeAot` and `Examples/SelfContained`
- Build Release solution

### 2) Build & Run Unit Tests (`.github/workflows/unit-tests.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`)
- **Matrix**: Ubuntu/Windows/macOS
- **Timeout**: 15 minutes (non-parallel), 60 minutes (parallel)
- **Process**:
1. Each OS checks out code, restores, and builds locally
2. **Performance optimizations**:
   - Disables Windows Defender on Windows runners (significant speedup)
3. Runs three test jobs:
   - **Non-parallel UnitTests.Legacy**: `Tests/UnitTests.Legacy` with diagnostic output
   - **Non-parallel UnitTests.NonParallelizable**: `Tests/UnitTests.NonParallelizable` with diagnostic output
   - **Parallel UnitTestsParallelizable**: `Tests/UnitTestsParallelizable` with diagnostic output
4. Uploads test logs and diagnostic data from all runners

**Test results**: All tests output to unified `TestResults/` directory at repository root

### 3) Build & Run Integration Tests (`.github/workflows/integration-tests.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`)
- **Matrix**: Ubuntu/Windows/macOS
- **Timeout**: 15 minutes
- **Process**:
1. Each OS checks out code, restores, and builds locally
2. **Performance optimizations**:
   - Disables Windows Defender on Windows runners
3. Runs IntegrationTests with diagnostic output
4. Uploads logs per-OS

### 4) Create Release (`.github/workflows/release.yml`)

- **Triggers**: `workflow_dispatch` (manual trigger from GitHub Actions UI)
- **Inputs**:
  - `release_type`: Choose from `prealpha`, `alpha`, `beta`, `rc`, or `stable`
  - `version_override`: (Optional) Specify exact version number, otherwise GitVersion calculates it
- **Process**:
  1. Checks out `v2_release` branch
  2. Determines version using GitVersion or override
  3. Creates annotated git tag (e.g., `v2.0.0-prealpha` or `v2.0.0`)
  4. Creates release commit with message
  5. Pushes tag and commit to repository
  6. Creates GitHub Release (marked as pre-release if not stable)
  7. Automatically triggers publish workflow (see below)
- **Purpose**: Automates the release process to prevent manual errors

### 5) Publish to NuGet (`.github/workflows/publish.yml`)

- **Triggers**: push to `v2_release`, `v2_develop`, and tags `v*`(ignores `**.md`)
- Uses GitVersion to compute SemVer, builds Release, packs with symbols, and pushes to NuGet.org using `NUGET_API_KEY`
- **Automatically triggered** by the Create Release workflow when a new tag is pushed
- **Additional actions on v2_release branch**:
  - Delists old NuGet packages to keep package list clean:
    - Keeps only the most recent `2.0.0-develop.*` package
    - Keeps only the just-published `2.0.0-alpha.*` or `2.0.0-beta.*` package
  - Triggers Terminal.Gui.templates repository update via repository_dispatch (requires `PAT_FOR_TEMPLATES` secret)

### 6) Build and publish API docs (`.github/workflows/api-docs.yml`)

- **Triggers**: push to `v1_release` and `v2_develop`
- Builds DocFX site on Windows and deploys to GitHub Pages when `ref_name` is `v2_release` or `v2_develop`


### Replicating CI Locally

```bash
# Full CI sequence:
dotnet restore
dotnet build --configuration Debug --no-restore
dotnet test --project Tests/UnitTests.Legacy --no-build --verbosity normal
dotnet test --project Tests/UnitTests.NonParallelizable --no-build --verbosity normal
dotnet test --project Tests/UnitTestsParallelizable --no-build --verbosity normal
dotnet build --configuration Release --no-restore
```
