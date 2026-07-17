using Core.Interfaces.ServicesInterface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Entertania1.Services
{
    /// <summary>
    /// Low-level file I/O service (UI layer implementation)
    /// Handles web-specific file operations with IFormFile
    /// Business logic is in Service.Services.FileStorageService
    /// </summary>
    public class FileService : IFileService
    {
        private readonly string _basePath;
        private readonly ILogger<FileService> _logger;

        public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
        {
            _basePath = Path.Combine(env.WebRootPath, "uploads");
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(object file, string folderName)
        {
            if (file is not IFormFile formFile || formFile.Length == 0)
                throw new ArgumentException("Invalid file");

            var folderPath = Path.Combine(_basePath, folderName);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var uniqueFileName = $"{Guid.NewGuid()}_{formFile.FileName}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await formFile.CopyToAsync(stream);
                }

                _logger.LogInformation($"✅ File uploaded: {uniqueFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error uploading file {formFile.FileName}: {ex.Message}");
                throw;
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public async Task<List<string>> SaveFilesAsync(IEnumerable<object> files, string folderName)
        {
            var paths = new List<string>();
            foreach (var file in files)
            {
                paths.Add(await SaveFileAsync(file, folderName));
            }
            return paths;
        }

        public bool DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;

            var fullPath = Path.Combine(_basePath, relativePath.TrimStart('/').Replace("uploads/", ""));
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"🗑️ File deleted: {relativePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error deleting file {relativePath}: {ex.Message}");
            }
            return false;
        }

        public bool IsValidFile(object file, string[] allowedExtensions, long maxFileSize)
        {
            if (file is not IFormFile formFile || formFile.Length == 0)
                return false;

            var ext = Path.GetExtension(formFile.FileName).ToLowerInvariant();
            return formFile.Length <= maxFileSize && allowedExtensions.Contains(ext);
        }

        public PhysicalFileResult GetFile(string relativePath)
        {
            var fullPath = Path.Combine(_basePath, relativePath.TrimStart('/').Replace("uploads/", ""));
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found", fullPath);

            var mime = "application/octet-stream";
            return new PhysicalFileResult(fullPath, mime)
            {
                FileDownloadName = Path.GetFileName(fullPath)
            };
        }
    }
}