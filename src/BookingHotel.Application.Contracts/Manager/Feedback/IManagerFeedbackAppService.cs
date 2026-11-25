using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Complaints;
using Volo.Abp.Application.Services;

namespace BookingHotel.Manager.Feedback;

public interface IManagerFeedbackAppService : IApplicationService
{
    Task<List<ReviewDto>> GetReviewsAsync(Guid hotelId, int? minScore = null);
    Task<ReviewDto> ReplyToReviewAsync(Guid reviewId, ReplyReviewDto input);

    Task<List<ComplaintDto>> GetComplaintsAsync(Guid hotelId, ComplaintStatus? status = null);
    Task<ComplaintDto> CreateComplaintAsync(CreateComplaintDto input);
    Task<ComplaintDto> UpdateComplaintStatusAsync(Guid complaintId, UpdateComplaintStatusDto input);
}
