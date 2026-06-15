using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Infrastructure.Email
{
    public class EmailOptions
    {
        public string DisplayName { get; set; } = "Base Clean Architecture";

        public string EmailAddress { get; set; } = "sabo@gmail.com";

        public string Password { get; set; } = "password";

        public EmailOptions() { }
    }
}
