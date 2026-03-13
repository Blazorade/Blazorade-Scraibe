<#
.SYNOPSIS
    Publishes the Blazorade website by converting /content Markdown files to static HTML.

.DESCRIPTION
    Reads .config.json from the repository root, builds the component library,
    then delegates the full publish pipeline to tools/Scraibe.Publisher.

    The agent only needs to call this script — no HTML is generated in the chat context.

.PARAMETER Configuration
    Build configuration for the component library. Defaults to Debug.

.PARAMETER Pages
    Comma-separated list of content-relative .md paths to publish selectively (e.g. "home.md,scraibe-docs/mermaid.md").
    When omitted or empty, a full publish is performed.

.EXAMPLE
    .\tools\Invoke-Publish.ps1
    .\tools\Invoke-Publish.ps1 -Configuration Release
    .\tools\Invoke-Publish.ps1 -Pages home.md,scraibe-docs/mermaid.md
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Debug',
    [string] $Pages = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot

# ── Read .config.json (upgrade legacy blazorade.config.md if needed) ────────

$jsonConfigFile = Join-Path $RepoRoot '.config.json'
$legacyConfigFile = Join-Path $RepoRoot 'blazorade.config.md'

function Get-LegacyConfigValue([string]$content, [string]$key) {
    if ($content -match "- ``$key``:\s*(.+)") { return $Matches[1].Trim() }
    if ($content -match "- `$key`:\s*(.+)")    { return $Matches[1].Trim() }
    if ($content -match "\b$key\b[`:]\s*(.+)") { return $Matches[1].Trim() }
    return $null
}

function Get-LegacyExcludedContent([string]$content) {
    $items = @()
    $inExcludedSection = $false

    foreach ($line in ($content -split "`n")) {
        if ($line -match '##\s+Excluded Content') { $inExcludedSection = $true; continue }
        if ($inExcludedSection -and $line -match '^##') { $inExcludedSection = $false; break }
        if ($inExcludedSection -and $line -match '^\s*[-*]\s+`?([^`\s]+)`?') {
            $items += $Matches[1].Trim()
        }
    }

    return $items
}

if (-not (Test-Path $jsonConfigFile)) {
    if (-not (Test-Path $legacyConfigFile)) {
        Write-Error ".config.json not found at '$jsonConfigFile'. Run first-run setup first."
    }

    Write-Host "Legacy configuration detected (blazorade.config.md). Upgrading to .config.json..."
    $legacyContent = Get-Content $legacyConfigFile -Raw

    $legacyDisplayName = Get-LegacyConfigValue $legacyContent 'DisplayName'
    $legacyAppName = Get-LegacyConfigValue $legacyContent 'AppName'
    $legacyHostName = Get-LegacyConfigValue $legacyContent 'HostName'
    $legacyWebAppPath = Get-LegacyConfigValue $legacyContent 'WebAppPath'
    $legacyComponentLibraryPath = Get-LegacyConfigValue $legacyContent 'ComponentLibraryPath'
    $legacyExcluded = @(Get-LegacyExcludedContent $legacyContent)

    if (-not $legacyDisplayName -or -not $legacyHostName -or -not $legacyWebAppPath -or -not $legacyComponentLibraryPath) {
        Write-Error "Could not parse all required values from legacy blazorade.config.md. " +
            "Expected: DisplayName, HostName, WebAppPath, ComponentLibraryPath."
    }

    if (-not $legacyAppName) {
        $legacyAppName = (Split-Path -Leaf $legacyWebAppPath) -replace '\.Web$',''
    }

    $newConfig = @{
        local = @{
            'scraibe.site.webAppPath' = $legacyWebAppPath
            'scraibe.site.componentLibraryPath' = $legacyComponentLibraryPath
            'scraibe.publish.excludedContent' = $legacyExcluded
        }
        scoped = @{
            'scraibe.site.displayName' = $legacyDisplayName
            'scraibe.site.appName' = $legacyAppName
            'scraibe.site.hostName' = $legacyHostName
            'scraibe.layout.default' = 'default'
        }
    }

    $json = $newConfig | ConvertTo-Json -Depth 20
    Set-Content -Path $jsonConfigFile -Value $json -Encoding UTF8
    Remove-Item -Path $legacyConfigFile -Force

    Write-Host "Upgrade complete: wrote .config.json and removed blazorade.config.md."
}

$configData = Get-Content $jsonConfigFile -Raw | ConvertFrom-Json -AsHashtable
if ($configData -isnot [hashtable]) {
    Write-Error "Invalid .config.json: document root must be an object."
}

$configLocal = @{}
$configScoped = @{}

if ($configData.ContainsKey('local')) {
    if ($configData.local -isnot [hashtable]) {
        Write-Error "Invalid .config.json: local must be an object."
    }
    $configLocal = $configData.local
}

if ($configData.ContainsKey('scoped')) {
    if ($configData.scoped -isnot [hashtable]) {
        Write-Error "Invalid .config.json: scoped must be an object."
    }
    $configScoped = $configData.scoped
}

function Get-EffectiveConfigValue([string]$key) {
    if ($configLocal.ContainsKey($key)) { return $configLocal[$key] }
    if ($configScoped.ContainsKey($key)) { return $configScoped[$key] }
    return $null
}

$DisplayName = Get-EffectiveConfigValue 'scraibe.site.displayName'
$HostName = Get-EffectiveConfigValue 'scraibe.site.hostName'
$WebAppPath = Get-EffectiveConfigValue 'scraibe.site.webAppPath'
$ComponentLibraryPath = Get-EffectiveConfigValue 'scraibe.site.componentLibraryPath'

if (-not $DisplayName -or -not $HostName -or -not $WebAppPath -or -not $ComponentLibraryPath) {
    Write-Error "Could not resolve required values from .config.json. " +
        "Expected keys: scraibe.site.displayName, scraibe.site.hostName, scraibe.site.webAppPath, scraibe.site.componentLibraryPath."
}

# Derive ComponentLibraryName from the last segment of ComponentLibraryPath
$ComponentLibraryName = Split-Path -Leaf $ComponentLibraryPath

Write-Host "Site         : $DisplayName ($HostName)"
Write-Host "Web app      : $WebAppPath"
Write-Host "Components   : $ComponentLibraryPath ($ComponentLibraryName)"

# ── Parse excluded content paths ─────────────────────────────────────────────

$ExcludedArgs = @()
$excludedContent = Get-EffectiveConfigValue 'scraibe.publish.excludedContent'

if ($excludedContent -is [System.Collections.IEnumerable] -and $excludedContent -isnot [string]) {
    foreach ($entry in $excludedContent) {
        if ($null -ne $entry -and ($entry.ToString().Trim()).Length -gt 0) {
            $ExcludedArgs += '--excluded'
            $ExcludedArgs += $entry.ToString().Trim()
        }
    }
}
elseif ($null -ne $excludedContent -and ($excludedContent.ToString().Trim()).Length -gt 0) {
    $ExcludedArgs += '--excluded'
    $ExcludedArgs += $excludedContent.ToString().Trim()
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
$staticWebAppTemplatePath = Join-Path $RepoRoot "templates/web-app/wwwroot/staticwebapp.config.json"
$layoutsPath   = Join-Path $RepoRoot "$ComponentLibraryPath/wwwroot/Layouts"
$namespace     = "$ComponentLibraryName.ShortCodes"

# ── Run the publisher ─────────────────────────────────────────────────────────

Write-Host "`nRunning publish pipeline..."
$publisherProject = Join-Path $PSScriptRoot 'Scraibe.Publisher/Scraibe.Publisher.csproj'

$PageArgs = @()
if ($Pages) {
    foreach ($page in ($Pages -split ',')) {
        $p = $page.Trim()
        if ($p) {
            $PageArgs += '--page'
            $PageArgs += $p
        }
    }
}

$publishArgs = @(
    'run', '--project', $publisherProject, '--',
    '--repo-root',           $RepoRoot,
    '--content',             $contentPath,
    '--output',              $outputPath,
    '--host',                $HostName,
    '--display-name',        $DisplayName,
    '--template',            $templatePath,
    '--staticwebapp-template', $staticWebAppTemplatePath,
    '--assembly',            $assemblyPath,
    '--component-namespace', $namespace,
    '--layouts',             $layoutsPath
) + $ExcludedArgs + $PageArgs

dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish pipeline reported errors (exit code $LASTEXITCODE)."
}

Write-Host "`nPublish complete." -ForegroundColor Green
