using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Application.Common.Constants
{
    public class PartnerIntegrate
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PartnerCode { get; set; } = string.Empty;

        public string PartnerName { get; set; } = string.Empty;

        public string PartnerType { get; set; } = string.Empty;
    }

    public static class PartnerType
    {
        public static string PaymentGateway = "PAYMENT_GATEWAY";
        public static string EmailSending = "EMAIL_SENDING";
    }

    public static class PartnerIntegrationData
    {
        public static PartnerIntegrate Payos = new PartnerIntegrate
        {
            PartnerId = "001",
            PartnerCode = "P001",
            PartnerName = "PayOS",
            PartnerType = PartnerType.PaymentGateway
        };
    }
}
