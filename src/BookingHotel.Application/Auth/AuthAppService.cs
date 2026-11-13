using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using BookingHotel.Auth.Dtos;
using BookingHotel.Auth.ErrorCodes;
using BookingHotel.Identity;
using BookingHotel.Localization;
using BookingHotel.Permissions;
using BookingHotel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Caching;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;

namespace BookingHotel.Auth;

public class AuthAppService : ApplicationService, IAuthAppService
{
    private readonly IdentityUserManager _userManager;
    private readonly IBruteForceManager _bruteForceManager;
    private readonly ICaptchaValidator _captchaValidator;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IDistributedCache<UserRegistrationStateCacheItem> _registrationCache;
    private readonly IEmailSender _emailSender;
    private readonly IdentitySecurityLogManager _securityLogManager;

    public AuthAppService(
        IdentityUserManager userManager,
        IBruteForceManager bruteForceManager,
        ICaptchaValidator captchaValidator,
        IGoogleTokenValidator googleTokenValidator,
        ITokenGenerator tokenGenerator,
        IDistributedCache<UserRegistrationStateCacheItem> registrationCache,
        IEmailSender emailSender,
        IdentitySecurityLogManager securityLogManager)
    {
        _userManager = userManager;
        _bruteForceManager = bruteForceManager;
        _captchaValidator = captchaValidator;
        _googleTokenValidator = googleTokenValidator;
        _tokenGenerator = tokenGenerator;
        _registrationCache = registrationCache;
        _emailSender = emailSender;
        _securityLogManager = securityLogManager;
        LocalizationResource = typeof(BookingHotelResource);
    }

    public async Task<LoginResultDto> LoginWithEmailAsync(LoginWithEmailDto input)
    {
        var user = await _userManager.FindByEmailAsync(input.Email);
        if (user == null)
        {
            return new LoginResultDto
            {
                IsSuccess = false,
                Message = L["Auth:InvalidCredentialsDefault"]
            };
        }

        var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, input.Password);
        var evalResult = await _bruteForceManager.EvaluateLoginAttemptAsync(user, isPasswordCorrect);

        await _securityLogManager.SaveAsync(new IdentitySecurityLogContext
        {
            Identity = "Identity",
            Action = isPasswordCorrect ? "LoginSucceeded" : "LoginFailed",
            UserName = user.UserName
        });

        if (!evalResult.IsAllowed)
        {
            string message;
            if (evalResult.IsDeactivated)
            {
                message = L["Auth:AccountDeactivated"];
            }
            else if (evalResult.IsLockedOut)
            {
                message = string.Format(L["Auth:AccountLocked"], evalResult.LockoutMinutes);
            }
            else
            {
                var key = evalResult.MessageKey ?? "Auth:InvalidCredentials";
                message = string.Format(L[key], evalResult.RemainingAttemptsInCycle);
            }

            return new LoginResultDto
            {
                IsSuccess = false,
                IsLockedOut = evalResult.IsLockedOut,
                RemainingLockoutSeconds = evalResult.RemainingLockoutSeconds,
                RemainingAttemptsInCycle = evalResult.RemainingAttemptsInCycle,
                IsDeactivated = evalResult.IsDeactivated,
                Message = message
            };
        }

        await _bruteForceManager.ResetCountersAsync(user);

        var (token, expiresIn) = await _tokenGenerator.GenerateTokenAsync(user, input.RememberMe);

        return new LoginResultDto
        {
            IsSuccess = true,
            AccessToken = token,
            ExpiresIn = expiresIn,
            TokenType = "Bearer"
        };
    }

    public async Task<LoginResultDto> LoginWithGoogleAsync(GoogleAuthInputDto input)
    {
        var googleInfo = await _googleTokenValidator.ValidateAsync(input.IdToken);
        if (googleInfo == null)
        {
            throw new UserFriendlyException(L["Auth:InvalidCredentialsDefault"]);
        }

        var user = await _userManager.FindByEmailAsync(googleInfo.Email);
        if (user == null)
        {
            var registrationToken = Guid.NewGuid();
            var cacheItem = new UserRegistrationStateCacheItem
            {
                RegistrationId = registrationToken,
                Email = googleInfo.Email,
                GoogleId = googleInfo.Subject,
                FullName = googleInfo.Name,
                CurrentStep = RegistrationStep.Step1Completed,
                CreatedAt = DateTime.UtcNow
            };

            await _registrationCache.SetAsync(
                registrationToken.ToString(),
                cacheItem,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                }
            );

            return new LoginResultDto
            {
                IsSuccess = false,
                RequiresRegistration = true,
                RegistrationToken = registrationToken,
                Email = googleInfo.Email,
                PrefilledName = googleInfo.Name,
                Message = "UserNotRegistered"
            };
        }

        if (!user.IsActive)
        {
            return new LoginResultDto
            {
                IsSuccess = false,
                IsDeactivated = true,
                Message = L["Auth:AccountDeactivated"]
            };
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            var remaining = (int)Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalSeconds);
            return new LoginResultDto
            {
                IsSuccess = false,
                IsLockedOut = true,
                RemainingLockoutSeconds = remaining,
                Message = string.Format(L["Auth:AccountLocked"], Math.Ceiling(remaining / 60.0))
            };
        }

        if (user.GetProperty<string>(BookingHotelIdentityConsts.GoogleIdProperty) != googleInfo.Subject)
        {
            user.SetProperty(BookingHotelIdentityConsts.GoogleIdProperty, googleInfo.Subject);
            await _userManager.UpdateAsync(user);
        }

        await _bruteForceManager.ResetCountersAsync(user);

        var (token, expiresIn) = await _tokenGenerator.GenerateTokenAsync(user);

        return new LoginResultDto
        {
            IsSuccess = true,
            AccessToken = token,
            ExpiresIn = expiresIn,
            TokenType = "Bearer"
        };
    }

    public async Task<RegisterStep1ResultDto> RegisterStep1Async(RegisterStep1InputDto input)
    {
        var existing = await _userManager.FindByEmailAsync(input.Email);
        if (existing != null)
        {
            throw new UserFriendlyException(L["Auth:EmailAlreadyExists"], BookingHotelAuthErrorCodes.EmailAlreadyExists);
        }

        var registrationToken = Guid.NewGuid();
        var cacheItem = new UserRegistrationStateCacheItem
        {
            RegistrationId = registrationToken,
            Email = input.Email,
            RawPassword = input.Password,
            CurrentStep = RegistrationStep.Step1Completed,
            CreatedAt = DateTime.UtcNow
        };

        await _registrationCache.SetAsync(
            registrationToken.ToString(),
            cacheItem,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            }
        );

        return new RegisterStep1ResultDto
        {
            RegistrationToken = registrationToken,
            Email = input.Email,
            NextStep = "Step2"
        };
    }

    public async Task<RegisterStep1ResultDto> RegisterGoogleInitAsync(GoogleAuthInputDto input)
    {
        var googleInfo = await _googleTokenValidator.ValidateAsync(input.IdToken);
        if (googleInfo == null)
        {
            throw new UserFriendlyException(L["Auth:InvalidCredentialsDefault"]);
        }

        var existing = await _userManager.FindByEmailAsync(googleInfo.Email);
        if (existing != null)
        {
            throw new UserFriendlyException(L["Auth:EmailAlreadyExists"], BookingHotelAuthErrorCodes.EmailAlreadyExists);
        }

        var registrationToken = Guid.NewGuid();

        var cacheItem = new UserRegistrationStateCacheItem
        {
            RegistrationId = registrationToken,
            Email = googleInfo.Email,
            GoogleId = googleInfo.Subject,
            FullName = googleInfo.Name,
            CurrentStep = RegistrationStep.Step1Completed,
            CreatedAt = DateTime.UtcNow
        };

        await _registrationCache.SetAsync(
            registrationToken.ToString(),
            cacheItem,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            }
        );

        return new RegisterStep1ResultDto
        {
            RegistrationToken = registrationToken,
            Email = googleInfo.Email,
            PrefilledName = googleInfo.Name,
            NextStep = "Step2"
        };
    }

    public async Task RegisterStep2Async(RegisterStep2InputDto input)
    {
        var cacheItem = await _registrationCache.GetAsync(input.RegistrationToken.ToString());
        if (cacheItem == null || cacheItem.CurrentStep < RegistrationStep.Step1Completed)
        {
            throw new UserFriendlyException(L["Auth:InvalidOnboardingToken"], BookingHotelAuthErrorCodes.InvalidOnboardingToken);
        }

        cacheItem.FullName = input.FullName;
        cacheItem.CurrentStep = RegistrationStep.Step2Completed;

        await _registrationCache.SetAsync(
            input.RegistrationToken.ToString(),
            cacheItem,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            }
        );
    }

    public async Task<LoginResultDto> RegisterStep3Async(RegisterStep3InputDto input)
    {
        var cacheItem = await _registrationCache.GetAsync(input.RegistrationToken.ToString());
        if (cacheItem == null || cacheItem.CurrentStep != RegistrationStep.Step2Completed)
        {
            throw new UserFriendlyException(L["Auth:InvalidOnboardingToken"], BookingHotelAuthErrorCodes.InvalidOnboardingToken);
        }

        var existing = await _userManager.FindByEmailAsync(cacheItem.Email);
        if (existing != null)
        {
            throw new UserFriendlyException(L["Auth:EmailAlreadyExists"], BookingHotelAuthErrorCodes.EmailAlreadyExists);
        }

        var user = new IdentityUser(
            GuidGenerator.Create(),
            cacheItem.Email,
            cacheItem.Email,
            CurrentTenant.Id
        );

        user.Name = cacheItem.FullName;
        user.SetPhoneNumber(input.PhoneNumber, false);
        user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.TotalFailedAttemptsProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 0);

        if (!string.IsNullOrWhiteSpace(cacheItem.GoogleId))
        {
            user.SetProperty(BookingHotelIdentityConsts.GoogleIdProperty, cacheItem.GoogleId);
        }

        IdentityResult createResult;
        if (!string.IsNullOrWhiteSpace(cacheItem.RawPassword))
        {
            createResult = await _userManager.CreateAsync(user, cacheItem.RawPassword);
        }
        else
        {
            createResult = await _userManager.CreateAsync(user);
        }

        if (!createResult.Succeeded)
        {
            throw new UserFriendlyException(string.Join(", ", createResult.Errors));
        }

        // Gán vai trò mặc định: Customer (nếu có trong hệ thống)
        try
        {
            await _userManager.AddToRoleAsync(user, BookingHotelRoleConsts.Customer);
        }
        catch
        {
            // Vai trò Customer có thể chưa khởi tạo trong môi trường test
        }

        await _registrationCache.RemoveAsync(input.RegistrationToken.ToString());

        await _securityLogManager.SaveAsync(new IdentitySecurityLogContext
        {
            Identity = "Identity",
            Action = "UserRegistered",
            UserName = user.UserName
        });

        var (token, expiresIn) = await _tokenGenerator.GenerateTokenAsync(user);

        return new LoginResultDto
        {
            IsSuccess = true,
            AccessToken = token,
            ExpiresIn = expiresIn,
            TokenType = "Bearer"
        };
    }

    public async Task SendPasswordResetCodeAsync(ForgotPasswordInputDto input)
    {
        var isCaptchaValid = await _captchaValidator.ValidateAsync(input.CaptchaToken);
        if (!isCaptchaValid)
        {
            throw new UserFriendlyException(L["Auth:InvalidCaptcha"], BookingHotelAuthErrorCodes.InvalidCaptcha);
        }

        var user = await _userManager.FindByEmailAsync(input.Email);
        if (user == null)
        {
            // Trả về thành công ảo để chống brute force liệt kê email (Account Enumeration)
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetLink = $"http://localhost:4200/auth/reset-password?userId={user.Id}&email={Uri.EscapeDataString(user.Email ?? "")}&token={Uri.EscapeDataString(token)}";
        var emailSubject = $"[Booking Hotel] {L["Auth:ResetPassword"].Value}";
        var emailBody = BuildPasswordResetEmailHtml(user.Name ?? user.UserName, user.Email!, resetLink);

        Logger.LogInformation("================ [PASSWORD RESET EMAIL] ================");
        Logger.LogInformation("To: {Email}", user.Email);
        Logger.LogInformation("Subject: {Subject}", emailSubject);
        Logger.LogInformation("Reset Link: {ResetLink}", resetLink);
        Logger.LogInformation("=========================================================");

        try
        {
            var logsDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            await System.IO.File.WriteAllTextAsync(Path.Combine(logsDir, "last-reset-email.html"), emailBody);
        }
        catch
        {
            // Bỏ qua nếu không ghi được file cục bộ
        }

        try
        {
            await _emailSender.SendAsync(
                user.Email!,
                emailSubject,
                emailBody,
                isBodyHtml: true
            );
            Logger.LogInformation("Successfully sent password reset email via SMTP to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send reset password email via SMTP. Reset link: {ResetLink}", resetLink);
        }
    }

    private string BuildPasswordResetEmailHtml(string userName, string userEmail, string resetLink)
    {
        return $@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" lang=""vi"">
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
    <title>Đặt lại mật khẩu - Booking Hotel</title>
    <style type=""text/css"">
        body, table, td, a {{ -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; }}
        table, td {{ mso-table-lspace: 0pt; mso-table-rspace: 0pt; }}
        table {{ border-collapse: collapse !important; }}
        body {{ height: 100% !important; margin: 0 !important; padding: 0 !important; width: 100% !important; }}
    </style>
</head>
<body style=""margin: 0; padding: 0; background-color: #f4f6f8; font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
    <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f6f8; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 16px;"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0; border-collapse: separate;"">
                    <!-- Brand Top Bar -->
                    <tr>
                        <td height=""5"" style=""background-color: #990000; font-size: 1px; line-height: 1px;"">&nbsp;</td>
                    </tr>
                    
                    <!-- Header with Logo -->
                    <tr>
                        <td align=""center"" style=""padding: 36px 24px 24px 24px; border-bottom: 1px solid #f1f5f9;"">
                            <div style=""font-size: 28px; font-weight: 800; line-height: 1.2; letter-spacing: -0.5px;"">
                                <span style=""color: #990000;"">Booking</span>&nbsp;<span style=""color: #111111;"">Hotel</span>
                            </div>
                            <div style=""color: #94a3b8; font-size: 11px; text-transform: uppercase; letter-spacing: 2.5px; margin-top: 6px; font-weight: 600;"">
                                LUXURY STAYS &amp; PREMIUM HOSPITALITY
                            </div>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 36px 36px 28px 36px;"">
                            <!-- Lock Icon Badge -->
                            <div style=""text-align: center; margin-bottom: 24px;"">
                                <div style=""display: inline-block; width: 64px; height: 64px; line-height: 64px; border-radius: 50%; background-color: #fff1f2; text-align: center; font-size: 28px; border: 1px solid #ffe4e6;"">
                                    🔒
                                </div>
                            </div>

                            <h1 style=""color: #0f172a; font-size: 22px; font-weight: 700; margin: 0 0 18px 0; text-align: center; letter-spacing: -0.3px;"">
                                Yêu cầu đặt lại mật khẩu
                            </h1>
                            
                            <p style=""color: #334155; font-size: 15px; line-height: 1.6; margin: 0 0 16px 0;"">
                                Xin chào <strong>{userName}</strong>,
                            </p>
                            
                            <p style=""color: #475569; font-size: 14px; line-height: 1.6; margin: 0 0 26px 0;"">
                                Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản đăng ký với email <strong style=""color: #990000;"">{userEmail}</strong> trên hệ thống <strong>Booking Hotel</strong>.
                            </p>

                            <!-- CTA Button -->
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin: 28px 0; border-collapse: collapse;"">
                                <tr>
                                    <td align=""center"">
                                        <table border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate;"">
                                            <tr>
                                                <td align=""center"" style=""border-radius: 8px; background-color: #990000;"">
                                                    <a href=""{resetLink}"" target=""_blank"" style=""display: inline-block; background-color: #990000; color: #ffffff !important; padding: 15px 38px; font-size: 15px; font-weight: 700; text-decoration: none; border-radius: 8px; border: 1px solid #990000; letter-spacing: 0.3px;"">
                                                        Đặt lại mật khẩu ngay
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Expiration Warning Callout -->
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #fff8f8; border-left: 4px solid #990000; border-radius: 6px; margin: 26px 0; border-collapse: separate;"">
                                <tr>
                                    <td style=""padding: 14px 16px;"">
                                        <p style=""color: #881337; font-size: 13px; margin: 0; line-height: 1.5;"">
                                            ⏱️ <strong>Lưu ý bảo mật:</strong> Liên kết này chỉ có hiệu lực trong vòng <strong>15 phút</strong>. Sau thời gian này, bạn sẽ cần tạo một yêu cầu mới.
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Fallback Link -->
                            <p style=""color: #64748b; font-size: 12px; line-height: 1.6; margin: 0 0 8px 0;"">
                                Nếu nút bấm phía trên không phản hồi, bạn vui lòng sao chép và dán liên kết bảo mật dưới đây vào thanh địa chỉ của trình duyệt:
                            </p>
                            
                            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; margin: 0 0 24px 0; border-collapse: separate;"">
                                <tr>
                                    <td style=""padding: 12px; word-break: break-all;"">
                                        <a href=""{resetLink}"" style=""color: #990000; font-size: 12px; text-decoration: underline; word-break: break-all;"">{resetLink}</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""color: #64748b; font-size: 13px; line-height: 1.5; margin: 0;"">
                                Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua thư hoặc thông báo ngay cho ban quản trị. Tài khoản của bạn vẫn được bảo mật an toàn.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding: 24px 24px; background-color: #f8fafc; border-top: 1px solid #f1f5f9;"">
                            <p style=""color: #64748b; font-size: 12px; margin: 0 0 6px 0;"">
                                © 2026 <strong>Booking Hotel</strong>. Bảo lưu mọi quyền.
                            </p>
                            <p style=""color: #94a3b8; font-size: 11px; margin: 0;"">
                                Email tự động được gửi từ Hệ thống Quản lý Khách sạn &amp; Đặt phòng Booking Hotel.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    public async Task ResetPasswordAsync(ResetPasswordInputDto input)
    {
        var user = await _userManager.FindByIdAsync(input.UserId);
        if (user == null)
        {
            throw new UserFriendlyException(L["Auth:InvalidResetToken"], BookingHotelAuthErrorCodes.InvalidResetToken);
        }

        var result = await _userManager.ResetPasswordAsync(user, input.ResetToken, input.NewPassword);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException(L["Auth:InvalidResetToken"], BookingHotelAuthErrorCodes.InvalidResetToken);
        }

        user.SetIsActive(true);
        await _bruteForceManager.ResetCountersAsync(user);
    }

    public async Task<ResetPasswordInfoDto> GetResetPasswordInfoAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return new ResetPasswordInfoDto { Email = string.Empty, MaskedEmail = string.Empty };
        }

        return new ResetPasswordInfoDto
        {
            Email = user.Email,
            MaskedEmail = MaskEmail(user.Email)
        };
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        var parts = email.Split('@');
        var name = parts[0];
        var prefix = name.Length >= 3 ? name.Substring(0, 3) : name;
        return $"{prefix}***";
    }

    [Authorize(BookingHotelPermissions.Identity.UnlockUser)]
    public async Task UnlockUserAsync(Guid userId)
    {
        IdentityUser? user;
        using (DataFilter.Disable<IMultiTenant>())
        {
            user = await _userManager.FindByIdAsync(userId.ToString());
        }

        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), userId);
        }

        if (CurrentTenant.Id.HasValue)
        {
            // Tenant Admin: only allow unlocking internal staff belonging to this Tenant
            if (user.TenantId != CurrentTenant.Id)
            {
                throw new AbpAuthorizationException("Quản trị viên Tenant chỉ có quyền mở khóa cho tài khoản nhân sự nội bộ trong cùng Tenant.");
            }
        }
        else
        {
            // Host Admin: only unlock Host accounts (prevent unlocking Tenant staff without switching context)
            if (user.TenantId != null)
            {
                throw new AbpAuthorizationException("Quản trị viên hệ thống Host chỉ mở khóa các tài khoản thuộc cấp độ Host.");
            }
        }

        await _bruteForceManager.UnlockAccountAsync(user);

        await _securityLogManager.SaveAsync(new IdentitySecurityLogContext
        {
            Identity = "Identity",
            Action = "AdminUnlockedUser",
            UserName = user.UserName
        });
    }
}
