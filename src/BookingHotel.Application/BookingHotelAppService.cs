using BookingHotel.Localization;
using Volo.Abp.Application.Services;

namespace BookingHotel;

/* Inherit your application services from this class.
 */
public abstract class BookingHotelAppService : ApplicationService
{
    protected BookingHotelAppService()
    {
        LocalizationResource = typeof(BookingHotelResource);
    }
}
