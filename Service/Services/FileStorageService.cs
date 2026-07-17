using Core.Interfaces.ServicesInterface;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    /// <summary>
    /// Service layer for file storage business logic
    /// Handles validation, path generation, and file metadata
    /// Actual file I/O is delegated to IFileService in UI layer
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FileStorageService> _logger;
        private readonly IEgyptTimeService _timeService;

        // File upload constraints
        private const long MaxPhotoSize = 20 * 1024 * 1024; // 20 MB for photos
        private const long MaxVideoSize = 250 * 1024 * 1024; // 250 MB for videos
        private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB for documents
        private const long MaxTotalSize = 2L * 1024 * 1024 * 1024; // 2 GB total
        private const int MaxFileCount = 25;

        private readonly string[] _allowedExtensions = new[]
        {
            // Documents
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
            // Images
            ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif",
            // Videos
            ".mp4", ".webm", ".mov", ".mkv", ".avi"
        };

        public FileStorageService(
            IFileService fileService,
            ILogger<FileStorageService> logger,
            IEgyptTimeService timeService)
        {
            _fileService = fileService;
            _logger = logger;
            _timeService = timeService;
        }

        #region Validation

        public bool ValidateFile(object file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_fileService.IsValidFile(file, _allowedExtensions, MaxVideoSize))
            {
                errorMessage = "Invalid file type or size exceeds limit (250 MB for videos, 20 MB for images, 100 MB for documents)";
                return false;
            }

            return true;
        }

        public bool ValidateFiles(IEnumerable<object> files, out string errorMessage)
        {
            errorMessage = string.Empty;

            var fileList = files.ToList();

            if (fileList.Count > MaxFileCount)
            {
                errorMessage = $"Maximum {MaxFileCount} files allowed";
                return false;
            }

            foreach (var file in fileList)
            {
                if (!ValidateFile(file, out var fileError))
                {
                    errorMessage = fileError;
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region File Operations

        public async Task<(bool Success, string? FilePath, string ErrorMessage)> SaveFileAsync(
            object file,
            string folderName)
        {
            try
            {
                if (!ValidateFile(file, out var validationError))
                {
                    return (false, null, validationError);
                }

                var filePath = await _fileService.SaveFileAsync(file, folderName);

                _logger.LogInformation($"File saved successfully to: {filePath}");
                return (true, filePath, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file");
                return (false, null, $"Error saving file: {ex.Message}");
            }
        }

        public async Task<(bool Success, List<string> FilePaths, string ErrorMessage)> SaveFilesAsync(
            IEnumerable<object> files,
            string folderName)
        {
            try
            {
                var fileList = files.ToList();

                if (!ValidateFiles(fileList, out var validationError))
                {
                    return (false, new List<string>(), validationError);
                }

                var filePaths = await _fileService.SaveFilesAsync(fileList, folderName);

                _logger.LogInformation($"Saved {filePaths.Count} files successfully");
                return (true, filePaths, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving files");
                return (false, new List<string>(), $"Error saving files: {ex.Message}");
            }
        }

        public bool DeleteFile(string relativePath)
        {
            try
            {
                if (string.IsNullOrEmpty(relativePath))
                {
                    _logger.LogWarning("Attempted to delete file with empty path");
                    return false;
                }

                var result = _fileService.DeleteFile(relativePath);

                if (result)
                {
                    _logger.LogInformation($"File deleted successfully: {relativePath}");
                }
                else
                {
                    _logger.LogWarning($"File not found or could not be deleted: {relativePath}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {relativePath}");
                return false;
            }
        }

        public bool DeleteFiles(IEnumerable<string> relativePaths)
        {
            try
            {
                var allSuccess = true;

                foreach (var path in relativePaths)
                {
                    if (!DeleteFile(path))
                    {
                        allSuccess = false;
                    }
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting multiple files");
                return false;
            }
        }

        #endregion

        #region File Metadata

        public string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                // Documents
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",

                // Images
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",

                // Videos
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".mkv" => "video/x-matroska",
                ".avi" => "video/x-msvideo",

                _ => "application/octet-stream"
            };
        }

        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.#} {sizes[order]}";
        }

        #endregion

        #region Constants Access

        public long GetMaxFileSize() => MaxVideoSize; // Return max (video size)
        public long GetMaxPhotoSize() => MaxPhotoSize;
        public long GetMaxVideoSize() => MaxVideoSize;
        public long GetMaxDocumentSize() => MaxFileSize;
        public long GetMaxTotalSize() => MaxTotalSize;
        public int GetMaxFileCount() => MaxFileCount;
        public string[] GetAllowedExtensions() => _allowedExtensions;

        #endregion
    }
}