using System;
using System.Collections.Generic;

namespace NammaOoru.Entities
{
    public class Report
    {
        public int Id { get; set; }

        // Basic information
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }

        // Location
        public string? LocationJson { get; set; }
        public string? LocationAddress { get; set; }

        // Status
        public ReportStatus Status { get; set; }

        // Relationships
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public int? AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }

        // Photos attached to this report
        public ICollection<ReportPhoto> Photos { get; set; } = new List<ReportPhoto>();

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

    // Audit: who last updated this report (officer/moderator/admin)
    public int? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }

        // Voting & priority
        public int UpvoteCount { get; set; } = 0;
        public int Priority { get; set; } = 2;
    }

    public enum ReportStatus
    {
        Submitted = 1,
        Acknowledged = 2,
        InProgress = 3,
        Resolved = 4,
        Closed = 5
    }
}