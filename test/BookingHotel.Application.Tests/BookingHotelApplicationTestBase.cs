using Volo.Abp.Modularity;

namespace BookingHotel;

public abstract class BookingHotelApplicationTestBase<TStartupModule> : BookingHotelTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
