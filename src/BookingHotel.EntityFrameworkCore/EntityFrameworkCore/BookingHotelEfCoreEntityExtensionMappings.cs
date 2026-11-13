using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace BookingHotel.EntityFrameworkCore;

public static class BookingHotelEfCoreEntityExtensionMappings
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        BookingHotelGlobalFeatureConfigurator.Configure();
        BookingHotelModuleExtensionConfigurator.Configure();

        OneTimeRunner.Run(() =>
        {
            ObjectExtensionManager.Instance
                .MapEfCoreProperty<IdentityUser, int>(
                    Identity.BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty,
                    (entityBuilder, propertyBuilder) =>
                    {
                        propertyBuilder.HasDefaultValue(0);
                    }
                )
                .MapEfCoreProperty<IdentityUser, int>(
                    Identity.BookingHotelIdentityConsts.TotalFailedAttemptsProperty,
                    (entityBuilder, propertyBuilder) =>
                    {
                        propertyBuilder.HasDefaultValue(0);
                    }
                )
                .MapEfCoreProperty<IdentityUser, int>(
                    Identity.BookingHotelIdentityConsts.CurrentLockoutMinutesProperty,
                    (entityBuilder, propertyBuilder) =>
                    {
                        propertyBuilder.HasDefaultValue(0);
                    }
                )
                .MapEfCoreProperty<IdentityUser, string>(
                    Identity.BookingHotelIdentityConsts.DeactivatedReasonProperty,
                    (entityBuilder, propertyBuilder) =>
                    {
                        propertyBuilder.HasMaxLength(128);
                    }
                )
                .MapEfCoreProperty<IdentityUser, string>(
                    Identity.BookingHotelIdentityConsts.GoogleIdProperty,
                    (entityBuilder, propertyBuilder) =>
                    {
                        propertyBuilder.HasMaxLength(128);
                    }
                );
        });
    }
}
