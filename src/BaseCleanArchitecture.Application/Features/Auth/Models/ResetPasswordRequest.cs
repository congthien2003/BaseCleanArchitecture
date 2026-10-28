using System.ComponentModel.DataAnnotations;

namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits.")] string Otp,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword
);
