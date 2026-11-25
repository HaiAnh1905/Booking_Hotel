using Volo.Abp.Application.Dtos;

namespace BookingHotel.Hotels.Dtos;

public class GetHotelsInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public HotelStatus? Status { get; set; }
}

