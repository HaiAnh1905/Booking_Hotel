using System;
using System.Collections.Generic;

namespace BookingHotel.SystemDashboard.Dtos;

public class SystemDashboardOverviewDto
{
    // Metric 1: Tenants
    public int TenantCount { get; set; }
    public int NewTenantsThisMonth { get; set; }

    // Metric 2: Branches/Hotels
    public int OperatingBranchCount { get; set; }

    // Metric 3: Host Users
    public int ActiveHostUserCount { get; set; }
    public int TotalHostUserCount { get; set; }
    public int TwoFactorHostUserCount { get; set; }

    // Metric 4: Security Alerts
    public int SecurityAlertCount { get; set; }
    public int LockedOutUserCount { get; set; }

    // Activity Timeline
    public List<SystemActivityLogDto> RecentActivities { get; set; } = new();

    // Server Health
    public SystemServerHealthDto ServerHealth { get; set; } = new();
}

public class SystemActivityLogDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public string Level { get; set; } = "info"; // success, info, warning, danger
    public string? UserName { get; set; }
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? StatusCode { get; set; }
    public string TimeFormatted { get; set; } = string.Empty;
}

public class SystemServerHealthDto
{
    public string OpenIddictStatus { get; set; } = "Bình thường";
    public string DatabaseStatus { get; set; } = "Bình thường";
    public double DatabaseLatencyMs { get; set; }
    public double MemoryUsageMb { get; set; }
    public string ServerPlatform { get; set; } = ".NET 10.0 / Windows Host";
    public string UptimeFormatted { get; set; } = string.Empty;
}
