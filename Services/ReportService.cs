
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using NammaOoru.Data;
using NammaOoru.Entities;
using NammaOoru.Models;

namespace NammaOoru.Services
{
    public interface IReportService
    {
        // Get paginated list of reports with optional filters
        Task<PagedResponse<ReportResponse>> GetReportsAsync(
            int? status = null,
            string? category = null,
            int skip = 0,
            int take = 10);
        
        // Get report by ID
        Task<ReportResponse?> GetReportByIdAsync(int id);

        // Update report status and return success/failure tuple
        Task<(bool success, string message)> UpdateReportStatusAsync(
            int reportId,
            int newStatus,
            int? updatedByUserId);

        // Map Report entity to ReportResponse model
        ReportResponse MapToResponse(Report report);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResponse<ReportResponse>> GetReportsAsync(
            int? status = null,
            string? category = null,
            int skip = 0,
            int take = 10)
        {

            IQueryable<Report> query = _db.Reports
                .Include(r => r.CreatedByUser)
                .Include(r => r.AssignedToUser)
                .Include(r => r.Photos)
                .AsNoTracking();

            // Apply filters
            if (status.HasValue)
            {
                query = query.Where(r => r.Status == (ReportStatus)status.Value);
            }
             //this helps to filter category
            if(!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(r => r.Category == category);
            }
            // Get total count before pagination
            int total =  await query.CountAsync();

            var reports  = await query
                .OrderByDescending(r=>r.CreatedAt)
                .Skip(skip)
                .Take(take) 
                .ToListAsync();

            var reportResponses = reports.Select(r=> MapToResponse(r)).ToList();

            int totalPages = (total+ take -1)/take;

            int page = (skip / take) + 1;

            return new PagedResponse<ReportResponse>
            {
                Data = reportResponses,
                Total = total,
                Page = page,
                TotalPages = totalPages
            };
        }

        public async Task<ReportResponse?> GetReportByIdAsync(int id)
        {
            var report = await _db.Reports
                .Include(r=>r.CreatedByUser)
                .Include(r=>r.AssignedToUser)
                .Include(r=>r.Photos)
                .FirstOrDefaultAsync(r=>r.Id == id);

            if(report == null)
                return null;
            
            return MapToResponse(report);
        }

        public async Task<(bool success, string message)> UpdateReportStatusAsync(
            int reportId,
            int newStatus,
            int? updatedByUserId)
        {
            try
            {
                //Find the report in database
                var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

                //if report doesnt exist return error
                if(report == null)
                {
                    return (false, "Report not found");
                }

                if(newStatus < 0 || newStatus > 3)
                {
                    return (false, "Invalid status value");
                }
                 
                //record who changed it and when(audit trail)
                //For simplicity, we just update the status and timestamp here
                report.Status = (ReportStatus)newStatus;
                report.UpdatedAt = DateTime.UtcNow;

                if(newStatus == 2)
                {
                    report.ResolvedAt = DateTime.UtcNow;
                }

                //save the updated report to the database
                _db.Reports.Update(report);
                await _db.SaveChangesAsync();

                return (true, $"Report status updated to {(ReportStatus)newStatus}");
            }
            catch(Exception)
            {
                //log the exception (not implemented here for brevity)
                return (false, "An error occurred while updating the report status");
            }
        }

        public ReportResponse MapToResponse(Report report)
        {
            return new ReportResponse
            {
                Id =  report.Id,
                Title =  report.Title,
                Description =  report.Description,
                Category =  report.Category,
                LocationAddress =  report.LocationAddress,
                Status =  (int)report.Status,
                Priority =  report.Priority,
                UpvoteCount =  report.UpvoteCount,
                CreatedByUserId =  report.CreatedByUserId,
                CreatedByUserName = report.CreatedByUser != null
                ? $"{report.CreatedByUser.FirstName} {report.CreatedByUser.LastName}": null,

                CreatedAt = report.CreatedAt,
                ResolvedAt = report.ResolvedAt,
                UpdatedAt = report.UpdatedAt,

                //Mapping Photos to DTOs
                Photos =  report.Photos.Select(p=> new ReportPhotoResponse
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    FileName = p.FileName,
                    ContentType = p.ContentType,
                    FileSizeInBytes = p.FileSizeInBytes,
                    Caption = p.Caption
                }).ToList()
            };
        }
    }
}