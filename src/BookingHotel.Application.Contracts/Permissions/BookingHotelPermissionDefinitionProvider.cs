using BookingHotel.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Permissions;

public class BookingHotelPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(BookingHotelPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(BookingHotelPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<BookingHotelResource>(name);
    }
}
