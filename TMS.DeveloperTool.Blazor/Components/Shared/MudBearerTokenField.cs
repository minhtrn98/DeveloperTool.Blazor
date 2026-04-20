using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using TMS.DeveloperTool.Blazor.Components.Dialogs;

namespace TMS.DeveloperTool.Blazor.Components.Shared;

/// <summary>
/// A text field for bearer tokens with an end-adornment button that opens a claims dialog.
/// </summary>
public class MudBearerTokenField : MudTextField<string>
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the title displayed on the claims dialog.
    /// </summary>
    [Parameter]
    public string ClaimsDialogTitle { get; set; } = "Token Claims";

    /// <summary>
    /// Gets or sets the icon used by the end-adornment claims button.
    /// </summary>
    [Parameter]
    public string ClaimsButtonIcon { get; set; } = Icons.Material.Filled.Badge;

    /// <summary>
    /// Gets or sets whether the claims button is shown on the field.
    /// </summary>
    [Parameter]
    public bool ShowClaimsButton { get; set; } = true;

    /// <summary>
    /// Gets or sets whether clicking the claims button should be ignored when value is empty.
    /// </summary>
    [Parameter]
    public bool DisableClaimsButtonWhenEmpty { get; set; } = true;

    /// <summary>
    /// Gets or sets options for the claims dialog.
    /// </summary>
    [Parameter]
    public DialogOptions ClaimsDialogOptions { get; set; } = new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseOnEscapeKey = true
    };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ShowClaimsButton)
        {
            return;
        }

        if (Adornment == Adornment.None)
        {
            Adornment = Adornment.End;
        }

        if (string.IsNullOrWhiteSpace(AdornmentIcon))
        {
            AdornmentIcon = ClaimsButtonIcon;
        }

        if (string.IsNullOrWhiteSpace(AdornmentAriaLabel))
        {
            AdornmentAriaLabel = "View token claims";
        }

        if (!OnAdornmentClick.HasDelegate)
        {
            OnAdornmentClick = EventCallback.Factory.Create<MouseEventArgs>(this, OpenClaimsDialogAsync);
        }
    }

    private async Task OpenClaimsDialogAsync(MouseEventArgs _)
    {
        if (DisableClaimsButtonWhenEmpty && string.IsNullOrWhiteSpace(Value))
        {
            return;
        }

        DialogParameters<BearerTokenClaimsDialog> parameters = new()
        {
            { x => x.Token, Value ?? string.Empty }
        };

        await DialogService.ShowAsync<BearerTokenClaimsDialog>(ClaimsDialogTitle, parameters, ClaimsDialogOptions);
    }
}