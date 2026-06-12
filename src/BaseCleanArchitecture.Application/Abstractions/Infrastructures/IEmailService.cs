using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Application.Abstractions.Infrastructures
{
    public interface IEmailService
    {
        public Task SendMail(string to, string subject, string body);
    }
}
