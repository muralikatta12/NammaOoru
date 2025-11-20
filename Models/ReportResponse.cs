using System;
using System.Collections.Generic;
using NammaOoru.Entities;

namespace NammaOoru.Models
{
    public class ReportResponse 
    { 
        public int Id { get; set; }
        public string? Title { get; set; }
        
        public string? Description { get; set; }

        public string? Category { get; set; }

        public string? LocationAddress { get; set; }

        public int Status { get; set; }

        public int Priority { get; set; }

        public int UpvoteCount { get; set; }

        public int CreatedByUserId { get; set; }

        public string? CreatedByUserName { get; set; }

        public int? AssignedToUserId { get; set; }  

        public string? AssignedToUserName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<ReportPhotoResponse> Photos { get; set; } = new();
    }

    public class ReportPhotoResponse
    {
        public int Id { get; set; }

        public string? PhotoUrl { get; set; }

        public string? FileName { get; set; }

        public string? ContentType { get; set; }

        public long FileSizeInBytes { get; set; }

        public string? Caption { get; set; }

        public DateTime UploadedAt { get; set; }

        public bool IsPrimary { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();

        public int Total { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }
    }
}