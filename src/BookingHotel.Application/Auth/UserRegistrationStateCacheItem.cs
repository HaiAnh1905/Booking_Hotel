using System;

namespace BookingHotel.Auth;

public class UserRegistrationStateCacheItem
{
    public Guid RegistrationId { get; set; }
    public string Email { get; set; } = default!;
    public string? RawPassword { get; set; }
    public string? GoogleId { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public RegistrationStep CurrentStep { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum RegistrationStep
{
    Step1Completed = 1, // Đã nhập Email/Pass hoặc xác thực Google
    Step2Completed = 2, // Đã nhập Tên
    Completed = 3       // Đã nhập SĐT, đã tạo user trong CSDL
}

