using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service layer interface for file storage business logic
    /// </summary>
    public interface IFileStorageService
    {
        // Validation
        bool ValidateFile(object file, out string errorMessage);
        bool ValidateFiles(IEnumerable<object> files, out string errorMessage);

        // File Operations
        Task<(bool Success, string? FilePath, string ErrorMessage)> SaveFileAsync(object file, string folderName);
        Task<(bool Success, List<string> FilePaths, string ErrorMessage)> SaveFilesAsync(IEnumerable<object> files, string folderName);
        bool DeleteFile(string relativePath);
        bool DeleteFiles(IEnumerable<string> relativePaths);

        // File Metadata
        string GetContentType(string fileName);
        string FormatFileSize(long bytes);

        // Constants Access
        long GetMaxFileSize();
        long GetMaxTotalSize();
        int GetMaxFileCount();
        string[] GetAllowedExtensions();
    }
}
