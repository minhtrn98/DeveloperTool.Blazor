using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Security;

public sealed class JwtTokenService
{
    private const string DefaultProfileImageUrl = "";
    private const string DefaultUserType = "Employee";
    private const string DefaultCompanyId = "01000000-7000-5000-0000-000000000001";
    private const string DefaultIsAdmin = "true";
    private const string DefaultScope = "api hrm-api tms-api openid profile email roles";

    private readonly JwtOptions _jwtSetting;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(JwtOptions jwtOptions)
    {
        _jwtSetting = jwtOptions;
    }

    public string CreateToken(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

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
            new(ClaimTypes.NameIdentifier, driver.Name),
            new(JwtRegisteredClaimNames.Aud, _jwtSetting.Audience),
            new("scope", DefaultScope),
            new("accessDepartments", "*"),
            new("roleNames", "SA")
        ];

        foreach (string permission in _jwtSetting.Permissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        DateTime utcNow = DateTime.UtcNow;
        DateTime expiresAt = utcNow.AddMinutes(_jwtSetting.DefaultExpiresMinutes);

        string privateKeyXml = File.ReadAllText("private.pem");
        RSA rsa = RSA.Create();
        rsa.FromXmlString(privateKeyXml);
        RsaSecurityKey key = new(rsa);
        SigningCredentials credentials = new(
            key,
            SecurityAlgorithms.RsaSha256
        );

        JwtSecurityToken token = new(
            issuer: _jwtSetting.Issuer,
            audience: null,
            claims: claims,
            notBefore: utcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }

    public DateTimeOffset GetDefaultExpiresAtUtc(DateTimeOffset? utcNow = null)
    {
        DateTimeOffset baseTime = utcNow ?? DateTimeOffset.UtcNow;
        return baseTime.AddMinutes(_jwtSetting.DefaultExpiresMinutes);
    }
}
