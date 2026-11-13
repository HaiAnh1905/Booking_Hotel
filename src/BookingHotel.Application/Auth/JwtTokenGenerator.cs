using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace BookingHotel.Auth;

public class JwtTokenGenerator : ITokenGenerator, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IdentityUserManager _userManager;

    public JwtTokenGenerator(IConfiguration configuration, IdentityUserManager userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<(string Token, int ExpiresIn)> GenerateTokenAsync(IdentityUser user, bool rememberMe = false)
    {
        var secretKey = _configuration["AuthServer:DefaultPassPhrase"] 
            ?? _configuration["StringEncryption:DefaultPassPhrase"] 
            ?? "BookingHotelSuperSecretSigningKey20261111!";
        
        var issuer = _configuration["AuthServer:Authority"] ?? "https://localhost:44386";
        var audience = "BookingHotel";
        
        // Hiến pháp Điều IX: Phiên đăng nhập tối đa 15 ngày khi chọn Remember Me, không được hơn.
        // Mặc định không Remember Me: 120 phút (2 giờ).
        var expiresInMinutes = rememberMe ? 15 * 24 * 60 : 120; // 15 ngày hoặc 2 giờ

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.Name ?? user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey.PadRight(32)));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresInMinutes * 60);
    }
}

