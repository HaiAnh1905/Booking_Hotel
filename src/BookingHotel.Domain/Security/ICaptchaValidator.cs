using System.Threading.Tasks;

namespace BookingHotel.Security;

public interface ICaptchaValidator
{
    Task<bool> ValidateAsync(string captchaToken, string? remoteIp = null);
}

