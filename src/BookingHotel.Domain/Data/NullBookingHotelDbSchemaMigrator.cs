using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace BookingHotel.Data;

/* This is used if database provider does't define
 * IBookingHotelDbSchemaMigrator implementation.
 */
public class NullBookingHotelDbSchemaMigrator : IBookingHotelDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
