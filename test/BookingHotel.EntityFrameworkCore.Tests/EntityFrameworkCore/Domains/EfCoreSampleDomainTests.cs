using BookingHotel.Samples;
using Xunit;

namespace BookingHotel.EntityFrameworkCore.Domains;

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<BookingHotelEntityFrameworkCoreTestModule>
{

}
