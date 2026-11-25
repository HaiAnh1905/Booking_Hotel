namespace BookingHotel.Rooms;

public enum PriceAdjustmentType
{
    FixedPrice = 1,
    Percentage = 2,
    RestrictionsOnly = 3
}

public enum PriceConflictResolution
{
    Cancel = 0,
    ReplaceExisting = 1,
    SkipExisting = 2
}

public enum PricingAuditActionType
{
    CreatePriceOverride = 1,
    UpdatePriceOverride = 2,
    DeletePriceOverride = 3,
    UpdateBasePrice = 4,
    ToggleRatePlanStatus = 5,
    UpdateRestrictions = 6
}
