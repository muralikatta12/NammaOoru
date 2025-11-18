using System.Collections.Generic;
using System;

namespace NammaOoru.Models
{
    public class CreateReportRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }

        // Category name (e.g., "Pothole", "StreetLight")
        public string? Category { get; set; }

        // Location: latitude/longitude
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string? Source { get; set; }

        // Optional attachments uploaded previously (URLs)
        public List<CreateMediaRequest>? Attachments { get; set; }
    }

    public class CreateMediaRequest
    {
        public string? Url { get; set; }
        public string? Type { get; set; } = "image/jpeg";
        public string? CheckSum { get; set; }
    }
}
