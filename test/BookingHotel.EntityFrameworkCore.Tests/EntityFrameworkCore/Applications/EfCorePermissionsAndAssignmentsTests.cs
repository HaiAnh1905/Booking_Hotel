using BookingHotel.Identity;
using BookingHotel.Permissions;
using Xunit;

namespace BookingHotel.EntityFrameworkCore.Applications;

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCorePermissionDefinitionTests : BookingHotelPermissionDefinitionTests<BookingHotelEntityFrameworkCoreTestModule>
{
}

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreUserHotelAssignmentTests : UserHotelAssignmentTests<BookingHotelEntityFrameworkCoreTestModule>
{
}

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCorePermissionSecurityTests : BookingHotelPermissionSecurityTests<BookingHotelEntityFrameworkCoreTestModule>
{
}
