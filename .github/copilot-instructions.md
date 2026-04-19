# Copilot Instructions — TMS.DeveloperTool.Blazor

## Stack

- **Blazor Server / .NET 10**
- **MudBlazor 8.x** — ONLY UI component library. Do NOT use Radzen, AntDesign, or any other UI library.
- No custom CSS files. All styling uses MudBlazor props only.

---

## Button Guidelines

### Convention Table

| Intent                                                    | Component                           | Variant    | Color                            | Size    | Notes                                        |
| --------------------------------------------------------- | ----------------------------------- | ---------- | -------------------------------- | ------- | -------------------------------------------- |
| Primary action (Lưu, Xác nhận, Send, Trigger, Submit)     | `MudButton`                         | `Filled`   | `Primary`                        | default | Main CTA in any form/dialog                  |
| Completion / send event with side-effects (Hoàn tất, Gửi) | `MudButton`                         | `Filled`   | `Success`                        | default | Happy-path final action                      |
| Cancel / Close / Đóng / Hủy in dialogs                    | `MudButton`                         | `Outlined` | `Default`                        | default | Always include `Color="Color.Default"`       |
| Secondary action (Copy, History, cURL, Export)            | `MudButton`                         | `Outlined` | `Secondary`                      | default | With `StartIcon` when relevant               |
| Informational action (Suggest, View)                      | `MudButton`                         | `Outlined` | `Info`                           | default |                                              |
| Refresh / reload page data                                | `MudButton`                         | `Filled`   | `Primary`                        | default | `StartIcon="@Icons.Material.Filled.Refresh"` |
| Add new item (above table)                                | `MudButton`                         | `Filled`   | `Primary`                        | default | `StartIcon="@Icons.Material.Filled.Add"`     |
| Add sub-item (inside form)                                | `MudButton`                         | `Filled`   | `Success`                        | `Small` | `StartIcon="@Icons.Material.Filled.Add"`     |
| Inline table row: view                                    | `MudIconButton` in `MudButtonGroup` | —          | `Info`                           | `Small` |                                              |
| Inline table row: edit                                    | `MudIconButton` in `MudButtonGroup` | —          | `Warning`                        | `Small` |                                              |
| Inline table row: delete                                  | `MudIconButton` in `MudButtonGroup` | —          | `Error`                          | `Small` |                                              |
| Standalone delete/clear                                   | `MudIconButton`                     | —          | `Error`                          | `Small` |                                              |
| Icon-only utility (copy, filter, expand…)                 | `MudIconButton`                     | —          | `Secondary` or `Primary`         | `Small` |                                              |
| NavBar / AppBar toggle icon                               | `MudIconButton`                     | —          | `Inherit`                        | default | `Edge="Edge.Start"`                          |
| NavBar utility buttons                                    | `MudButton`                         | `Filled`   | `Primary`/`Secondary`/`Tertiary` | default | `StartIcon`, `Class="me-2"`                  |

### Rules

1. **Always specify both `Variant` and `Color`** on every `MudButton`. Never leave either implicit.
2. **Never use `MudButton` with icon and no label text** — use `MudIconButton` instead.
3. **Never put `Variant` on `MudIconButton`** — it is not a valid prop and is silently ignored.
4. **Never hardcode height/min-height on buttons** via `Style=""`. Use `Size` prop instead.
5. **Table row action buttons** must use `Size="Size.Small"`.

### Dialog Footer Pattern

Cancel always comes first, primary action second:

```razor
<MudButton OnClick="Cancel" Variant="Variant.Outlined" Color="Color.Default">Hủy</MudButton>
<MudButton OnClick="SubmitAsync" Variant="Variant.Filled" Color="Color.Primary">Lưu</MudButton>
```

### Grouped Table Row Actions

```razor
<MudButtonGroup Variant="Variant.Outlined" Size="Size.Small">
    <MudIconButton Icon="@Icons.Material.Filled.Visibility" Color="Color.Info"    Size="Size.Small" />
    <MudIconButton Icon="@Icons.Material.Filled.Edit"       Color="Color.Warning" Size="Size.Small" />
    <MudIconButton Icon="@Icons.Material.Filled.Delete"     Color="Color.Error"   Size="Size.Small" />
</MudButtonGroup>
```

### Color Semantics

| Color       | Use when                                       |
| ----------- | ---------------------------------------------- |
| `Primary`   | Main / default positive action                 |
| `Secondary` | Neutral utility (copy, export, history)        |
| `Success`   | Irreversible completion or send-event          |
| `Warning`   | Edit / caution action                          |
| `Error`     | Delete / destructive action                    |
| `Info`      | View / suggestion / informational              |
| `Default`   | Cancel, close, neutral dismiss                 |
| `Inherit`   | Toolbar / AppBar icons inheriting AppBar color |
| `Tertiary`  | AppBar only                                    |

---

## Dropdown Guidelines

### Rule: All dropdowns use `MudAutocomplete<T>`

Do NOT use `MudSelect`, native `<select>`, or any other dropdown component.

### Rule: Model must implement `IDisplaySearchItem`

```csharp
// TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces
public interface IDisplaySearchItem
{
    string DisplayString { get; }   // text shown in the dropdown list
    bool Like(string searchTerm);   // filter logic when user types
}
```

#### Implementing on a domain class

```csharp
public sealed class Employee : IDisplaySearchItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string DisplayString => $"{Code} - {Name}";

    public bool Like(string searchTerm) =>
        Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
```

#### Implementing on a DTO record

```csharp
public sealed record DailyPlanDto : IDisplaySearchItem
{
    public required string Code { get; init; }
    public required string Name { get; init; }

    public string DisplayString => $"{Code} - {Name}";

    public bool Like(string searchTerm) =>
        Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
```

#### Enum wrapper (for enum-based dropdowns)

```csharp
// Do NOT use T="string" or T="int" for enum dropdowns
public sealed record EnumOption(string Value, string Description) : IDisplaySearchItem
{
    public string DisplayString => Description;
    public bool Like(string searchTerm) =>
        Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
```

### Razor Template

```razor
<MudAutocomplete T="MyModel"
                 @bind-Value="selectedItem"
                 SearchFunc="SearchItems"
                 Label="Tên nhãn"
                 Variant="Variant.Outlined"
                 Clearable="true"
                 MaxItems="20"
                 ToStringFunc="AutocompleteDisplayHelper.GetDisplayString<MyModel>" />
```

When a side-effect is needed on selection, use `Value` + `ValueChanged` instead of `@bind-Value`:

```razor
<MudAutocomplete T="MyModel"
                 Value="selectedItem"
                 ValueChanged="OnItemChanged"
                 SearchFunc="SearchItems"
                 Label="Tên nhãn"
                 Variant="Variant.Outlined"
                 Clearable="true"
                 MaxItems="20"
                 ToStringFunc="AutocompleteDisplayHelper.GetDisplayString<MyModel>" />
```

### Code-Behind Pattern

```csharp
// Always use AutocompleteSearchHelper — do NOT write custom LINQ
private Task<IEnumerable<MyModel>> SearchItems(string searchTerm, CancellationToken ct)
{
    var result = AutocompleteSearchHelper.SearchByLike(allItems, searchTerm);
    return Task.FromResult(result);
}
```

### Available Helpers (never rewrite these)

| Helper                                            | Namespace                       | Used for                            |
| ------------------------------------------------- | ------------------------------- | ----------------------------------- |
| `AutocompleteDisplayHelper.GetDisplayString<T>()` | `Infrastructure.Shared.Helpers` | `ToStringFunc` on `MudAutocomplete` |
| `AutocompleteSearchHelper.SearchByLike<T>()`      | `Infrastructure.Shared.Helpers` | `SearchFunc` on `MudAutocomplete`   |

### Checklist for a new dropdown

- [ ] Model/DTO implements `IDisplaySearchItem` (`DisplayString` + `Like()`)
- [ ] `MudAutocomplete` with `Variant="Variant.Outlined"`, `Clearable="true"`, `MaxItems="20"`
- [ ] `ToStringFunc="AutocompleteDisplayHelper.GetDisplayString<T>"`
- [ ] `SearchFunc` calls `AutocompleteSearchHelper.SearchByLike()`
- [ ] `@bind-Value` if no side-effect; `Value` + `ValueChanged` if side-effect needed

### Notes

- Keep `MaxItems="20"` as the default to avoid overwhelming the UI.
- `DisplayString` should be short, typically `$"{Code} - {Name}"`.
- `Like()` should search on fields users can recall (code, name) — not on GUIDs/IDs.
