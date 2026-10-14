# IIS deployment

The app runs on IIS with `ASPNETCORE_ENVIRONMENT=Local` (see `TMS.DeveloperTool.Blazor/web.config`).

## Automatic release

A Windows scheduled task on the IIS machine runs [iis-auto-release.ps1](iis-auto-release.ps1) every 2 minutes.
When `origin/release/iis` has a new commit it pulls it into its own clone (`D:\TMS\Release\TMS.DeveloperTool.Blazor\repo`),
runs the tests, runs `iis-deploy.ps1` and smoke-tests `http://localhost:5000`.

- Release logs: `D:\TMS\Release\TMS.DeveloperTool.Blazor\logs\release_*.log` (last 30 kept).
- A failed commit is not retried; push a new commit, or delete `last-failed.txt` in the work dir.
- Run once now: `Start-ScheduledTask -TaskName "TMS.DeveloperTool.Blazor auto release"`.

## One-time setup on the IIS machine

1. Install the [.NET 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0), then `iisreset`.
2. As Administrator: `.\deploy\iis-setup.ps1` (site, app pool, permissions, firewall).
3. Put `appsettings.Local.json` in `D:\TMS\Deployments\TMS.DeveloperTool.Blazor` (it is git-ignored, so auto release never deploys it):
   `.\deploy\iis-deploy.ps1 -IncludeLocalSettings` from a dev machine that has the file, or copy it by hand.
4. Push `release/iis`, then as Administrator: `.\deploy\iis-auto-release.ps1 -Register`
   (clones the branch and registers the task, running as SYSTEM; needs `git` and `dotnet` on the system PATH).

## Manual release

```powershell
.\deploy\iis-deploy.ps1                         # keeps the server's appsettings.Local.json
.\deploy\iis-deploy.ps1 -IncludeLocalSettings   # also pushes your local appsettings.Local.json
```

Logs: `D:\TMS\Deployments\TMS.DeveloperTool.Blazor\logs\stdout_*.log`.
