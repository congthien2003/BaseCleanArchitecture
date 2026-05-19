using BaseCleanArchitecture.Application.Interfaces;

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
