using System;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Approvals.Dtos;
using BookingHotel.Identity;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using Xunit;

namespace BookingHotel.Approvals;

public abstract class OperationalApprovalAppServiceTests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IOperationalApprovalAppService ApprovalAppService { get; }
    protected IRepository<OperationalApprovalTicket, Guid> TicketRepository { get; }
    protected IRepository<UserHotelAssignment, Guid> AssignmentRepository { get; }
    protected ICurrentUser CurrentUser { get; }
    protected ICurrentTenant CurrentTenant { get; }

    protected OperationalApprovalAppServiceTests()
    {
        ApprovalAppService = GetRequiredService<IOperationalApprovalAppService>();
        TicketRepository = GetRequiredService<IRepository<OperationalApprovalTicket, Guid>>();
        AssignmentRepository = GetRequiredService<IRepository<UserHotelAssignment, Guid>>();
        CurrentUser = GetRequiredService<ICurrentUser>();
        CurrentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Should_Create_Pending_Approval_Ticket_Successfully()
    {
        var hotelId = Guid.NewGuid();
        var input = new CreateApprovalTicketDto
        {
            HotelId = hotelId,
            TicketType = ApprovalTicketType.DiscountOverride,
            RoomNumber = "P.301",
            Amount = 150000,
            DiscountPercent = 10,
            Reason = "Khách hàng thân thiết kỷ niệm sinh nhật"
        };

        var ticket = await ApprovalAppService.CreateAsync(input);

        ticket.ShouldNotBeNull();
        ticket.Id.ShouldNotBe(Guid.Empty);
        ticket.HotelId.ShouldBe(hotelId);
        ticket.TicketType.ShouldBe(ApprovalTicketType.DiscountOverride);
        ticket.Status.ShouldBe(ApprovalTicketStatus.Pending);
        ticket.Reason.ShouldBe("Khách hàng thân thiết kỷ niệm sinh nhật");
        ticket.DiscountPercent.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Approve_Pending_Ticket_By_Manager()
    {
        var hotelId = Guid.NewGuid();
        var created = await ApprovalAppService.CreateAsync(new CreateApprovalTicketDto
        {
            HotelId = hotelId,
            TicketType = ApprovalTicketType.PenaltyWaiver,
            Reason = "Khách bị hoãn chuyến bay vì bão"
        });

        var approved = await ApprovalAppService.ApproveAsync(created.Id);

        approved.Status.ShouldBe(ApprovalTicketStatus.Approved);
        approved.ReviewedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Reject_Pending_Ticket_With_Reason()
    {
        var hotelId = Guid.NewGuid();
        var created = await ApprovalAppService.CreateAsync(new CreateApprovalTicketDto
        {
            HotelId = hotelId,
            TicketType = ApprovalTicketType.EarlyCheckIn,
            Reason = "Khách muốn nhận phòng lúc 6 giờ sáng không tính phí"
        });

        var rejected = await ApprovalAppService.RejectAsync(created.Id, new ReviewApprovalTicketDto
        {
            RejectionReason = "Khách sạn hết phòng sạch vào khung giờ sáng sớm"
        });

        rejected.Status.ShouldBe(ApprovalTicketStatus.Rejected);
        rejected.RejectionReason.ShouldBe("Khách sạn hết phòng sạch vào khung giờ sáng sớm");
        rejected.ReviewedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Throw_When_Approving_Ticket_Not_In_Pending_State()
    {
        var hotelId = Guid.NewGuid();
        var created = await ApprovalAppService.CreateAsync(new CreateApprovalTicketDto
        {
            HotelId = hotelId,
            TicketType = ApprovalTicketType.RoomUpgrade,
            Reason = "Khách VIP muốn nâng hạng phòng Suite"
        });

        await ApprovalAppService.ApproveAsync(created.Id);

        // Thử duyệt lại lần 2
        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await ApprovalAppService.ApproveAsync(created.Id);
        });

        ex.Code.ShouldBe("BookingHotel:TicketNotInPendingState");
    }

    [Fact]
    public async Task Should_Throw_When_Rejection_Reason_Is_Empty()
    {
        var hotelId = Guid.NewGuid();
        var created = await ApprovalAppService.CreateAsync(new CreateApprovalTicketDto
        {
            HotelId = hotelId,
            TicketType = ApprovalTicketType.LateCheckOut,
            Reason = "Khách muốn check out muộn đến 16h"
        });

        await Should.ThrowAsync<Exception>(async () =>
        {
            await ApprovalAppService.RejectAsync(created.Id, new ReviewApprovalTicketDto
            {
                RejectionReason = "   "
            });
        });
    }

    [Fact]
    public async Task Should_Filter_Tickets_By_Hotel_And_Status()
    {
        var hotel1 = Guid.NewGuid();
        var hotel2 = Guid.NewGuid();

        await ApprovalAppService.CreateAsync(new CreateApprovalTicketDto
        {
            HotelId = hotel1,
            TicketType = ApprovalTicketType.DiscountOverride,
            Reason = "Filter test hotel 1"
        });

        await ApprovalAppService.CreateAsync(new CreateApprovalTicketDto
        {
            HotelId = hotel2,
            TicketType = ApprovalTicketType.DiscountOverride,
            Reason = "Filter test hotel 2"
        });

        var listHotel1 = await ApprovalAppService.GetListAsync(new GetApprovalTicketListDto
        {
            HotelId = hotel1,
            Status = ApprovalTicketStatus.Pending
        });

        listHotel1.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        listHotel1.Items.All(t => t.HotelId == hotel1).ShouldBeTrue();
    }
}
