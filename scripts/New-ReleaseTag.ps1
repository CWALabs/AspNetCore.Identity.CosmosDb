[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$VersionFile = "Directory.Build.props",
    [ValidateSet("Major", "Minor", "Patch", "None")]
    [string]$ChangeType = "None",
    [int]$ReleaseCandidateNumber,
    [string]$TagPrefix = "v",
    [string]$Remote = "origin",
    [string]$MainBranch = "main",
    [string]$Message,
    [switch]$Push,
    [switch]$NoPush,
    [switch]$PushBranch,
    [switch]$Lightweight,
    [switch]$AllowNonMainBranch
)

$ErrorActionPreference = "Stop"

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
    $repoVersionNodes = $xml.GetElementsByTagName("RepoVersion")
    if ($null -eq $repoVersionNodes -or $repoVersionNodes.Count -eq 0) {
        throw "No <RepoVersion> element was found in $Path"
    }

    $repoVersion = $repoVersionNodes.Item(0).InnerText

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
        [string]$Version
    )

    $repoVersionNodes = $Xml.GetElementsByTagName("RepoVersion")
    if ($null -eq $repoVersionNodes -or $repoVersionNodes.Count -eq 0) {
        throw "No <RepoVersion> node exists in $Path"
    }

    $repoVersionNodes.Item(0).InnerText = $Version
    $Xml.Save($Path)
}

function ConvertTo-VersionParts {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    if ($Version -notmatch '^(\d+)\.(\d+)\.(\d+)$') {
        throw "Version '$Version' must use numeric SemVer core format: major.minor.patch"
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
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [ValidateSet("Major", "Minor", "Patch", "None")]
        [string]$Bump
    )

    if ($Bump -eq "None") {
        return $Version
    }

    $parts = ConvertTo-VersionParts -Version $Version

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

function Read-PositiveInt {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prompt,
        [int]$Default = 1
    )

    while ($true) {
        $response = Read-Host "$Prompt [$Default]"
        if ([string]::IsNullOrWhiteSpace($response)) {
            return $Default
        }

        if ([int]::TryParse($response, [ref]$null)) {
            $value = [int]$response
            if ($value -gt 0) {
                return $value
            }
        }

        Write-Host "Please enter a whole number greater than zero."
    }
}

function Get-DefaultReleaseMessage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TagName,
        [Parameter(Mandatory = $true)]
        [string]$ChangeType
    )

    if ($ChangeType -eq "None") {
        return "Release $TagName"
    }

    return "$ChangeType release $TagName"
}

function Assert-CleanWorkingTree {
    $status = Invoke-GitCapture -Arguments @("status", "--short")
    if (-not [string]::IsNullOrWhiteSpace($status)) {
        throw "Working tree is not clean. Commit, stash, or discard changes before running the release script."
    }
}

function Assert-AllowedBranch {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExpectedBranch,
        [Parameter(Mandatory = $true)]
        [bool]$AllowOverride
    )

    if ($AllowOverride) {
        return
    }

    $currentBranch = Invoke-GitCapture -Arguments @("branch", "--show-current")
    if ([string]::IsNullOrWhiteSpace($currentBranch)) {
        throw "Release tagging is only allowed from '$ExpectedBranch' unless -AllowNonMainBranch is specified. Detached HEAD is not allowed by default."
    }

    if ($currentBranch -ne $ExpectedBranch) {
        throw "Release tagging is only allowed from '$ExpectedBranch'. Current branch is '$currentBranch'. Use -AllowNonMainBranch to override this safeguard."
    }
}

$repoRoot = Invoke-GitCapture -Arguments @("rev-parse", "--show-toplevel")
Set-Location -LiteralPath $repoRoot
Assert-CleanWorkingTree
Assert-AllowedBranch -ExpectedBranch $MainBranch -AllowOverride $AllowNonMainBranch

if ($Push -and $NoPush) {
    throw "Use either -Push or -NoPush, not both."
}

if ($ReleaseCandidateNumber -lt 0) {
    throw "Release candidate number must be zero or greater."
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
            $ChangeType = if ($isPatch) { "Patch" } else { "None" }
        }
    }

    $pushRequested = Read-YesNo -Prompt "Push the current branch and tag after creation?" -Default $true
    if ($pushRequested) {
        $Push = $true
        $PushBranch = $true
    }
    else {
        $NoPush = $true
    }

    $isReleaseCandidate = Read-YesNo -Prompt "Is this a release candidate?" -Default $false
    if ($isReleaseCandidate) {
        $ReleaseCandidateNumber = Read-PositiveInt -Prompt "Enter the release candidate number" -Default 1
    }
    else {
        $ReleaseCandidateNumber = 0
    }
}

$nextVersion = Get-IncrementedVersion -Version $currentVersion -Bump $ChangeType
$tagSuffix = if ($ReleaseCandidateNumber -gt 0) { "-rc$ReleaseCandidateNumber" } else { "" }
$tagName = "$TagPrefix$nextVersion$tagSuffix"

if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = Get-DefaultReleaseMessage -TagName $tagName -ChangeType $ChangeType
}

if ($interactiveMode) {
    Write-Host "Current version: $currentVersion"
    Write-Host "Next version:    $nextVersion"
    Write-Host "Tag:             $tagName"

    $useGeneratedMessage = Read-YesNo -Prompt "Use auto-generated message '$Message'?" -Default $true
    if (-not $useGeneratedMessage) {
        $customMessage = Read-Host "Enter the tag annotation message"
        if (-not [string]::IsNullOrWhiteSpace($customMessage)) {
            $Message = $customMessage.Trim()
        }
    }

    $runWhatIf = Read-YesNo -Prompt "Run in WhatIf mode?" -Default $false
    if ($runWhatIf) {
        $WhatIfPreference = $true
    }
}

$existingLocalTag = Invoke-GitCapture -Arguments @("tag", "--list", $tagName)
if (-not [string]::IsNullOrWhiteSpace($existingLocalTag)) {
    throw "Tag already exists locally: $tagName"
}

$remoteMatch = Invoke-GitCapture -Arguments @("ls-remote", "--tags", $Remote, $tagName)
if (-not [string]::IsNullOrWhiteSpace($remoteMatch)) {
    throw "Tag already exists on remote '$Remote': $tagName"
}

$versionChanged = $nextVersion -ne $currentVersion
if ($versionChanged) {
    if ($PSCmdlet.ShouldProcess($versionFilePath, "Update shared version to $nextVersion")) {
        Set-VersionConfiguration -Xml $versionConfig.Xml -Path $versionFilePath -Version $nextVersion
    }

    if ($PSCmdlet.ShouldProcess($versionFilePath, "Commit version update")) {
        Invoke-Git -Arguments @("add", "--", $VersionFile)
        Invoke-Git -Arguments @("commit", "-m", "Bump version to $nextVersion")
    }
}

if ($Lightweight) {
    if ($PSCmdlet.ShouldProcess($tagName, "Create lightweight tag")) {
        Invoke-Git -Arguments @("tag", $tagName)
    }
}
else {
    if ($PSCmdlet.ShouldProcess($tagName, "Create annotated tag")) {
        Invoke-Git -Arguments @("tag", "-a", $tagName, "-m", $Message)
    }
}

if ($PushBranch -or $Push) {
    if ($PSCmdlet.ShouldProcess($Remote, "Push current branch")) {
        Invoke-Git -Arguments @("push", $Remote, "HEAD")
    }
}

if ($Push) {
    if ($PSCmdlet.ShouldProcess("$Remote/$tagName", "Push tag")) {
        Invoke-Git -Arguments @("push", $Remote, $tagName)
    }
}

Write-Host "Current version: $currentVersion"
Write-Host "Next version: $nextVersion"
Write-Host "Tag: $tagName"
Write-Host "Annotated: $([bool](-not $Lightweight))"
Write-Host "Branch pushed: $([bool]($PushBranch -or $Push))"
Write-Host "Tag pushed: $([bool]$Push)"
Write-Host "WhatIf: $([bool]$WhatIfPreference)"