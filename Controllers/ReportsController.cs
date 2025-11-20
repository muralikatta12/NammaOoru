

using NammaOoru.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NammaOoru.Data;
using NammaOoru.Services;
using NammaOoru.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;

namespace NammaOoru.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportsController : ControllerBase
    {
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReportsController> _logger;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _env;

        public ReportsController(
            ApplicationDbContext db,
            ILogger<ReportsController> logger,
            IEmailService emailService,
            IWebHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _emailService = emailService;
            _env = env;
        }

        [HttpPost]
        [Authorize(Roles = "Citizen,Moderator,Official,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateReportRequest req, CancellationToken ct)
        {
            // Validate basic input
            if (req == null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(req.Title) && string.IsNullOrWhiteSpace(req.Description))
                return BadRequest("Either title or description is required.");

            // Get current user id from JWT claims (the AuthService writes claim "id")
            var userIdClaim = User.FindFirst("id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Unable to determine user from token.");
            }

            // Map DTO to entity
            var report = new Report
            {
                Title = req.Title,
                Description = req.Description,
                Category = req.Category ?? "Other",
                LocationAddress = req.Source,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Submitted,
                Priority = 2
            };

            // Add photos if provided (Attachments contain URLs)
            if (req.Attachments != null && req.Attachments.Count > 0)
            {
                foreach (var a in req.Attachments)
                {
                    var photo = new ReportPhoto
                    {
                        PhotoUrl = a.Url,
                        FileName = a.Url != null ? System.IO.Path.GetFileName(a.Url) : null,
                        ContentType = a.Type ?? "image/jpeg",
                        FileSizeInBytes = 0,
                        UploadedAt = DateTime.UtcNow,
                        IsPrimary = false
                    };
                    report.Photos.Add(photo);
                }
                // mark the first photo as primary
                if (report.Photos.Count > 0)
                    report.Photos.First().IsPrimary = true;
            }

            _db.Reports.Add(report);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetReportById), new { id = report.Id }, new { report.Id });
        }

        /// <summary>
        /// Upload a photo for a report. Accepts multipart/form-data with a single file field named "file".
        /// Roles: Citizen (only for own reports), Official/Moderator/Admin (for any report)
        /// </summary>
        [HttpPost("{id}/photos")]
        [Authorize(Roles = "Citizen,Moderator,Official,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto(int id, [FromForm] NammaOoru.Models.UploadPhotoRequest req)
        {
            var file = req?.File;
            var caption = req?.Caption;
            var isPrimary = req?.IsPrimary ?? false;

            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            // Validate file size (max 5 MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
                return BadRequest($"File size exceeds the maximum allowed size of {maxFileSize} bytes.");

            // Validate content type / extension
            var permitted = new[] { "image/jpeg", "image/png" };
            if (!permitted.Contains(file.ContentType))
                return BadRequest("Only JPEG and PNG images are allowed.");

            // Get current user id and role
            var userIdClaim = User.FindFirst("id")?.Value;
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Unable to determine user from token.");
            }

            // Find report
            var report = await _db.Reports.Include(r => r.Photos).FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound($"Report with id {id} not found.");

            // If user is Citizen, ensure they own the report
            if (string.Equals(roleClaim, "Citizen", StringComparison.OrdinalIgnoreCase) && report.CreatedByUserId != userId)
            {
                return Forbid();
            }

            // Prepare upload folder
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var storedFileName = Guid.NewGuid().ToString("N") + ext;
            var fullPath = Path.Combine(uploadsDir, storedFileName);

            // Save file
            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // If marked primary, unset other photos as primary
            if (isPrimary && report.Photos != null)
            {
                foreach (var p in report.Photos)
                    p.IsPrimary = false;
            }

            var photo = new ReportPhoto
            {
                ReportId = report.Id,
                PhotoUrl = $"/uploads/{storedFileName}",
                FileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                FileSizeInBytes = file.Length,
                Caption = caption,
                UploadedAt = DateTime.UtcNow,
                IsPrimary = isPrimary || (report.Photos == null || !report.Photos.Any())
            };

            _db.ReportPhotos.Add(photo);
            await _db.SaveChangesAsync();

            var response = new ReportPhotoResponse
            {
                Id = photo.Id,
                PhotoUrl = photo.PhotoUrl,
                FileName = photo.FileName,
                ContentType = photo.ContentType,
                FileSizeInBytes = photo.FileSizeInBytes,
                Caption = photo.Caption,
                UploadedAt = photo.UploadedAt,
                IsPrimary = photo.IsPrimary
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(int id)
        {
            var report = await _db.Reports
                .Include(r => r.Photos)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            return Ok(report);
        }

        [HttpGet]
        [Authorize(Roles = "Citizen,Moderator,Official,Admin")]
        public async Task<IActionResult> GetReports(
            [FromQuery] int? status = null,
            [FromQuery] string? category = null,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 10)
        {
            if (skip < 0 || take < 1 || take > 100)
            {
                return BadRequest("Invalid pagination skip >= 0, 1 <= take <= 100");
            }

            try
            {
                // Fallback: query directly from DbContext (keeps endpoint working even if service wiring is missing)
                var query = _db.Reports
                    .Include(r => r.Photos)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.AssignedToUser)
                    .AsNoTracking()
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(r => r.Status == (ReportStatus)status.Value);

                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(r => r.Category == category);

                var total = await query.CountAsync();

                var items = await query.OrderByDescending(r => r.CreatedAt)
                                       .Skip(skip)
                                       .Take(take)
                                       .ToListAsync();

                var responses = items.Select(r => new
                {
                    id = r.Id,
                    title = r.Title,
                    description = r.Description,
                    category = r.Category,
                    locationAddress = r.LocationAddress,
                    status = (int)r.Status,
                    priority = r.Priority,
                    upvoteCount = r.UpvoteCount,
                    createdByUserId = r.CreatedByUserId,
                    createdByUserName = r.CreatedByUser != null ? $"{r.CreatedByUser.FirstName} {r.CreatedByUser.LastName}" : null,
                    createdAt = r.CreatedAt,
                    resolvedAt = r.ResolvedAt,
                    updatedAt = r.UpdatedAt,
                    photos = r.Photos.Select(p => new
                    {
                        id = p.Id,
                        photoUrl = p.PhotoUrl,
                        fileName = p.FileName,
                        contentType = p.ContentType,
                        fileSizeInBytes = p.FileSizeInBytes,
                        caption = p.Caption
                    }).ToList()
                }).ToList();

                var paged = new
                {
                    data = responses,
                    total = total,
                    page = (skip / take) + 1,
                    totalPages = (total + take - 1) / take
                };

                return Ok(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reports");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        /// <summary>
        /// Phase 2-B: Update Report Status and Notify Citizen
        /// 
        /// This endpoint allows Officers/Moderators/Admins to update a report's status.
        /// When status changes, the citizen who created the report receives an email notification.
        /// 
        /// Example statuses:
        /// 0 = Submitted (initial state)
        /// 1 = In Progress (official is working on it)
        /// 2 = Resolved (problem is fixed)
        /// 3 = Closed (final state, cannot reopen)
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Moderator,Official,Admin")]
        public async Task<IActionResult> UpdateReportStatus(
            int id,
            [FromBody] UpdateReportStatusRequest req)
        {
            // Validate the request body is not null
            if (req == null)
            {
                return BadRequest("Request body is required.");
            }

            // Get the current user's ID from the JWT token claims
            var userIdClaim = User.FindFirst("id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Unable to determine user from token.");
            }

            try
            {
                // Find the report in database with the citizen who created it
                var report = await _db.Reports
                    .Include(r => r.CreatedByUser)
                    .FirstOrDefaultAsync(r => r.Id == id);

                // If report doesn't exist, return 404
                if (report == null)
                {
                    return NotFound($"Report with id {id} not found.");
                }

                // Store old status for notification message
                var oldStatus = report.Status;

                // Update the report status
                report.Status = (ReportStatus)req.Status;
                report.UpdatedAt = DateTime.UtcNow;

                // If status is "Resolved" (2), set the ResolvedAt timestamp for audit trail
                if (req.Status == 2)
                {
                    report.ResolvedAt = DateTime.UtcNow;
                }

                // Save changes to database (include UpdatedByUserId for audit)
                report.UpdatedByUserId = userId;
                _db.Reports.Update(report);
                await _db.SaveChangesAsync();

                // Queue a notification email to the report creator about status change
                var citizenEmail = report.CreatedByUser?.Email;
                var citizenName = report.CreatedByUser?.FirstName ?? "User";

                if (!string.IsNullOrEmpty(citizenEmail))
                {
                    // Map status integer to readable text
                    var statusText = req.Status switch
                    {
                        0 => "Submitted",
                        1 => "In Progress",
                        2 => "Resolved",
                        3 => "Closed",
                        _ => "Updated"
                    };

                    var oldStatusText = oldStatus switch
                    {
                        ReportStatus.Submitted => "Submitted",
                        ReportStatus.InProgress => "In Progress",
                        ReportStatus.Resolved => "Resolved",
                        ReportStatus.Closed => "Closed",
                        _ => "Unknown"
                    };

                    var emailSubject = $"Report #{id} Status Updated";
                    var emailBody = $@"Hi {citizenName},

Your reported problem (Report #{id}: {report.Title}) status has been updated.

Previous Status: {oldStatusText}
New Status: {statusText}

We appreciate your report and our team's commitment to resolving it.

Best regards,
NammaOoru Team";

                    // Enqueue email into EmailQueue table for reliable processing (Phase 2-D will implement the worker)
                    var queueItem = new NammaOoru.Entities.EmailQueue
                    {
                        RecipientEmail = citizenEmail,
                        RecipientName = citizenName,
                        Subject = emailSubject,
                        Body = emailBody,
                        Attempts = 0,
                        NextRetry = DateTime.UtcNow,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.EmailQueue.Add(queueItem);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Enqueued status change email for report {ReportId} to {Email}", id, citizenEmail);
                }

                // Return success response
                return Ok(new
                {
                    success = true,
                    message = $"Report status updated from {oldStatus} to {report.Status}",
                    reportId = report.Id,
                    newStatus = (int)report.Status,
                    updatedAt = report.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating status for report {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the report status.",
                    error = ex.Message
                });
            }
        }
    }
}