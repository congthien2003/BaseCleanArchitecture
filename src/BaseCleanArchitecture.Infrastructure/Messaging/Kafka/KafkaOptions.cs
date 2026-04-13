using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Infrastructure.Messaging.Kafka
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string Topic { get; set; } = "default-topic";
    }
}
