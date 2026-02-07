namespace TMS.DeveloperTool.Blazor.Infrastructure.Security;

public sealed record JwtTokenRequest
{
    public string Subject { get; init; } = string.Empty;
    public string JwtId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string EmployeeCode { get; init; } = string.Empty;
    public string ProfileImageUrl { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string EmployeeId { get; init; } = string.Empty;
    public List<string> CompanyAdminIds { get; init; } = [];
    public string UserType { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string IsAdmin { get; init; } = string.Empty;
    public string AdminId { get; init; } = string.Empty;
    public string NameIdentifier { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public List<string> Audiences { get; init; } = [];
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
