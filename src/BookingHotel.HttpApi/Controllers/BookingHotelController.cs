using BookingHotel.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace BookingHotel.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class BookingHotelController : AbpControllerBase
{
    protected BookingHotelController()
    {
        LocalizationResource = typeof(BookingHotelResource);
    }
}
