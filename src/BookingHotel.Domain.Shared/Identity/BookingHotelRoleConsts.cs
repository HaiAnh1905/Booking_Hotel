namespace BookingHotel.Identity;

public static class BookingHotelRoleConsts
{
    public const string SystemAdmin = "SystemAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string HotelManager = "HotelManager";
    public const string Receptionist = "Receptionist";
    public const string Customer = "Customer";

    public static readonly string[] AllRoles =
    [
        SystemAdmin,
        TenantAdmin,
        HotelManager,
        Receptionist,
        Customer
    ];
}

