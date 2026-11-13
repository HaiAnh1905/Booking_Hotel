using BookingHotel.Data;
using Xunit;

namespace BookingHotel.EntityFrameworkCore.Domains;

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreBookingHotelDataSeedContributorTests : BookingHotelDataSeedContributorTests<BookingHotelEntityFrameworkCoreTestModule>
{
}

