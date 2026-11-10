using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookingHotel.Data;
using Volo.Abp.DependencyInjection;

namespace BookingHotel.EntityFrameworkCore;

public class EntityFrameworkCoreBookingHotelDbSchemaMigrator
    : IBookingHotelDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreBookingHotelDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the BookingHotelDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<BookingHotelDbContext>()
            .Database
            .MigrateAsync();
    }
}
