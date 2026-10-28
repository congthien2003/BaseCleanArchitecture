using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record RegisterDto(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null,
    DateOnly? DateOfBirth = null,
    Gender Gender = Gender.Unknown,
    string? Address = null,
    RoleType Role = RoleType.Customer
);
