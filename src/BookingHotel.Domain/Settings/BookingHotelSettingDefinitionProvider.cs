using Volo.Abp.Settings;

namespace BookingHotel.Settings;

public class BookingHotelSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(BookingHotelSettings.MySetting1));
    }
}
