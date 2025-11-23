using System;
namespace NammaOoru.Entities
{
    public class ReportPhoto
    {
        public int Id { get; set; }

        public int ReportId { get; set; }          // FK stored in DB

    public Report? Report { get; set; }         // Navigation property (nullable to satisfy nullable reference checks)


    public string? PhotoUrl { get; set; }

    public string? FileName { get; set; }


    public long FileSizeInBytes { get; set; }

    // MIME type, e.g. "image/jpeg"
    public string? ContentType { get; set; }


    public string? Caption { get; set; }


    public DateTime UploadedAt { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsPrimary { get; set; } = false;

    }
}