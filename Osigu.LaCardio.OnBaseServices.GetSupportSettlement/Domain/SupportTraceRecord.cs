using System;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain
{
    public class SupportTraceRecord
    {
        public int Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? SupportType { get; set; }
        public int AttemptCount { get; set; }
        public DateTime LastAttemptTime { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? FilePath { get; set; }
        public string? ProcessId { get; set; }
        public string? UniqueVerificationCode { get; set; }
        public string? SourceCode { get; set; }
    }
}
