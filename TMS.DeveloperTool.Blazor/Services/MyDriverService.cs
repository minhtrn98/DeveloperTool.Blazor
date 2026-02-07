using Microsoft.EntityFrameworkCore;
using TMS.DeveloperTool.Blazor.Infrastructure.Security;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class MyDriverService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;

    public MyDriverService(ApplicationDbContext dbContext, JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<int> GenerateTokensForAllDriversAsync(CancellationToken cancellationToken = default)
    {
        List<Driver> drivers = await _dbContext.Drivers.ToListAsync(cancellationToken);
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        foreach (Driver driver in drivers)
        {
            bool hasValidToken = !string.IsNullOrWhiteSpace(driver.BearerToken)
                && driver.TokenExpiredAt.HasValue
                && driver.TokenExpiredAt.Value > utcNow;

            if (hasValidToken)
            {
                continue;
            }

            driver.BearerToken = _jwtTokenService.CreateToken(driver);
            driver.TokenExpiredAt = _jwtTokenService.GetDefaultExpiresAtUtc(utcNow);
        }

        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Driver>> GetAllDriversAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Drivers.AsNoTracking().ToListAsync(cancellationToken);
    }
}
