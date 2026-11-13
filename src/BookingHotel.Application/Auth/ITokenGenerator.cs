using System.Threading.Tasks;
using Volo.Abp.Identity;

namespace BookingHotel.Auth;

public interface ITokenGenerator
{
    Task<(string Token, int ExpiresIn)> GenerateTokenAsync(IdentityUser user, bool rememberMe = false);
}

