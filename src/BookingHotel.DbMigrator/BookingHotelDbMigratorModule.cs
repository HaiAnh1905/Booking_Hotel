using BookingHotel.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace BookingHotel.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(BookingHotelEntityFrameworkCoreModule),
    typeof(BookingHotelApplicationContractsModule)
)]
public class BookingHotelDbMigratorModule : AbpModule
{
}
