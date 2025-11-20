namespace NammaOoru.Models
{
    /// <summary>
    /// Request model for updating a report status.
    /// 
    /// Status values are:
    /// 0 = Submitted (just created)
    /// 1 = InProgress (officer is working on it)
    /// 2 = Resolved (officer fixed the issue)
    /// 3 = Closed (final, archived)
    /// </summary>
    public class UpdateReportStatusRequest
    {
        /// <summary>
        /// New status value (0-3)
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Optional comments about the status update (e.g., "Pothole filled" when marking as Resolved)
        /// </summary>
        public string? Comments { get; set; }
    }
}