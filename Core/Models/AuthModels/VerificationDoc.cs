using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.AuthModels
{
    public class VerificationDoc
    {
        public int VerificationDocId { get; set; }
        public int UserId { get; set; }            // FK -> ApplicationUser.Id
        public User? User { get; set; } // Navigation

        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!; // local path (wwwroot/...)
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Optional: a short description or mime type
        public string? ContentType { get; set; }
    }
}
