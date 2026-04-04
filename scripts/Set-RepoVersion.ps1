[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$VersionFile = "Directory.Build.props",
    [string]$Version,
    [ValidateSet("Major", "Minor", "Patch")]
    [string]$ChangeType,
    [string]$Message,
    [switch]$Commit,
    [switch]$NoCommit
)

$ErrorActionPreference = "Stop"

function Invoke-GitCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $result = & git @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }

    return ($result | Out-String).Trim()
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & git @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path -Path $Root -ChildPath $Path
}

function Get-VersionConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Version file not found: $Path"
    }

    [xml]$xml = Get-Content -LiteralPath $Path -Raw
    $repoVersion = @($xml.Project.PropertyGroup.RepoVersion) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($repoVersion)) {
        throw "No <RepoVersion> element was found in $Path"
    }

    return [pscustomobject]@{
        Xml = $xml
        RepoVersion = $repoVersion.Trim()
        Path = $Path
    }
}

function Set-VersionConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [xml]$Xml,
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$NewVersion
    )

    $repoVersionNode = $Xml.Project.PropertyGroup | Select-Object -First 1 | ForEach-Object { $_.RepoVersion }
    if (-not $repoVersionNode) {
        throw "No <RepoVersion> node exists in $Path"
    }

    $repoVersionNode = [System.Xml.XmlElement]$repoVersionNode
    $repoVersionNode.InnerText = $NewVersion
    $Xml.Save($Path)
}

function ConvertTo-VersionParts {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InputVersion
    )

    if ($InputVersion -notmatch '^(\d+)\.(\d+)\.(\d+)$') {
        throw "Version '$InputVersion' must use numeric SemVer core format: major.minor.patch"
    }

    return [pscustomobject]@{
        Major = [int]$matches[1]
        Minor = [int]$matches[2]
        Patch = [int]$matches[3]
    }
}

function Get-IncrementedVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InputVersion,
        [Parameter(Mandatory = $true)]
        [ValidateSet("Major", "Minor", "Patch")]
        [string]$Bump
    )

    $parts = ConvertTo-VersionParts -InputVersion $InputVersion

    switch ($Bump) {
        "Major" { return "{0}.0.0" -f ($parts.Major + 1) }
        "Minor" { return "{0}.{1}.0" -f $parts.Major, ($parts.Minor + 1) }
        "Patch" { return "{0}.{1}.{2}" -f $parts.Major, $parts.Minor, ($parts.Patch + 1) }
    }
}

function Read-YesNo {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prompt,
        [Parameter(Mandatory = $true)]
        [bool]$Default
    )

    $defaultText = if ($Default) { "Y/n" } else { "y/N" }

    while ($true) {
        $response = Read-Host "$Prompt [$defaultText]"
        if ([string]::IsNullOrWhiteSpace($response)) {
            return $Default
        }

        switch ($response.Trim().ToLowerInvariant()) {
            "y" { return $true }
            "yes" { return $true }
            "n" { return $false }
            "no" { return $false }
            default { Write-Host "Please answer yes or no." }
        }
    }
}

function Assert-CleanWorkingTree {
    $status = Invoke-GitCapture -Arguments @("status", "--short")
    if (-not [string]::IsNullOrWhiteSpace($status)) {
        throw "Working tree is not clean. Commit, stash, or discard changes before committing a version bump."
    }
}

$repoRoot = Invoke-GitCapture -Arguments @("rev-parse", "--show-toplevel")
Set-Location -LiteralPath $repoRoot
Assert-CleanWorkingTree

if ($Commit -and $NoCommit) {
    throw "Use either -Commit or -NoCommit, not both."
}

if ($Version -and $ChangeType) {
    throw "Specify either -Version or -ChangeType, not both."
}

$interactiveMode = $PSBoundParameters.Count -eq 0
$versionFilePath = Resolve-RepoPath -Root $repoRoot -Path $VersionFile
$versionConfig = Get-VersionConfiguration -Path $versionFilePath
$currentVersion = $versionConfig.RepoVersion

if ($interactiveMode) {
    $isMajor = Read-YesNo -Prompt "Are you making incompatible (breaking) API changes?" -Default $false
    if ($isMajor) {
        $ChangeType = "Major"
    }
    else {
        $isMinor = Read-YesNo -Prompt "Are you adding new functionality in a backward-compatible way?" -Default $false
        if ($isMinor) {
            $ChangeType = "Minor"
        }
        else {
            $isPatch = Read-YesNo -Prompt "Are you making backward-compatible bug fixes only?" -Default $true
            if (-not $isPatch) {
                throw "Interactive mode requires a major, minor, or patch bump."
            }

            $ChangeType = "Patch"
        }
    }

    $shouldCommit = Read-YesNo -Prompt "Commit the version change after updating it?" -Default $true
    if ($shouldCommit) {
        $Commit = $true
    }
    else {
        $NoCommit = $true
    }
}

if (-not $Version -and -not $ChangeType) {
    throw "Specify -Version or -ChangeType, or run with no arguments for interactive mode."
}

$nextVersion = if ($Version) { $Version.Trim() } else { Get-IncrementedVersion -InputVersion $currentVersion -Bump $ChangeType }
ConvertTo-VersionParts -InputVersion $nextVersion | Out-Null

if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = "Bump version to $nextVersion"
}

if ($interactiveMode) {
    Write-Host "Current version: $currentVersion"
    Write-Host "Next version:    $nextVersion"
}

if ($PSCmdlet.ShouldProcess($versionFilePath, "Update shared version to $nextVersion")) {
    Set-VersionConfiguration -Xml $versionConfig.Xml -Path $versionFilePath -NewVersion $nextVersion
}

$shouldCommitChange = $Commit -or (-not $NoCommit -and -not $interactiveMode)
if ($interactiveMode) {
    $shouldCommitChange = $Commit
}

if ($shouldCommitChange) {
    if ($PSCmdlet.ShouldProcess($versionFilePath, "Commit version update")) {
        Invoke-Git -Arguments @("add", "--", $VersionFile)
        Invoke-Git -Arguments @("commit", "-m", $Message)
    }
}

Write-Host "Current version: $currentVersion"
Write-Host "Next version: $nextVersion"
Write-Host "Committed: $([bool]$shouldCommitChange)"
Write-Host "WhatIf: $([bool]$WhatIfPreference)"