<#
.SYNOPSIS
    Pack and publish Sean.Core.DbRepository NuGet packages.

.DESCRIPTION
    Builds the two ORM projects (Sean.Core.DbRepository, Sean.Core.DbRepository.Dapper)
    in Release config, then pushes the resulting .nupkg / .snupkg files to the configured
    NuGet targets.

    Scope 'Local'    (default) - push only to the local NuGet server (internal testing).
    Scope 'Official'           - push only to official nuget.org (skip local cache).
    Scope 'All'                - push to the local server AND official nuget.org (stable release).

    The local NuGet server does NOT support .snupkg symbol packages, so symbol packages
    are only pushed to the official feed.

    API keys are read from environment variables, NEVER hard-coded. Only the keys for
    targets in the current scope are required:
        $env:SEAN_NUGET_KEY   local NuGet server publish key (needed for Local / All,
                              OPTIONAL when 'sean.nuget.com' is already defined in
                              %APPDATA%\NuGet\NuGet.Config with allowInsecureConnections=true)
        $env:NUGET_ORG_KEY    nuget.org API key (https://www.nuget.org/account/apikeys) (needed for Official / All)

    Each target pushes by SourceName (a NuGet.Config packageSource key) when set,
    letting NuGet resolve the URL, the API key, and the allowInsecureConnections
    flag from config. For the local server this is the preferred path because the
    push URL form does NOT consult NuGet.Config for the insecure-connections flag.

.PARAMETER Scope
    Push scope. 'Local' (default), 'Official', or 'All'.

.EXAMPLE
    # Internal test - only the local server. If 'sean.nuget.com' is already
    # configured in NuGet.Config (incl. API key and allowInsecureConnections),
    # the env var is not required.
    $env:SEAN_NUGET_KEY = '<your local key>'   # optional in this case
    .\Publish-NuGet.ps1

.EXAMPLE
    # Direct release to official, skipping the local server (e.g. hotfix)
    $env:NUGET_ORG_KEY = '<your nuget.org key>'
    .\Publish-NuGet.ps1 -Scope Official

.EXAMPLE
    # Stable release - both local server and official nuget.org
    $env:SEAN_NUGET_KEY = '<your local key>'
    $env:NUGET_ORG_KEY  = '<your nuget.org key>'
    .\Publish-NuGet.ps1 -Scope All

.EXAMPLE
    # Persist keys across PowerShell sessions (Windows; current user only)
    [System.Environment]::SetEnvironmentVariable('SEAN_NUGET_KEY', '<your local key>',  'User')
    [System.Environment]::SetEnvironmentVariable('NUGET_ORG_KEY',  '<your nuget.org key>', 'User')
#>

[CmdletBinding()]
param(
    [ValidateSet('Local', 'Official', 'All')]
    [string]$Scope = 'Local'
)

$ErrorActionPreference = 'Stop'

# Run from the repo root regardless of where the script is invoked from.
Set-Location (Join-Path $PSScriptRoot '..')

# ----- 1. Build the list of push targets based on -Scope -----
# Each target specifies a SourceName (a packageSource key in NuGet.Config) OR
# a full SourceUrl. When SourceName is set, the script omits --api-key and
# --source, letting NuGet resolve both the URL and the stored API key from
# NuGet.Config (incl. allowInsecureConnections). When SourceUrl is set, the
# script passes --api-key from the env var and --source by URL.
$targets = @(
    @{
        Name        = 'Local NuGet server (intranet)'
        SourceName  = 'sean.nuget.com'                              # resolved from %APPDATA%\NuGet\NuGet.Config
        SourceUrl   = $null
        Key         = $env:SEAN_NUGET_KEY                            # optional override; ignored when SourceName is set
        PushSymbols = $false   # local server does not host symbols
    }
)

if ($Scope -in 'Official', 'All') {
    $targets += @{
        Name        = 'Official nuget.org (public)'
        SourceName  = $null
        SourceUrl   = 'https://api.nuget.org/v3/index.json'
        Key         = $env:NUGET_ORG_KEY
        PushSymbols = $true    # nuget.org accepts .snupkg; the upstream build step runs --no-incremental so DLLs/PDBs are paired
    }
}

# ----- 2. Validate keys up front so we fail fast before any pack/push -----
# Targets that resolve by SourceName get their key from NuGet.Config; env-var
# override is not required for them.
foreach ($t in $targets) {
    if (-not $t.SourceName -and [string]::IsNullOrWhiteSpace($t.Key)) {
        throw "Missing API key for [$($t.Name)]. Set the corresponding `$env:...` variable before running."
    }
}

# ----- 3. Build + Pack the two ORM projects -----
# CRITICAL: build first with --no-incremental so the DLLs and PDBs are produced
# from the same compile pass. If we let `dotnet pack` reuse stale bin/Release
# artifacts from an earlier build, the .nupkg and .snupkg will contain DLL/PDB
# pairs that are out of sync, and the symbol server will reject the .snupkg
# with "pdb(s) for a corresponding dll(s) not found in the nuget package".
$packagesDir = Join-Path (Get-Location) 'packages'
if (Test-Path $packagesDir) { Remove-Item -Recurse -Force $packagesDir }
New-Item -ItemType Directory -Path $packagesDir | Out-Null

$projects = @(
    'src\Sean.Core.DbRepository\Sean.Core.DbRepository.csproj',
    'src\Sean.Core.DbRepository.Dapper\Sean.Core.DbRepository.Dapper.csproj'
)

foreach ($p in $projects) {
    Write-Host "==> Building $p (--no-incremental to force fresh DLL+PDB)" -ForegroundColor Cyan
    dotnet build $p -c Release --no-incremental
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for $p" }

    Write-Host "==> Packing  $p" -ForegroundColor Cyan
    dotnet pack $p -c Release --no-build -o $packagesDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed for $p" }
}

# ----- 4. Show the artifacts we are about to push -----
$artifacts = @(Get-ChildItem -Path $packagesDir -Filter '*.nupkg') +
             @(Get-ChildItem -Path $packagesDir -Filter '*.snupkg')
Write-Host ''
Write-Host "==> Built $($artifacts.Count) artifact(s):" -ForegroundColor Cyan
$artifacts | Format-Table Name, Length -AutoSize

# ----- 5. Push to each target, in declaration order -----
foreach ($t in $targets) {
    $sourceLabel = if ($t.SourceName) { "name='$($t.SourceName)'" } else { "url='$($t.SourceUrl)'" }
    Write-Host ''
    Write-Host "==> Pushing to $($t.Name) [$sourceLabel]" -ForegroundColor Cyan

    # Build the common argument list for `dotnet nuget push`. When SourceName is
    # set, both --api-key and --source are omitted so NuGet can resolve them
    # from NuGet.Config (and honour allowInsecureConnections on the source).
    $commonArgs = @('--skip-duplicate')

    if ($t.SourceName) {
        # dotnet nuget push: pass --source <name>; key comes from NuGet.Config.
        $sourceArg = @('--source', $t.SourceName)
        $keyArg    = @()    # do NOT pass --api-key; let NuGet.Config provide it
    } else {
        $sourceArg = @('--source', $t.SourceUrl)
        $keyArg    = if ($t.Key) { @('--api-key', $t.Key) } else { @() }
    }

    # 5a. Runtime packages (.nupkg) - push each file individually so a single
    #     failure does not silently abort the rest of the batch.
    $nupkgFiles = @(Get-ChildItem -Path $packagesDir -Filter '*.nupkg' | Sort-Object Name)
    if ($nupkgFiles.Count -eq 0) { throw "No .nupkg files in $packagesDir" }
    foreach ($pkg in $nupkgFiles) {
        Write-Host "    PUT $($pkg.Name)" -ForegroundColor DarkGray
        dotnet nuget push $pkg.FullName @keyArg @sourceArg @commonArgs
        if ($LASTEXITCODE -ne 0) { throw "Failed to push $($pkg.Name) to $($t.Name)" }
    }

    # 5b. Symbol packages (.snupkg) - only if the target accepts them
    if ($t.PushSymbols) {
        $snupkgFiles = @(Get-ChildItem -Path $packagesDir -Filter '*.snupkg' | Sort-Object Name)
        if ($snupkgFiles.Count -eq 0) { throw "No .snupkg files in $packagesDir" }
        foreach ($pkg in $snupkgFiles) {
            Write-Host "    PUT $($pkg.Name)" -ForegroundColor DarkGray
            dotnet nuget push $pkg.FullName @keyArg @sourceArg @commonArgs
            if ($LASTEXITCODE -ne 0) { throw "Failed to push $($pkg.Name) to $($t.Name)" }
        }
    } else {
        Write-Host '    (skipping .snupkg per target config)' -ForegroundColor DarkGray
    }
}

Write-Host ''
Write-Host "==> All done. Scope=$Scope" -ForegroundColor Green
