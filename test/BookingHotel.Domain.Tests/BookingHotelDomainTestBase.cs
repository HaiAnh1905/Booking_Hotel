using Volo.Abp.Modularity;

namespace BookingHotel;

/* Inherit from this class for your domain layer tests. */
public abstract class BookingHotelDomainTestBase<TStartupModule> : BookingHotelTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
