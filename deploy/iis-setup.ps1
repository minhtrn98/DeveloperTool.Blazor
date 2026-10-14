#Requires -RunAsAdministrator
<#
.SYNOPSIS
    One-time IIS setup for TMS.DeveloperTool.Blazor (run on the IIS server as Administrator).

.DESCRIPTION
    - Installs the IIS features needed (WebSockets for Blazor Server, Application Initialization for preload).
    - Creates an app pool with No Managed Code, AlwaysRunning, no idle timeout and no periodic recycle,
      so the background jobs (hosted services) keep running without incoming requests.
    - Creates the site with preloadEnabled and grants the app pool identity write access to App_Data and logs.

    Prerequisite: ASP.NET Core 10 Hosting Bundle installed
    (https://dotnet.microsoft.com/download/dotnet/10.0 -> "Hosting Bundle"), then run `iisreset`.
#>
param(
    [string]$SiteName = "TMS.DeveloperTool.Blazor",
    [string]$AppPoolName = "TMS.DeveloperTool.Blazor",
    [string]$PhysicalPath = "D:\TMS\Deployments\TMS.DeveloperTool.Blazor",
    [int]$Port = 5000,
    [string]$HostHeader = ""
)

$ErrorActionPreference = "Stop"

# --- IIS features -----------------------------------------------------------
$features = @("IIS-WebServerRole", "IIS-WebServer", "IIS-WebSockets", "IIS-ApplicationInit", "IIS-ManagementConsole")
$isServer = (Get-CimInstance Win32_OperatingSystem).ProductType -ne 1
if ($isServer) {
    Install-WindowsFeature Web-Server, Web-WebSockets, Web-AppInit, Web-Mgmt-Console | Out-Null
} else {
    foreach ($f in $features) {
        Enable-WindowsOptionalFeature -Online -FeatureName $f -All -NoRestart | Out-Null
    }
}

$ancm = Join-Path $env:ProgramFiles "IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (-not (Test-Path $ancm)) {
    throw "ASP.NET Core Module V2 not found. Install the .NET 10 Hosting Bundle, run 'iisreset', then re-run this script."
}

# appcmd works in both Windows PowerShell 5.1 and PowerShell 7
# (the WebAdministration module's IIS: drive is not available in pwsh).
$appcmd = Join-Path $env:windir "System32\inetsrv\appcmd.exe"
function Invoke-AppCmd {
    & $appcmd @args
    if ($LASTEXITCODE -ne 0) { throw "appcmd $($args -join ' ') failed with exit code $LASTEXITCODE" }
}
function Test-AppCmdItem([string]$Kind, [string]$Name) {
    $found = & $appcmd list $Kind "/name:$Name" 2>$null
    return [bool]$found
}

# --- Folders ----------------------------------------------------------------
foreach ($dir in @($PhysicalPath, (Join-Path $PhysicalPath "App_Data"), (Join-Path $PhysicalPath "logs"))) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

# --- App pool ---------------------------------------------------------------
if (-not (Test-AppCmdItem "apppool" $AppPoolName)) {
    Invoke-AppCmd add apppool "/name:$AppPoolName"
}
Invoke-AppCmd set apppool "/apppool.name:$AppPoolName" `
    "/managedRuntimeVersion:" `
    "/startMode:AlwaysRunning" `
    "/processModel.idleTimeout:00:00:00" `
    "/processModel.loadUserProfile:true" `
    "/recycling.periodicRestart.time:00:00:00"

# --- Site -------------------------------------------------------------------
if (-not (Test-AppCmdItem "site" $SiteName)) {
    Invoke-AppCmd add site "/name:$SiteName" "/physicalPath:$PhysicalPath" "/bindings:http/*:${Port}:$HostHeader"
} else {
    Invoke-AppCmd set vdir "/vdir.name:$SiteName/" "/physicalPath:$PhysicalPath"
}
Invoke-AppCmd set app "/app.name:$SiteName/" "/applicationPool:$AppPoolName" "/preloadEnabled:true"
Invoke-AppCmd set site "/site.name:$SiteName" "/applicationDefaults.preloadEnabled:true"

# --- Permissions ------------------------------------------------------------
$identity = "IIS AppPool\$AppPoolName"
icacls $PhysicalPath /grant "${identity}:(OI)(CI)RX" /T /Q | Out-Null
icacls (Join-Path $PhysicalPath "App_Data") /grant "${identity}:(OI)(CI)M" /T /Q | Out-Null
icacls (Join-Path $PhysicalPath "logs") /grant "${identity}:(OI)(CI)M" /T /Q | Out-Null

# --- Firewall ---------------------------------------------------------------
$ruleName = "IIS $SiteName ($Port)"
if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
}

Write-Host "IIS site '$SiteName' ready at http://localhost:$Port (path: $PhysicalPath)" -ForegroundColor Green
