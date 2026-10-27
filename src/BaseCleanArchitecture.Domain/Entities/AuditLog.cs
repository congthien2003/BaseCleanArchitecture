using BaseCleanArchitecture.Domain.Abtractions;
using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Domain.Entities
{
    public class AuditLog : EntityBase<Guid>
    {
        public AuditType AuditType { get; set; }
        public int StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public long DurationMs { get; set; }

        /// <summary>Encrypted (see IAuditEncryptor).</summary>
        public string? RequestBody { get; set; }

        /// <summary>Encrypted (see IAuditEncryptor).</summary>
        public string? ResponseBody { get; set; }

        /// <summary>Encrypted JSON list of entity changes made by the request.</summary>
        public string? Changes { get; set; }

        public bool IsRequestTruncated { get; set; }
        public bool IsResponseTruncated { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public AuditLog() : base(Guid.CreateVersion7())
        {
        }
    }
}
