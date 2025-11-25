namespace NammaOoru.Entities
{
    public class OtpVerification
    {
        public int Id { get; set; }
        public int? UserId { get; set; }  // Make nullable - user may not exist yet during OTP flow
        public string OtpCode { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }// indicates if the OTP has been used
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }  

        // Navigation property
        public User? User { get; set; }
    }
}
