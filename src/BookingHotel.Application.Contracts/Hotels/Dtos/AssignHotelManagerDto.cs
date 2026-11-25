using System;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Hotels.Dtos;

public class AssignHotelManagerDto
{
    public Guid? ManagerUserId { get; set; }
}
