using Volo.Abp.Modularity;

namespace BookingHotel;

[DependsOn(
    typeof(BookingHotelApplicationModule),
    typeof(BookingHotelDomainTestModule)
)]
public class BookingHotelApplicationTestModule : AbpModule
{

}
