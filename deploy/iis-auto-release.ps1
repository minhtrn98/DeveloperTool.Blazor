<#
.SYNOPSIS
    Polls the release branch and redeploys IIS when it has new commits. Meant to run from Windows Task Scheduler.

.DESCRIPTION
    Uses its own clone (WorkDir\repo), separate from any dev working copy:
      git fetch -> new commit on origin/<Branch>? -> reset to it -> dotnet test -> iis-deploy.ps1 -> smoke test.
    The last deployed / last failed commit is stored in WorkDir, so a failed commit is not retried every run;
    push a new commit (or delete WorkDir\last-failed.txt) to retry.

    Register the scheduled task once (as Administrator):
        .\deploy\iis-auto-release.ps1 -Register
#>
param(
    [string]$RepoUrl = "https://github.com/minhtrn98/DeveloperTool.Blazor.git",
    [string]$Branch = "release/iis",
    [string]$WorkDir = "D:\TMS\Release\TMS.DeveloperTool.Blazor",
    [string]$TargetPath = "D:\TMS\Deployments\TMS.DeveloperTool.Blazor",
    [string]$SiteUrl = "http://localhost:5000",
    [int]$IntervalMinutes = 2,
    [switch]$SkipTests,
    [switch]$Register
)

$ErrorActionPreference = "Stop"
$TaskName = "TMS.DeveloperTool.Blazor auto release"

# --- Register the scheduled task -------------------------------------------
if ($Register) {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run with -Register as Administrator."
    }

    # The task runs the copy of this script inside the release clone, so script changes ship with the branch.
    $repoDir = Join-Path $WorkDir "repo"
    if (-not (Test-Path (Join-Path $repoDir ".git"))) {
        New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null
        git clone --branch $Branch $RepoUrl $repoDir
        if ($LASTEXITCODE -ne 0) { throw "git clone failed" }
    }
    $script = Join-Path $repoDir "deploy\iis-auto-release.ps1"
    $shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { (Get-Command pwsh).Source } else { "powershell.exe" }
    $taskArgs = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$script`" " +
        "-RepoUrl `"$RepoUrl`" -Branch `"$Branch`" -WorkDir `"$WorkDir`" -TargetPath `"$TargetPath`" -SiteUrl `"$SiteUrl`""
    if ($SkipTests) { $taskArgs += " -SkipTests" }

    $action = New-ScheduledTaskAction -Execute $shell -Argument $taskArgs -WorkingDirectory $WorkDir
    $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes $IntervalMinutes)
    $settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 30) -StartWhenAvailable
    $taskPrincipal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
        -Principal $taskPrincipal -Force | Out-Null

    Write-Host "Scheduled task '$TaskName' registered: every $IntervalMinutes min, watching $Branch." -ForegroundColor Green
    Write-Host "Logs: $(Join-Path $WorkDir 'logs')"
    exit 0
}

# --- Poll and release -------------------------------------------------------
$repoDir = Join-Path $WorkDir "repo"
$logDir = Join-Path $WorkDir "logs"
$lastDeployedFile = Join-Path $WorkDir "last-deployed.txt"
$lastFailedFile = Join-Path $WorkDir "last-failed.txt"
New-Item -ItemType Directory -Force -Path $WorkDir, $logDir | Out-Null

# SYSTEM does not own the clone when it was created by another user.
function Invoke-Git {
    git -c safe.directory=* -C $repoDir @args
    if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed with exit code $LASTEXITCODE" }
}

if (-not (Test-Path (Join-Path $repoDir ".git"))) {
    git clone --branch $Branch $RepoUrl $repoDir
    if ($LASTEXITCODE -ne 0) { throw "git clone failed" }
}

Invoke-Git fetch --quiet origin $Branch
$remoteSha = (git -c safe.directory=* -C $repoDir rev-parse "origin/$Branch").Trim()
$lastDeployed = if (Test-Path $lastDeployedFile) { (Get-Content $lastDeployedFile -Raw).Trim() } else { "" }
$lastFailed = if (Test-Path $lastFailedFile) { (Get-Content $lastFailedFile -Raw).Trim() } else { "" }

if ($remoteSha -eq $lastDeployed -or $remoteSha -eq $lastFailed) {
    exit 0  # nothing new
}

$logFile = Join-Path $logDir ("release_{0:yyyyMMdd_HHmmss}_{1}.log" -f (Get-Date), $remoteSha.Substring(0, 7))
Start-Transcript -Path $logFile | Out-Null
try {
    Write-Host "Releasing $Branch @ $remoteSha (previous: $lastDeployed)"
    Invoke-Git reset --hard "origin/$Branch"
    Invoke-Git clean -fdx -e "**/bin/" -e "**/obj/"
    Invoke-Git log -1 --format="%h %an: %s"

    if (-not $SkipTests) {
        dotnet test (Join-Path $repoDir "TMS.DeveloperTool.Blazor.Tests\TMS.DeveloperTool.Blazor.Tests.csproj") --configuration Release
        if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    }

    & (Join-Path $repoDir "deploy\iis-deploy.ps1") -TargetPath $TargetPath
    if ($LASTEXITCODE -ne 0) { throw "iis-deploy.ps1 failed with exit code $LASTEXITCODE" }

    $up = $false
    for ($i = 1; $i -le 10 -and -not $up; $i++) {
        try { $up = (Invoke-WebRequest $SiteUrl -UseBasicParsing -TimeoutSec 30).StatusCode -eq 200 }
        catch { Write-Host "Smoke test attempt ${i}: $($_.Exception.Message)"; Start-Sleep -Seconds 6 }
    }
    if (-not $up) { throw "Site did not respond with 200 at $SiteUrl" }

    Set-Content -Path $lastDeployedFile -Value $remoteSha
    Remove-Item $lastFailedFile -ErrorAction SilentlyContinue
    Write-Host "Release OK: $remoteSha"
}
catch {
    Set-Content -Path $lastFailedFile -Value $remoteSha
    Write-Host "Release FAILED: $($_.Exception.Message)"
    throw
}
finally {
    Stop-Transcript | Out-Null
    # Keep the last 30 release logs.
    Get-ChildItem $logDir -Filter "release_*.log" | Sort-Object LastWriteTime -Descending |
        Select-Object -Skip 30 | Remove-Item -Force -ErrorAction SilentlyContinue
}
