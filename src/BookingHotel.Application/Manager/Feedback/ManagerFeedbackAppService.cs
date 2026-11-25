using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Complaints;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Reviews;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace BookingHotel.Manager.Feedback;

[Authorize(BookingHotelPermissions.Reviews.Default)]
public class ManagerFeedbackAppService : ManagerAppServiceBase, IManagerFeedbackAppService
{
    private readonly IRepository<Review, Guid> _reviewRepository;
    private readonly IRepository<Complaint, Guid> _complaintRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IdentityUserManager _userManager;

    public ManagerFeedbackAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Review, Guid> reviewRepository,
        IRepository<Complaint, Guid> complaintRepository,
        IRepository<Booking, Guid> bookingRepository,
        IdentityUserManager userManager)
        : base(userHotelAssignmentRepository)
    {
        _reviewRepository = reviewRepository;
        _complaintRepository = complaintRepository;
        _bookingRepository = bookingRepository;
        _userManager = userManager;
    }

    public async Task<List<ReviewDto>> GetReviewsAsync(Guid hotelId, int? minScore = null)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var query = await _reviewRepository.GetQueryableAsync();
        query = query.Where(r => r.HotelId == hotelId);

        if (minScore.HasValue)
        {
            query = query.Where(r => r.OverallScore >= minScore.Value);
        }

        var reviews = query.OrderByDescending(r => r.CreationTime).ToList();

        var bookingIds = reviews.Select(r => r.BookingId).Distinct().ToList();
        var bookings = (await _bookingRepository.GetQueryableAsync())
            .Where(b => bookingIds.Contains(b.Id))
            .ToDictionary(b => b.Id, b => b);

        var replierIds = reviews.Where(r => r.RepliedById.HasValue).Select(r => r.RepliedById!.Value).Distinct().ToList();
        var replierDict = new Dictionary<Guid, string>();
        foreach (var uid in replierIds)
        {
            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user != null)
            {
                replierDict[uid] = user.UserName;
            }
        }

        return reviews.Select(r =>
        {
            bookings.TryGetValue(r.BookingId, out var booking);
            var replierName = r.RepliedById.HasValue && replierDict.ContainsKey(r.RepliedById.Value)
                ? replierDict[r.RepliedById.Value]
                : (r.RepliedById.HasValue ? "Staff" : null);

            return new ReviewDto
            {
                Id = r.Id,
                HotelId = r.HotelId,
                BookingCode = booking?.BookingCode ?? "N/A",
                GuestFullName = booking?.GuestFullName ?? "Anonymous Guest",
                CleanlinessScore = r.CleanlinessScore,
                StaffScore = r.StaffScore,
                AmenitiesScore = r.AmenitiesScore,
                LocationScore = r.LocationScore,
                OverallScore = r.OverallScore,
                Comment = r.Comment,
                CreationTime = r.CreationTime,
                OfficialReply = r.OfficialReply,
                RepliedByName = replierName,
                RepliedAt = r.RepliedAt
            };
        }).ToList();
    }

    [Authorize(BookingHotelPermissions.Reviews.Reply)]
    public async Task<ReviewDto> ReplyToReviewAsync(Guid reviewId, ReplyReviewDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var review = await _reviewRepository.GetAsync(reviewId);
        if (review.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        review.OfficialReply = input.ReplyContent;
        review.RepliedById = CurrentUser.GetId();
        review.RepliedAt = Clock.Now;

        await _reviewRepository.UpdateAsync(review);

        var booking = await _bookingRepository.FindAsync(review.BookingId);

        return new ReviewDto
        {
            Id = review.Id,
            HotelId = review.HotelId,
            BookingCode = booking?.BookingCode ?? "N/A",
            GuestFullName = booking?.GuestFullName ?? "Guest",
            CleanlinessScore = review.CleanlinessScore,
            StaffScore = review.StaffScore,
            AmenitiesScore = review.AmenitiesScore,
            LocationScore = review.LocationScore,
            OverallScore = review.OverallScore,
            Comment = review.Comment,
            CreationTime = review.CreationTime,
            OfficialReply = review.OfficialReply,
            RepliedByName = CurrentUser.UserName,
            RepliedAt = review.RepliedAt
        };
    }

    public async Task<List<ComplaintDto>> GetComplaintsAsync(Guid hotelId, ComplaintStatus? status = null)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var query = await _complaintRepository.GetQueryableAsync();
        query = query.Where(c => c.HotelId == hotelId);

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var complaints = query.OrderByDescending(c => c.CreationTime).ToList();

        var bookingIds = complaints.Where(c => c.BookingId.HasValue).Select(c => c.BookingId!.Value).Distinct().ToList();
        var bookings = (await _bookingRepository.GetQueryableAsync())
            .Where(b => bookingIds.Contains(b.Id))
            .ToDictionary(b => b.Id, b => b);

        var resolverIds = complaints.Where(c => c.ResolvedById.HasValue).Select(c => c.ResolvedById!.Value).Distinct().ToList();
        var resolverDict = new Dictionary<Guid, string>();
        foreach (var uid in resolverIds)
        {
            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user != null)
            {
                resolverDict[uid] = user.UserName;
            }
        }

        var result = new List<ComplaintDto>();
        var now = Clock.Now;

        foreach (var c in complaints)
        {
            bookings.TryGetValue(c.BookingId ?? Guid.Empty, out var booking);

            var isOverdue = now > c.SlaDeadline && c.Status != ComplaintStatus.Resolved;
            var hoursRemaining = isOverdue ? 0 : Math.Max(0, Math.Round((c.SlaDeadline - now).TotalHours, 1));

            // Auto-escalate if overdue and still Open/InInvestigation
            if (isOverdue && (c.Status == ComplaintStatus.Open || c.Status == ComplaintStatus.InInvestigation))
            {
                c.Status = ComplaintStatus.Escalated;
                c.EscalatedAt = now;
                await _complaintRepository.UpdateAsync(c);
            }

            var resolverName = c.ResolvedById.HasValue && resolverDict.ContainsKey(c.ResolvedById.Value)
                ? resolverDict[c.ResolvedById.Value]
                : (c.ResolvedById.HasValue ? "Staff" : null);

            result.Add(new ComplaintDto
            {
                Id = c.Id,
                HotelId = c.HotelId,
                Title = c.Title,
                Description = c.Description,
                GuestFullName = booking?.GuestFullName ?? "Customer",
                GuestPhone = booking?.GuestPhone ?? "N/A",
                BookingCode = booking?.BookingCode,
                Priority = c.Priority,
                Status = c.Status,
                CreationTime = c.CreationTime,
                SlaDeadline = c.SlaDeadline,
                HoursRemaining = hoursRemaining,
                IsOverdue = isOverdue,
                EscalatedAt = c.EscalatedAt,
                ResolutionNotes = c.ResolutionNotes,
                ResolvedByName = resolverName,
                ResolvedAt = c.ResolvedAt
            });
        }

        return result;
    }

    [Authorize(BookingHotelPermissions.Reviews.ManageComplaints)]
    public async Task<ComplaintDto> CreateComplaintAsync(CreateComplaintDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var slaDeadline = Clock.Now.AddHours(48);
        var complaint = new Complaint(
            GuidGenerator.Create(),
            input.HotelId,
            input.Title,
            input.Description,
            slaDeadline,
            input.Priority,
            ComplaintStatus.Open,
            input.GuestId,
            input.BookingId,
            CurrentTenant.Id
        );

        await _complaintRepository.InsertAsync(complaint);

        Booking? booking = null;
        if (complaint.BookingId.HasValue)
        {
            booking = await _bookingRepository.FindAsync(complaint.BookingId.Value);
        }

        return new ComplaintDto
        {
            Id = complaint.Id,
            HotelId = complaint.HotelId,
            Title = complaint.Title,
            Description = complaint.Description,
            GuestFullName = booking?.GuestFullName ?? "Customer",
            GuestPhone = booking?.GuestPhone ?? "N/A",
            BookingCode = booking?.BookingCode,
            Priority = complaint.Priority,
            Status = complaint.Status,
            CreationTime = complaint.CreationTime,
            SlaDeadline = complaint.SlaDeadline,
            HoursRemaining = 48.0,
            IsOverdue = false
        };
    }

    [Authorize(BookingHotelPermissions.Reviews.ManageComplaints)]
    public async Task<ComplaintDto> UpdateComplaintStatusAsync(Guid complaintId, UpdateComplaintStatusDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var complaint = await _complaintRepository.GetAsync(complaintId);
        if (complaint.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        complaint.Status = input.TargetStatus;
        if (!string.IsNullOrWhiteSpace(input.ResolutionNotes))
        {
            complaint.ResolutionNotes = input.ResolutionNotes;
        }

        if (input.TargetStatus == ComplaintStatus.Resolved)
        {
            complaint.ResolvedById = CurrentUser.GetId();
            complaint.ResolvedAt = Clock.Now;
        }
        else if (input.TargetStatus == ComplaintStatus.Escalated)
        {
            complaint.EscalatedAt = Clock.Now;
        }

        await _complaintRepository.UpdateAsync(complaint);

        Booking? booking = null;
        if (complaint.BookingId.HasValue)
        {
            booking = await _bookingRepository.FindAsync(complaint.BookingId.Value);
        }

        var isOverdue = Clock.Now > complaint.SlaDeadline && complaint.Status != ComplaintStatus.Resolved;
        var hoursRemaining = isOverdue ? 0 : Math.Max(0, Math.Round((complaint.SlaDeadline - Clock.Now).TotalHours, 1));

        return new ComplaintDto
        {
            Id = complaint.Id,
            HotelId = complaint.HotelId,
            Title = complaint.Title,
            Description = complaint.Description,
            GuestFullName = booking?.GuestFullName ?? "Customer",
            GuestPhone = booking?.GuestPhone ?? "N/A",
            BookingCode = booking?.BookingCode,
            Priority = complaint.Priority,
            Status = complaint.Status,
            CreationTime = complaint.CreationTime,
            SlaDeadline = complaint.SlaDeadline,
            HoursRemaining = hoursRemaining,
            IsOverdue = isOverdue,
            EscalatedAt = complaint.EscalatedAt,
            ResolutionNotes = complaint.ResolutionNotes,
            ResolvedByName = complaint.ResolvedById.HasValue ? CurrentUser.UserName : null,
            ResolvedAt = complaint.ResolvedAt
        };
    }
}
