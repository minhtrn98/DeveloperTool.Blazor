# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the app
dotnet run --project TMS.DeveloperTool.Blazor

# Build (Release)
dotnet build TMS.DeveloperTool.Blazor.slnx --configuration Release

# Run all tests
dotnet test TMS.DeveloperTool.Blazor.Tests/TMS.DeveloperTool.Blazor.Tests.csproj

# Run a single test class
dotnet test TMS.DeveloperTool.Blazor.Tests/ --filter "FullyQualifiedName~JsonBuilderServiceTests"
```

App runs on `http://localhost:5000`. No browser auto-launch.

## Architecture

**Blazor Server on .NET 10** — Interactive Server render mode (not WebAssembly or static).

**Feature-based vertical slices:** each feature under `Features/` has its own Pages (`.razor`), Services, and Models. Cross-cutting infrastructure lives in `Infrastructure/`.

**Two database layers:**
- **Developer DB (PostgreSQL via EF Core):** local tables — `vehicles`, `employees`, `route_checkpoints`, `route_checkpoint_templates`, `request_histories`. Accessed via `ApplicationDbContext`.
- **TMS databases (read-only via Dapper):** DriverDb, FleetDb, RouteDb, PlanningDb, OrderDb, TrackingDb. Queried through `ApplicationDbQuery` (keyed services registered per database name).

**HTTP clients:** Refit interfaces (`IFleetAssignmentApi`, `IPickupTaskApi`, `IVehicleStatusApi`) for typed API calls. Custom handlers in `Infrastructure/Http/` inject headers, API keys, and logging.

**Caching:** `CacheService` wraps Redis. Stores per-browser session data (department selection) keyed by browser ID set via cookie middleware.

**Configuration:** Options pattern throughout. All settings classes are in `Infrastructure/Configuration/` and bound/validated at startup via `ServiceCollectionExtensions`.

**Service registration entry points:**
- `Infrastructure/Extensions/ServiceCollectionExtensions.cs` — main DI wiring
- `Infrastructure/Extensions/DatabaseServiceExtensions.cs` — EF Core + Dapper
- `Infrastructure/Extensions/ExternalApiServiceExtensions.cs` — Refit clients

## UI Conventions

**MudBlazor is the only UI library.** Do not use Radzen, AntDesign, or any other component library. Do not write custom CSS files — use MudBlazor props only.

### Buttons

Always specify both `Variant` and `Color` on every `MudButton`. Never leave either implicit.

| Intent | Component | Variant | Color |
|---|---|---|---|
| Primary action (Save, Submit, Send, Trigger) | `MudButton` | `Filled` | `Primary` |
| Completion with side-effects (Hoàn tất, Gửi) | `MudButton` | `Filled` | `Success` |
| Cancel / Close / Hủy in dialogs | `MudButton` | `Outlined` | `Default` |
| Secondary action (Copy, History, Export, cURL) | `MudButton` | `Outlined` | `Secondary` |
| Informational action (Suggest, View) | `MudButton` | `Outlined` | `Info` |
| Refresh / reload | `MudButton` | `Filled` | `Primary` |
| Add new item (above table) | `MudButton` | `Filled` | `Primary` |
| Add sub-item (inside form) | `MudButton` | `Filled` | `Success` (Size.Small) |
| Table row: view | `MudIconButton` in `MudButtonGroup` | — | `Info` (Size.Small) |
| Table row: edit | `MudIconButton` in `MudButtonGroup` | — | `Warning` (Size.Small) |
| Table row: delete | `MudIconButton` in `MudButtonGroup` | — | `Error` (Size.Small) |
| Icon-only utility | `MudIconButton` | — | `Secondary` or `Primary` (Size.Small) |
| AppBar toggle | `MudIconButton` | — | `Inherit` |

Rules:
- Never use `MudButton` with icon and no label — use `MudIconButton` instead.
- Never put `Variant` on `MudIconButton` — it is silently ignored.
- Never hardcode height via `Style=""` — use the `Size` prop.
- Dialog footer: Cancel first, primary action second.

```razor
<MudButton OnClick="Cancel" Variant="Variant.Outlined" Color="Color.Default">Hủy</MudButton>
<MudButton OnClick="SubmitAsync" Variant="Variant.Filled" Color="Color.Primary">Lưu</MudButton>
```

### Dropdowns

**All dropdowns use `MudAutocomplete<T>`.** Do not use `MudSelect` or native `<select>`.

The bound type must implement `IDisplaySearchItem` (`Infrastructure/Shared/Interfaces/`):

```csharp
public interface IDisplaySearchItem
{
    string DisplayString { get; }
    bool Like(string searchTerm);
}
```

Standard `MudAutocomplete` template:

```razor
<MudAutocomplete T="MyModel"
                 @bind-Value="selectedItem"
                 SearchFunc="SearchItems"
                 Label="Label text"
                 Variant="Variant.Outlined"
                 Clearable="true"
                 MaxItems="20"
                 ToStringFunc="AutocompleteDisplayHelper.GetDisplayString<MyModel>" />
```

Search function always uses `AutocompleteSearchHelper` — do not write custom LINQ:

```csharp
private Task<IEnumerable<MyModel>> SearchItems(string searchTerm, CancellationToken ct)
    => Task.FromResult(AutocompleteSearchHelper.SearchByLike(allItems, searchTerm));
```

When a side-effect is needed on selection, use `Value` + `ValueChanged` instead of `@bind-Value`.

For enum dropdowns, wrap in `EnumOption(string Value, string Description) : IDisplaySearchItem` — do not use `T="string"` or `T="int"`.

## Testing

xUnit v3 with Moq and FluentAssertions. Tests live in `TMS.DeveloperTool.Blazor.Tests/`. CI runs on Ubuntu via `.github/workflows/dotnet-tests.yml` on every push to main and all PRs.
