namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record EmployeeDto(Guid Id, string Name, string Code);

public sealed record EmployeeContactDto(Guid Id, string Email, string Phone);