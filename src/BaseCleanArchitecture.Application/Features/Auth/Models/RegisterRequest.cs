using System.ComponentModel.DataAnnotations;
using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record RegisterRequest(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, EmailAddress, StringLength(100)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    [Required, StringLength(100)] string FullName,
    [Phone, StringLength(20)] string? PhoneNumber = null,
    DateOnly? DateOfBirth = null,
    Gender Gender = Gender.Unknown,
    [StringLength(300)] string? Address = null,
    RoleType Role = RoleType.Customer
);
