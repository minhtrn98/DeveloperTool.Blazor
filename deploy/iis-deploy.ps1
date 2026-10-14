<#
.SYNOPSIS
    Publishes TMS.DeveloperTool.Blazor and copies it to the IIS site folder.

.DESCRIPTION
    Drops app_offline.htm first so IIS releases the locked DLLs, mirrors the publish output
    (keeping App_Data, logs and the server's appsettings.Local.json), then removes app_offline.htm.
    The site runs with ASPNETCORE_ENVIRONMENT=Local (see web.config). appsettings.Local.json is git-ignored,
    so CI builds don't have it: the server copy is kept unless -IncludeLocalSettings pushes your local one.

    TargetPath can be a local folder or a UNC share, e.g. \\tms-server\d$\TMS\Deployments\TMS.DeveloperTool.Blazor
#>
param(
    [string]$TargetPath = "D:\TMS\Deployments\TMS.DeveloperTool.Blazor",
    [switch]$IncludeLocalSettings
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot "TMS.DeveloperTool.Blazor\TMS.DeveloperTool.Blazor.csproj"
$publishDir = Join-Path $repoRoot "TMS.DeveloperTool.Blazor\bin\Release\net10.0\publish-iis"

Write-Host "Publishing..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
dotnet publish $project -c Release -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

New-Item -ItemType Directory -Force -Path $TargetPath | Out-Null
$appOffline = Join-Path $TargetPath "app_offline.htm"
Set-Content -Path $appOffline -Value "<html><body><h3>Updating to a new version, please retry in a minute...</h3></body></html>" -Encoding UTF8
Start-Sleep -Seconds 3

try {
    # /MIR mirrors the publish output; /XD and /XF keep server-only data out of the mirror.
    robocopy $publishDir $TargetPath /MIR /R:3 /W:2 /NFL /NDL /NP `
        /XD (Join-Path $TargetPath "App_Data") (Join-Path $TargetPath "logs") `
        /XF app_offline.htm appsettings.Local.json
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }

    $localSettings = Join-Path $publishDir "appsettings.Local.json"
    if ($IncludeLocalSettings) {
        if (-not (Test-Path $localSettings)) { throw "appsettings.Local.json not found in publish output" }
        Copy-Item $localSettings $TargetPath -Force
    } elseif (-not (Test-Path (Join-Path $TargetPath "appsettings.Local.json"))) {
        Write-Warning "appsettings.Local.json is missing on the target; run once with -IncludeLocalSettings."
    }
}
finally {
    Remove-Item $appOffline -Force -ErrorAction SilentlyContinue
}

Write-Host "Deployed to $TargetPath" -ForegroundColor Green
exit 0  # robocopy returns 1-7 on success
