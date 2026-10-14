<#
.SYNOPSIS
    Publishes TMS.DeveloperTool.Blazor and copies it to the IIS site folder.

.DESCRIPTION
    Drops app_offline.htm first so IIS releases the locked DLLs, mirrors the publish output
    (keeping App_Data and logs), then removes app_offline.htm.
    The site runs with ASPNETCORE_ENVIRONMENT=Local (see web.config), so appsettings.Local.json is deployed as-is.

    TargetPath can be a local folder or a UNC share, e.g. \\tms-server\d$\TMS\Deployments\TMS.DeveloperTool.Blazor
#>
param(
    [string]$TargetPath = "D:\TMS\Deployments\TMS.DeveloperTool.Blazor"
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
        /XF app_offline.htm
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }
}
finally {
    Remove-Item $appOffline -Force -ErrorAction SilentlyContinue
}

Write-Host "Deployed to $TargetPath" -ForegroundColor Green
exit 0  # robocopy returns 1-7 on success
