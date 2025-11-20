using Microsoft.AspNetCore.Http;

namespace NammaOoru.Models
{
    // Bound from multipart/form-data
    public class UploadPhotoRequest
    {
        public IFormFile? File { get; set; }
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
    }
}
