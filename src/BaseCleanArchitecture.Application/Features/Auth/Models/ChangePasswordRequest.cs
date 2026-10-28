using System.ComponentModel.DataAnnotations;

namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword
);
