using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Http;

public sealed class DepartmentHeaderHandler(CacheService cacheService, BrowserContext browserContext) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // remove existing headers if present to avoid duplicates
        request.Headers.Remove("Departmentid");
        request.Headers.Remove("Companyid");

        string? browserId = browserContext.BrowserId;
        if (!string.IsNullOrEmpty(browserId))
        {
            DepartmentDto? department = cacheService.GetSessionDepartment(browserId);
            if (department is not null)
            {
                request.Headers.TryAddWithoutValidation("Departmentid", department.Id.ToString());
                request.Headers.TryAddWithoutValidation("Companyid", department.CompanyId.ToString());
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
