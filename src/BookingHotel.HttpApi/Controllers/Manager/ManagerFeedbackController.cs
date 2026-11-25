using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Complaints;
using BookingHotel.Controllers;
using BookingHotel.Manager.Feedback;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.HttpApi.Controllers.Manager;

[RemoteService]
[Route("api/app/manager/feedback")]
public class ManagerFeedbackController : BookingHotelController, IManagerFeedbackAppService
{
    private readonly IManagerFeedbackAppService _feedbackAppService;

    public ManagerFeedbackController(IManagerFeedbackAppService feedbackAppService)
    {
        _feedbackAppService = feedbackAppService;
    }

    [HttpGet("reviews")]
    public Task<List<ReviewDto>> GetReviewsAsync([FromQuery] Guid hotelId, [FromQuery] int? minScore = null)
    {
        return _feedbackAppService.GetReviewsAsync(hotelId, minScore);
    }

    [HttpPost("reviews/{reviewId}/reply")]
    public Task<ReviewDto> ReplyToReviewAsync(Guid reviewId, [FromBody] ReplyReviewDto input)
    {
        return _feedbackAppService.ReplyToReviewAsync(reviewId, input);
    }

    [HttpGet("complaints")]
    public Task<List<ComplaintDto>> GetComplaintsAsync([FromQuery] Guid hotelId, [FromQuery] ComplaintStatus? status = null)
    {
        return _feedbackAppService.GetComplaintsAsync(hotelId, status);
    }

    [HttpPost("complaints")]
    public Task<ComplaintDto> CreateComplaintAsync([FromBody] CreateComplaintDto input)
    {
        return _feedbackAppService.CreateComplaintAsync(input);
    }

    [HttpPut("complaints/{complaintId}/status")]
    public Task<ComplaintDto> UpdateComplaintStatusAsync(Guid complaintId, [FromBody] UpdateComplaintStatusDto input)
    {
        return _feedbackAppService.UpdateComplaintStatusAsync(complaintId, input);
    }
}
