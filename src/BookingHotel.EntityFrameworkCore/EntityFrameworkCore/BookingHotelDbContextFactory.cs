using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BookingHotel.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class BookingHotelDbContextFactory : IDesignTimeDbContextFactory<BookingHotelDbContext>
{
    public BookingHotelDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        BookingHotelEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<BookingHotelDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new BookingHotelDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../BookingHotel.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
