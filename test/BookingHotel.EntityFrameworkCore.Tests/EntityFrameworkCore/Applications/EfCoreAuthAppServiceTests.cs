using BookingHotel.Tests.Auth;
using Xunit;

namespace BookingHotel.EntityFrameworkCore.Applications;

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreAuthAppServiceLoginTests : AuthAppService_Login_Tests<BookingHotelEntityFrameworkCoreTestModule>
{
}

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreAuthAppServiceRegistrationTests : AuthAppService_Registration_Tests<BookingHotelEntityFrameworkCoreTestModule>
{
}

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreAuthAppServiceGoogleAuthTests : AuthAppService_GoogleAuth_Tests<BookingHotelEntityFrameworkCoreTestModule>
{
}

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreAuthAppServicePasswordResetTests : AuthAppService_PasswordReset_Tests<BookingHotelEntityFrameworkCoreTestModule>
{
}

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreAdminUnlockUserTests : AdminUnlockUser_Tests<BookingHotelEntityFrameworkCoreTestModule>
{
}

[Collection(BookingHotelTestConsts.CollectionDefinitionName)]
public class EfCoreAuthAppServiceUnlockTests : AuthAppService_Unlock_Tests<BookingHotelEntityFrameworkCoreTestModule>
{
}

