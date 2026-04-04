# Release Scripts

This folder contains local PowerShell helpers for keeping solution versions aligned and creating release tags.

All projects in the solution now inherit their shared version from [Directory.Build.props](../Directory.Build.props).

## Scripts

### `New-ReleaseTag.ps1`

Bumps the shared solution version, commits the version change, creates a git tag, and optionally pushes the branch and tag.

If you run it with no arguments, it prompts for:

- breaking, minor, or patch release type
- whether to push
- whether the release is an RC
- annotation message
- whether to run in `WhatIf`

Examples:

```powershell
# Interactive mode
.\scripts\New-ReleaseTag.ps1

# Preview a patch release without changing anything
.\scripts\New-ReleaseTag.ps1 -ChangeType Patch -NoPush -WhatIf

# Create and push a minor release tag
.\scripts\New-ReleaseTag.ps1 -ChangeType Minor -Push

# Create and push a release candidate tag
.\scripts\New-ReleaseTag.ps1 -ChangeType Patch -ReleaseCandidateNumber 1 -Push

# Create a major release tag with a custom message
.\scripts\New-ReleaseTag.ps1 -ChangeType Major -Message "Major release v13.0.0" -Push
```

Typical outputs:

- version bump commit: `Bump version to x.y.z`
- stable tag: `vx.y.z`
- release candidate tag: `vx.y.z-rcN`

### `Set-RepoVersion.ps1`

Updates the shared solution version in [Directory.Build.props](../Directory.Build.props) without creating a tag.

Use this when you want to bump or set the version independently of the release workflow.

Examples:

```powershell
# Interactive mode
.\scripts\Set-RepoVersion.ps1

# Preview a patch bump without changing anything
.\scripts\Set-RepoVersion.ps1 -ChangeType Patch -NoCommit -WhatIf

# Bump the shared version as a minor change and commit it
.\scripts\Set-RepoVersion.ps1 -ChangeType Minor -Commit

# Set an explicit version and commit it
.\scripts\Set-RepoVersion.ps1 -Version 12.1.0 -Commit

# Set an explicit version without committing
.\scripts\Set-RepoVersion.ps1 -Version 12.1.1 -NoCommit
```

## Notes

- Both scripts are designed to run from the repository root, but they resolve paths safely if launched from elsewhere.
- `New-ReleaseTag.ps1` requires a clean working tree for non-`WhatIf` runs because it creates a version bump commit and tag.
- `Set-RepoVersion.ps1` requires a clean working tree only when it is going to commit the version change.
- Annotated tags are the default for releases.
