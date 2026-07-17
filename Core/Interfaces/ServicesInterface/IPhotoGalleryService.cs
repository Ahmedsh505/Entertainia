using Core.Models.PhotoModels;
using Core.Models.VideoModels;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service interface for photo and video gallery management
    /// </summary>
    public interface IPhotoGalleryService
    {
        // ========================================
        // EVENT PHOTOS
        // ========================================
        Task<(bool Success, List<int> PhotoIds, string Message)> UploadEventPhotosAsync(
            int eventId,
            IEnumerable<object> files,
            int uploadedByUserId);

        Task<(bool Success, string Message)> DeleteEventPhotoAsync(int photoId, int userId, bool isAdmin);

        Task<(bool Success, string Message)> SetEventPrimaryPhotoAsync(int eventId, int photoId);

        Task<(bool Success, string Message)> ReorderEventPhotosAsync(int eventId, List<int> photoIds);

        List<EventPhoto> GetEventPhotos(int eventId);

        EventPhoto? GetEventPrimaryPhoto(int eventId);

        // ========================================
        // EVENT VIDEOS
        // ========================================
        Task<(bool Success, List<int> VideoIds, string Message)> UploadEventVideosAsync(
            int eventId,
            IEnumerable<object> files,
            int uploadedByUserId);

        Task<(bool Success, string Message)> DeleteEventVideoAsync(int videoId, int userId, bool isAdmin);

        List<EventVideo> GetEventVideos(int eventId);

        // ========================================
        // VENUE PHOTOS
        // ========================================
        Task<(bool Success, List<int> PhotoIds, string Message)> UploadVenuePhotosAsync(
            int venueId,
            IEnumerable<object> files,
            int uploadedByUserId);

        Task<(bool Success, string Message)> DeleteVenuePhotoAsync(int photoId, int userId, bool isAdmin);

        Task<(bool Success, string Message)> SetVenuePrimaryPhotoAsync(int venueId, int photoId);

        Task<(bool Success, string Message)> ReorderVenuePhotosAsync(int venueId, List<int> photoIds);

        List<VenuePhoto> GetVenuePhotos(int venueId);

        VenuePhoto? GetVenuePrimaryPhoto(int venueId);

        // ========================================
        // VENUE VIDEOS
        // ========================================
        Task<(bool Success, List<int> VideoIds, string Message)> UploadVenueVideosAsync(
            int venueId,
            IEnumerable<object> files,
            int uploadedByUserId);

        Task<(bool Success, string Message)> DeleteVenueVideoAsync(int videoId, int userId, bool isAdmin);

        List<VenueVideo> GetVenueVideos(int venueId);

        // ========================================
        // VALIDATION
        // ========================================
        (bool IsValid, string ErrorMessage) ValidatePhotoUpload(IEnumerable<object> files);
        (bool IsValid, string ErrorMessage) ValidateVideoUpload(IEnumerable<object> files);
    }
}