using BaseCleanArchitecture.Infrastructure.Messaging.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Infrastructure.Messaging
{
    public class MessagingOptions
    {
        public const string SectionName = "Messaging";

        public string Provider { get; set; } = "RabbitMQ";

        public RabbitMQOptions RabbitMQOptions { get; set; }

        public bool UseRabbitMQ => Provider.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase);
    }
}
