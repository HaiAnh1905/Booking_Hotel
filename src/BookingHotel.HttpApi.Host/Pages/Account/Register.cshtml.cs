using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BookingHotel.Pages.Account;

public class RegisterModel : AbpPageModel
{
    private readonly IConfiguration _configuration;

    public RegisterModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var redirectUrl = $"{angularUrl}/auth/register";

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            redirectUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        }

        return Redirect(redirectUrl);
    }
}

