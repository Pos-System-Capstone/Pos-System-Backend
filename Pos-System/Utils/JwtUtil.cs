using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.Domain.Models;

namespace Pos_System.API.Utils;

public class JwtUtil
{
    private JwtUtil()
    {
    }

    public static string GenerateJwtToken(Account account, Tuple<string, Guid> guidClaim)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables(EnvironmentVariableConstant.Prefix).Build();
        JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();
        SymmetricSecurityKey secrectKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>(JwtConstant.SecretKey)));
        var credentials = new SigningCredentials(secrectKey, SecurityAlgorithms.HmacSha256Signature);
        string issuer = configuration.GetValue<string>(JwtConstant.Issuer);
        List<Claim> claims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, account.Username),
            new Claim(ClaimTypes.Role, account.Role.Name),
        };
        if (guidClaim != null) claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
        var expires = account.Role.Name.Equals(RoleEnum.Staff.GetDescriptionFromEnum())
            ? DateTime.Now.AddDays(1)
            : DateTime.Now.AddDays(30);
        var token = new JwtSecurityToken(issuer, null, claims, notBefore: DateTime.Now, expires, credentials);
        return jwtHandler.WriteToken(token);
    }
}