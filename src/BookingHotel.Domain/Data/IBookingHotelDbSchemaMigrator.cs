using System.Threading.Tasks;

namespace BookingHotel.Data;

public interface IBookingHotelDbSchemaMigrator
{
    Task MigrateAsync();
}
