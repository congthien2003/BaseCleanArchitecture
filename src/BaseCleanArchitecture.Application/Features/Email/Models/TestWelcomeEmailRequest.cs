using System.ComponentModel.DataAnnotations;

namespace BaseCleanArchitecture.Application.Features.Email.Models;

public sealed record TestWelcomeEmailRequest(string To);
