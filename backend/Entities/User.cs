using System;
using System.Collections.Generic;

namespace NammaOoru.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // password 
        
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string Role { get; set; } = "Citizen"; // Roles: Citizen, Official, Moderator, Admin

    // Navigation property: OTPs created for this user
    public ICollection<OtpVerification> OtpVerifications { get; set; } = new List<OtpVerification>();

    // Navigation property: Reports created by this user (citizen reports)
    public ICollection<Report> Reports { get; set; } = new List<Report>();

    // Navigation property: Reports assigned to this user (if the user is an officer)
    public ICollection<Report> AssignedReports { get; set; } = new List<Report>();
    }
}
