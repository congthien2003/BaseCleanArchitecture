using System.ComponentModel.DataAnnotations;

namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record RefreshTokenRequest(
    [Required] string RefreshToken
);
