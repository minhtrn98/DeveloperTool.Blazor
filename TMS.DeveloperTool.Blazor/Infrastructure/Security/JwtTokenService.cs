using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Security;

public sealed class JwtTokenService
{
    private readonly JwtOptions _jwtSetting;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(JwtOptions jwtOptions)
    {
        _jwtSetting = jwtOptions;
    }

    public string CreateToken(
        Employee employee,
        bool isAdmin,
        List<string> permissions,
        List<string> roleNames,
        List<string> accessDepartments,
        long permissionsVersion
    )
    {
        ArgumentNullException.ThrowIfNull(employee);

        string employeeId = employee.EmployeeId.ToString();

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, employeeId),
            new(JwtRegisteredClaimNames.Email, ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Aud, _jwtSetting.Audience),
            new(ClaimTypes.NameIdentifier, employee.Name),
            new("employeeId", employeeId),
            new("employeeCode", employee.Code),
            new("fullName", employee.Name),
            new("jobTitleName", ""),
            new("phone", "0000000000"),
            new("companyId", "01000000-7000-5000-0000-000000000001"),
            new("isAdmin", isAdmin.ToString()),
            new("pv", permissionsVersion.ToString()), // phiên bản
        ];

        foreach (string permission in permissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        foreach (string accessDept in accessDepartments)
        {
            claims.Add(new Claim("accessDepartments", accessDept));
        }

        foreach (string roleName in roleNames)
        {
            claims.Add(new Claim("roleNames", roleName));
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
