using BookingHotel.Frontdesk;
using Xunit;

namespace BookingHotel.EntityFrameworkCore.Applications;

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreFrontdeskAppServiceTests : FrontdeskAppServiceTests<BookingHotelEntityFrameworkCoreTestModule>
{
}
