using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace BookingHotel.Security;

[ExposeServices(typeof(ICaptchaValidator))]
public class CloudflareTurnstileValidator : ICaptchaValidator, ITransientDependency
{
    private const string TurnstileVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudflareTurnstileValidator> _logger;

    public CloudflareTurnstileValidator(
        IConfiguration configuration,
        ILogger<CloudflareTurnstileValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(string captchaToken, string? remoteIp = null)
    {
        if (string.IsNullOrWhiteSpace(captchaToken))
        {
            _logger.LogWarning("Captcha token is null or empty.");
            return false;
        }

        var bypassInDev = _configuration.GetValue<bool>("Turnstile:BypassInDevelopment", false);
        var secretKey = _configuration["Turnstile:SecretKey"];

        // Hỗ trợ mock / bypass cho môi trường Dev/Testing nếu cấu hình BypassInDevelopment
        if (bypassInDev)
        {
            if (captchaToken == "INVALID_CAPTCHA_TOKEN")
            {
                _logger.LogInformation("Turnstile rejected invalid mock token: {Token}", captchaToken);
                return false;
            }

            if (captchaToken == "MOCK_TURNSTILE_TOKEN_VALID")
            {
                _logger.LogInformation("Turnstile bypassed for development/test with token: {Token}", captchaToken);
                return true;
            }
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogError("Turnstile:SecretKey is not configured in appsettings.");
            return false;
        }

        try
        {
            var client = HttpClient;
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "secret", secretKey },
                { "response", captchaToken },
                { "remoteip", remoteIp ?? string.Empty }
            });

            var response = await client.PostAsync(TurnstileVerifyUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Turnstile API returned non-success HTTP status: {StatusCode}", response.StatusCode);
                return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var verifyResult = JsonSerializer.Deserialize<TurnstileVerifyResponse>(responseBody);

            if (verifyResult?.Success == true)
            {
                return true;
            }

            _logger.LogWarning("Turnstile verification failed. Error codes: {ErrorCodes}",
                verifyResult?.ErrorCodes != null ? string.Join(", ", verifyResult.ErrorCodes) : "Unknown");

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during Cloudflare Turnstile verification.");
            return false;
        }
    }

    private class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public List<string>? ErrorCodes { get; set; }
    }
}

