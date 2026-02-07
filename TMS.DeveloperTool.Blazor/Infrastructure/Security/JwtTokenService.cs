using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Security;

public sealed class JwtTokenService
{
    private const string DefaultProfileImageUrl = "";
    private const string DefaultUserType = "Employee";
    private const string DefaultCompanyId = "";
    private const string DefaultIsAdmin = "True";
    private const string DefaultAdminId = "";
    private const string DefaultScope = "api hrm-api tms-api openid profile email roles";
    private static readonly string[] DefaultCompanyAdminIds = ["", ""];

    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        if (string.IsNullOrWhiteSpace(_options.Key))
        {
            throw new InvalidOperationException("Identity:Key is required.");
        }

        string driverId = driver.DriverId.ToString();

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, driverId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, driver.Email),
            new("employeeCode", driver.Code),
            new("profileImageUrl", DefaultProfileImageUrl),
            new("fullName", driver.Name),
            new("employeeId", driverId),
            new("user_type", DefaultUserType),
            new("company_id", DefaultCompanyId),
            new("isAdmin", DefaultIsAdmin),
            new("admin_id", DefaultAdminId),
            new(ClaimTypes.NameIdentifier, driver.Name),
            new("scope", DefaultScope)
        ];

        foreach (string value in DefaultCompanyAdminIds)
        {
            claims.Add(new Claim("company_admin_id", value));
        }

        IEnumerable<string> audiences = _options.Audiences;

        foreach (string audience in audiences)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));
        }

        DateTime utcNow = DateTime.UtcNow;
        DateTime expiresAt = utcNow.AddMinutes(_options.DefaultExpiresMinutes);

        SymmetricSecurityKey signingKey = new(System.Text.Encoding.UTF8.GetBytes(_options.Key));
        SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _options.Issuer,
            audience: null,
            claims: claims,
            notBefore: utcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }
}
