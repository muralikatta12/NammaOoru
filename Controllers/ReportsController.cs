

using NammaOoru.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NammaOoru.Data;
// Ensure this using directive matches the namespace where IUserContext is defined
using NammaOoru.Services; // keep for future IUserContext if added
using NammaOoru.Entities;
using Microsoft.EntityFrameworkCore;

[ApiController ]
[Route("reports")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReportsController(ApplicationDbContext db)
    {
        _db = db;
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
                    ContentType = a.Type,
                    FileSizeInBytes = 0,
                    UploadedAt = DateTime.UtcNow,
                    IsPrimary = false
                };
                report.Photos.Add(photo);
            }
            // mark the first photo as primary
            report.Photos.First().IsPrimary = true;
        }

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetReportById), new { id = report.Id }, new { report.Id });
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
}