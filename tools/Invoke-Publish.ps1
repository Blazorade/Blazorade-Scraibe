<#
.SYNOPSIS
    Publishes the Blazorade website by converting /content Markdown files to static HTML.

.DESCRIPTION
    Reads blazorade.config.md from the repository root, builds the component library,
    then delegates the full publish pipeline to tools/Scraibe.Publisher.

    The agent only needs to call this script — no HTML is generated in the chat context.

.PARAMETER Configuration
    Build configuration for the component library. Defaults to Debug.

.EXAMPLE
    .\tools\Invoke-Publish.ps1
    .\tools\Invoke-Publish.ps1 -Configuration Release
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot

# ── Read blazorade.config.md ─────────────────────────────────────────────────

$configFile = Join-Path $RepoRoot 'blazorade.config.md'
if (-not (Test-Path $configFile)) {
    Write-Error "blazorade.config.md not found at '$configFile'. Run first-run setup first."
}

$configContent = Get-Content $configFile -Raw

function Get-ConfigValue([string]$key) {
    if ($configContent -match "- ``$key``:\s*(.+)") { return $Matches[1].Trim() }
    if ($configContent -match "- `$key`:\s*(.+)")    { return $Matches[1].Trim() }
    if ($configContent -match "\b$key\b[`:]\s*(.+)") { return $Matches[1].Trim() }
    return $null
}

$DisplayName          = Get-ConfigValue 'DisplayName'
$HostName             = Get-ConfigValue 'HostName'
$WebAppPath           = Get-ConfigValue 'WebAppPath'
$ComponentLibraryPath = Get-ConfigValue 'ComponentLibraryPath'

if (-not $DisplayName -or -not $HostName -or -not $WebAppPath -or -not $ComponentLibraryPath) {
    Write-Error "Could not parse all required values from blazorade.config.md. " +
        "Expected: DisplayName, HostName, WebAppPath, ComponentLibraryPath."
}

# Derive ComponentLibraryName from the last segment of ComponentLibraryPath
$ComponentLibraryName = Split-Path -Leaf $ComponentLibraryPath

Write-Host "Site         : $DisplayName ($HostName)"
Write-Host "Web app      : $WebAppPath"
Write-Host "Components   : $ComponentLibraryPath ($ComponentLibraryName)"

# ── Parse excluded content paths ─────────────────────────────────────────────

$ExcludedArgs = @()
$inExcludedSection = $false
foreach ($line in ($configContent -split "`n")) {
    if ($line -match '##\s+Excluded Content') { $inExcludedSection = $true; continue }
    if ($inExcludedSection -and $line -match '^##') { $inExcludedSection = $false; break }
    if ($inExcludedSection -and $line -match '^\s*[-*]\s+`?([^`\s]+)`?') {
        $ExcludedArgs += '--excluded'
        $ExcludedArgs += $Matches[1].Trim()
    }
}

# ── Build component library ──────────────────────────────────────────────────

$componentCsproj = Join-Path $RepoRoot "$ComponentLibraryPath/$ComponentLibraryName.csproj"
if (-not (Test-Path $componentCsproj)) {
    # Try without the extra segment
    $componentCsproj = Join-Path $RepoRoot "$ComponentLibraryPath.csproj"
}

Write-Host "`nBuilding component library ($Configuration)..."
dotnet build $componentCsproj -c $Configuration --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed. Fix build errors and re-run."
}

# Locate the compiled assembly
$assemblyPath = Join-Path $RepoRoot `
    "$ComponentLibraryPath/bin/$Configuration/net10.0/$ComponentLibraryName.dll"

if (-not (Test-Path $assemblyPath)) {
    Write-Error "Compiled assembly not found at '$assemblyPath'."
}

# ── Resolve paths ─────────────────────────────────────────────────────────────

$contentPath   = Join-Path $RepoRoot 'content'
$outputPath    = Join-Path $RepoRoot "$WebAppPath/wwwroot"
$templatePath  = Join-Path $RepoRoot "$WebAppPath/page-template.html"
$layoutsPath   = Join-Path $RepoRoot "$ComponentLibraryPath/wwwroot/Layouts"
$namespace     = "$ComponentLibraryName.ShortCodes"

# ── Run the publisher ─────────────────────────────────────────────────────────

Write-Host "`nRunning publish pipeline..."
$publisherProject = Join-Path $PSScriptRoot 'Scraibe.Publisher/Scraibe.Publisher.csproj'

$publishArgs = @(
    'run', '--project', $publisherProject, '--',
    '--content',             $contentPath,
    '--output',              $outputPath,
    '--host',                $HostName,
    '--display-name',        $DisplayName,
    '--template',            $templatePath,
    '--assembly',            $assemblyPath,
    '--component-namespace', $namespace,
    '--layouts',             $layoutsPath
) + $ExcludedArgs

dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish pipeline reported errors (exit code $LASTEXITCODE)."
}

Write-Host "`nPublish complete." -ForegroundColor Green
