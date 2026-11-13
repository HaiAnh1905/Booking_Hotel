using System;
using System.Threading.Tasks;
using BookingHotel.Auth.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Auth;

public interface IAuthAppService : IApplicationService
{
    Task<LoginResultDto> LoginWithEmailAsync(LoginWithEmailDto input);

    Task<LoginResultDto> LoginWithGoogleAsync(GoogleAuthInputDto input);

    Task<RegisterStep1ResultDto> RegisterStep1Async(RegisterStep1InputDto input);

    Task<RegisterStep1ResultDto> RegisterGoogleInitAsync(GoogleAuthInputDto input);

    Task RegisterStep2Async(RegisterStep2InputDto input);

    Task<LoginResultDto> RegisterStep3Async(RegisterStep3InputDto input);

    Task SendPasswordResetCodeAsync(ForgotPasswordInputDto input);

    Task ResetPasswordAsync(ResetPasswordInputDto input);

    Task<ResetPasswordInfoDto> GetResetPasswordInfoAsync(Guid userId);

    Task UnlockUserAsync(Guid userId);
}

