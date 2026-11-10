using Volo.Abp.Modularity;

namespace BookingHotel;

[DependsOn(
    typeof(BookingHotelDomainModule),
    typeof(BookingHotelTestBaseModule)
)]
public class BookingHotelDomainTestModule : AbpModule
{

}
