using System;

namespace BookingHotel.TenantDashboard.Dtos;

public class GetTenantDashboardInput
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
