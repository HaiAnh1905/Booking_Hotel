using BookingHotel.Samples;
using Xunit;

namespace BookingHotel.EntityFrameworkCore.Applications;

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<BookingHotelEntityFrameworkCoreTestModule>
{

}
