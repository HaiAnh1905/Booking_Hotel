using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BookingHotel.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;

namespace BookingHotel.Auth;

public class JwtTokenGenerator : ITokenGenerator, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IdentityUserManager _userManager;
    private readonly ICurrentTenant _currentTenant;

    public JwtTokenGenerator(
        IConfiguration configuration,
        IdentityUserManager userManager,
        ICurrentTenant currentTenant)
    {
        _configuration = configuration;
        _userManager = userManager;
        _currentTenant = currentTenant;
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
            new(AbpClaimTypes.UserId, user.Id.ToString()),
            new(AbpClaimTypes.UserName, user.UserName ?? string.Empty),
            new(AbpClaimTypes.Name, user.Name ?? user.UserName ?? string.Empty),
            new(AbpClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("remember_me", rememberMe ? "true" : "false")
        };

        if (user.TenantId.HasValue)
        {
            claims.Add(new Claim(AbpClaimTypes.TenantId, user.TenantId.Value.ToString()));
        }

        var shouldChangePassword = user.GetProperty<bool>(BookingHotelIdentityConsts.ShouldChangePasswordOnNextLoginProperty);
        claims.Add(new Claim("should_change_password", shouldChangePassword ? "true" : "false"));

        IList<string> roles;
        using (_currentTenant.Change(user.TenantId))
        {
            roles = await _userManager.GetRolesAsync(user);
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(AbpClaimTypes.Role, role));
        }

        // Nếu là tài khoản quản trị trực thuộc Tenant (có role 'admin' của Tenant),
        // bổ sung thêm Role Claim 'TenantAdmin' để phân hệ UI / Guard nhận diện chính xác
        if (user.TenantId.HasValue && roles.Contains("admin") && !roles.Contains(BookingHotelRoleConsts.TenantAdmin))
        {
            claims.Add(new Claim(AbpClaimTypes.Role, BookingHotelRoleConsts.TenantAdmin));
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

