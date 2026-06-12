using BaseCleanArchitecture.Application.Abstractions.Infrastructures;

namespace BaseCleanArchitecture.Infrastructure.Email
{
    public class EmailService : IEmailService
    {
        public Task SendMail(string to, string subject, string body)
        {
            throw new NotImplementedException();
        }
    }
}
