using System;

namespace NammaOoru.Entities
{
    public class EmailQueue
    {
        public int Id { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int Attempts { get; set; } = 0;
        public DateTime NextRetry { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Sent, Failed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
    }
}
