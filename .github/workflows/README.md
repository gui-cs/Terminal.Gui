## CI/CD Workflows

The repository uses multiple GitHub Actions workflows. What runs and when:

### 1) Build Solution (`.github/workflows/build.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`); supports `workflow_call`
- **Runner/timeout**: `ubuntu-latest`, 10 minutes
- **Steps**:
- Checkout and setup .NET 8.x GA
- `dotnet restore`
- Build Debug: `dotnet build --configuration Debug --no-restore -property:NoWarn=0618%3B0612`
- Build Release (library): `dotnet build Terminal.Gui/Terminal.Gui.csproj --configuration Release --no-incremental --force -property:NoWarn=0618%3B0612`
- Pack Release: `dotnet pack Terminal.Gui/Terminal.Gui.csproj --configuration Release --output ./local_packages -property:NoWarn=0618%3B0612`
- Restore NativeAot/SelfContained examples, then restore solution again
- Build Release for `Examples/NativeAot` and `Examples/SelfContained`
- Build Release solution
- Upload artifacts named `build-artifacts`, retention 1 day

### 2) Build & Run Unit Tests (`.github/workflows/unit-tests.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`)
- **Matrix**: Ubuntu/Windows/macOS
- **Timeout**: 15 minutes per job
- **Process**:
1. Calls build workflow to build solution once
2. Downloads build artifacts
3. Runs `dotnet restore` (required for `--no-build` to work)
4. **Performance optimizations**:
   - Disables Windows Defender on Windows runners (significant speedup)
   - Collects code coverage **only on Linux** (ubuntu-latest) for performance
   - Windows and macOS skip coverage collection to reduce test time
   - Increased blame-hang-timeout to 120s for Windows/macOS (60s for Linux)
5. Runs two test jobs:
   - **Non-parallel UnitTests**: `Tests/UnitTests` with blame/diag flags; `xunit.stopOnFail=false`
   - **Parallel UnitTestsParallelizable**: `Tests/UnitTestsParallelizable` with blame/diag flags; `xunit.stopOnFail=false`
6. Uploads test logs and diagnostic data from all runners
7. **Uploads code coverage to Codecov only from Linux runner**

**Test results**: All tests output to unified `TestResults/` directory at repository root

### 3) Build & Run Integration Tests (`.github/workflows/integration-tests.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`)
- **Matrix**: Ubuntu/Windows/macOS
- **Timeout**: 15 minutes
- **Process**:
1. Calls build workflow
2. Downloads build artifacts
3. Runs `dotnet restore`
4. **Performance optimizations** (same as unit tests):
   - Disables Windows Defender on Windows runners
   - Collects code coverage **only on Linux**
   - Increased blame-hang-timeout to 120s for Windows/macOS
5. Runs IntegrationTests with blame/diag flags; `xunit.stopOnFail=true`
6. Uploads logs per-OS
7. **Uploads coverage to Codecov only from Linux runner**

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
dotnet test Tests/UnitTests --no-build --verbosity normal
dotnet test Tests/UnitTestsParallelizable --no-build --verbosity normal
dotnet build --configuration Release --no-restore
```
