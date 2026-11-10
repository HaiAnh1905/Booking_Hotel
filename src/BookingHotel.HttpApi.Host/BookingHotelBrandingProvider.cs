using Microsoft.Extensions.Localization;
using BookingHotel.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace BookingHotel;

[Dependency(ReplaceServices = true)]
public class BookingHotelBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<BookingHotelResource> _localizer;

    public BookingHotelBrandingProvider(IStringLocalizer<BookingHotelResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];

    public override string? LogoUrl => "~/images/logo/leptonxlite/logo-light-thumbnail.png";

    public override string? LogoIconUrl => "~/images/logo/leptonxlite/logo-dark-thumbnail.png";
}
