using System.ComponentModel.DataAnnotations;

namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
