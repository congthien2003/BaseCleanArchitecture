using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Email.Commands;

public sealed record SendTestWelcomeEmailCommand(string To) : IRequest;

public sealed class SendTestWelcomeEmailCommandHandler : IRequestHandler<SendTestWelcomeEmailCommand>
{
    private const string Subject = "Welcome to BaseCleanArchitecture";
    private const string Body = "<h1>Welcome to BaseCleanArchitecture</h1>";

    private readonly IEmailService _emailService;

    public SendTestWelcomeEmailCommandHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(SendTestWelcomeEmailCommand request, CancellationToken cancellationToken)
    {
        await _emailService.SendMail(request.To, Subject, Body);
    }
}
