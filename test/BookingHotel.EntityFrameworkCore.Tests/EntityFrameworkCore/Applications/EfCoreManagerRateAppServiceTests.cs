using BookingHotel.Manager;
using Xunit;

namespace BookingHotel.EntityFrameworkCore.Applications;

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreManagerRateAppServiceTests : ManagerRateAppServiceTests<BookingHotelEntityFrameworkCoreTestModule>
{
}
