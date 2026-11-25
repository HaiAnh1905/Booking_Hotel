using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookingHotel.Complaints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace BookingHotel.Manager.Feedback;

public class ComplaintSlaCheckWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComplaintSlaCheckWorker> _logger;

    public ComplaintSlaCheckWorker(
        IServiceProvider serviceProvider,
        ILogger<ComplaintSlaCheckWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ComplaintSlaCheckWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOverdueComplaintsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing ComplaintSlaCheckWorker.");
            }

            // Quét định kỳ mỗi 5 phút
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task CheckOverdueComplaintsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        using var uow = uowManager.Begin();

        var complaintRepo = scope.ServiceProvider.GetRequiredService<IRepository<Complaint, Guid>>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.Now;
        var query = await complaintRepo.GetQueryableAsync();

        var overdueComplaints = query
            .Where(c => c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Escalated && c.SlaDeadline < now)
            .ToList();

        if (overdueComplaints.Any())
        {
            _logger.LogWarning("Found {Count} complaints that breached 48h SLA deadline. Auto-escalating to Tenant Admin...", overdueComplaints.Count);

            foreach (var complaint in overdueComplaints)
            {
                complaint.Status = ComplaintStatus.Escalated;
                complaint.EscalatedAt = now;
                await complaintRepo.UpdateAsync(complaint);
            }

            await uow.SaveChangesAsync();
        }
    }
}
