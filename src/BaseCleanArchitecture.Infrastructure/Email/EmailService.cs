using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using Microsoft.Extensions.Options;
using Resend;

namespace BaseCleanArchitecture.Infrastructure.Email
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly EmailOptions emailOptions;

        public EmailService(IResend resend, IOptions<EmailOptions> option)
        {
            _resend = resend;
            emailOptions = option.Value;
        }

        public async Task SendMail(string to, string subject, string body)
        {
            var message = new EmailMessage();
            message.From = emailOptions.DisplayName;
            message.To.Add(to);
            message.Subject = subject;
            message.HtmlBody = body;
            await _resend.EmailSendAsync(message);
        }
    }
}
