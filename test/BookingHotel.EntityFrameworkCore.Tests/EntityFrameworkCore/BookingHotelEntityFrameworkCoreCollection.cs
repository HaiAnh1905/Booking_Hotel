using Xunit;

namespace BookingHotel.EntityFrameworkCore;

[CollectionDefinition(BookingHotelTestConsts.CollectionDefinitionName)]
public class BookingHotelEntityFrameworkCoreCollection : ICollectionFixture<BookingHotelEntityFrameworkCoreFixture>
{

}
