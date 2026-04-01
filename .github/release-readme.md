# DotnetTinyRun Release Workflow

## Overview

This project uses a GitHub Actions workflow (`.github/workflows/release.yml`) to automate the release process. When the `<Version>` in `src/DotnetTinyRun/DotnetTinyRun.csproj` is bumped and pushed to the `main` branch, the workflow automatically:

1. Detects the version change
2. Builds AOT native binaries for Windows, Linux, and macOS
3. Publishes the NuGet tool package
4. Creates a GitHub Release with all artifacts attached

## How to Release

### Prerequisites

- The repository must have a `NUGET_API_KEY` secret configured in **Settings > Secrets and variables > Actions** for NuGet publishing.

### Steps

1. Update the version in `src/DotnetTinyRun/DotnetTinyRun.csproj`:

   ```xml
   <Version>1.1.0</Version>
   ```

2. Commit and push to `main`:

   ```bash
   git add src/DotnetTinyRun/DotnetTinyRun.csproj
   git commit -m "chore: bump version to 1.1.0"
   git push origin main
   ```

3. The workflow will automatically:
   - Create a git tag (e.g., `v1.1.0`)
   - Build AOT binaries for: `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`
   - Push the `.nupkg` to NuGet.org
   - Create a GitHub Release with all build artifacts

## Workflow Details

### Trigger

| Condition | Detail |
|-----------|--------|
| Branch | `main` |
| Path filter | `src/DotnetTinyRun/DotnetTinyRun.csproj` |

The workflow only runs when the `.csproj` file changes on `main`. It compares the new `<Version>` against the previous commit to determine if a release is needed.

### Jobs

| Job | Description |
|-----|-------------|
| `check-version` | Compares `<Version>` between the current and previous commit |
| `build-aot` | Builds native AOT binaries for 4 platform targets |
| `publish-nuget` | Packs and pushes the .NET tool to NuGet.org |
| `create-release` | Downloads all artifacts and creates a GitHub Release |

### Artifacts

Each release includes:

- `dotnet-tiny-run-win-x64.exe` — Windows x64 native binary
- `dotnet-tiny-run-linux-x64` — Linux x64 native binary
- `dotnet-tiny-run-osx-x64` — macOS x64 native binary
- `dotnet-tiny-run-osx-arm64` — macOS ARM64 native binary
- NuGet `.nupkg` package

## Installing as a .NET Tool

After the NuGet package is published:

```bash
dotnet tool install --global DotnetTinyRun
```

To update:

```bash
dotnet tool update --global DotnetTinyRun
```
