using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Guests;

public class InternalGuestNote : FullAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid GuestId { get; set; }

    public virtual string NoteText { get; set; } = string.Empty;

    public virtual string Category { get; set; } = "General";

    public virtual bool IsAlert { get; set; }

    protected InternalGuestNote()
    {
    }

    public InternalGuestNote(
        Guid id,
        Guid hotelId,
        Guid guestId,
        string noteText,
        string category = "General",
        bool isAlert = false,
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        GuestId = guestId;
        NoteText = noteText;
        Category = category;
        IsAlert = isAlert;
        TenantId = tenantId;
    }
}
