using Microsoft.AspNetCore.Mvc;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Low-level file I/O service (UI layer implementation)
    /// Uses object type to avoid tight coupling to IFormFile
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Save a file to the specified folder
        /// </summary>
        /// <param name="file">File object (must be IFormFile in implementation)</param>
        /// <param name="folderName">Target folder name</param>
        /// <returns>Relative file path</returns>
        Task<string> SaveFileAsync(object file, string folderName);

        /// <summary>
        /// Save multiple files to the specified folder
        /// </summary>
        Task<List<string>> SaveFilesAsync(IEnumerable<object> files, string folderName);

        /// <summary>
        /// Delete a file by relative path
        /// </summary>
        bool DeleteFile(string relativePath);

        /// <summary>
        /// Validate a file against allowed extensions and size limits
        /// </summary>
        bool IsValidFile(object file, string[] allowedExtensions, long maxFileSize);

        /// <summary>
        /// Get a file for download/viewing
        /// </summary>
        PhysicalFileResult GetFile(string relativePath);
    }
}