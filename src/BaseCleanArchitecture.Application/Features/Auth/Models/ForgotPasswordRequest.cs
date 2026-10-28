using System.ComponentModel.DataAnnotations;

namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);
