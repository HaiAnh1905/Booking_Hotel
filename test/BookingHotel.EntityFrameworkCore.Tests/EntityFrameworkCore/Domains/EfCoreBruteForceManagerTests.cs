using BookingHotel.Domain.Tests.Identity;
using Xunit;

namespace BookingHotel.EntityFrameworkCore.Domains;

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreBruteForceManagerTests : BruteForceManagerTests<BookingHotelEntityFrameworkCoreTestModule>
{
}

