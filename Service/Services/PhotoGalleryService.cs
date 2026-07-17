using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models.PhotoModels;
using Core.Models.VideoModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    public class PhotoGalleryService : IPhotoGalleryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<PhotoGalleryService> _logger;
        private readonly IEgyptTimeService _timeService;

        // ✅ UPDATED LIMITS
        private const int MaxPhotosPerGallery = 10;
        private const int MaxVideosPerGallery = 5;
        private const long MaxPhotoSize = 20 * 1024 * 1024; // 20 MB
        private const long MaxVideoSize = 250 * 1024 * 1024; // 250 MB

        // ✅ UPDATED FILE EXTENSIONS
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };
        private readonly string[] _allowedVideoExtensions = { ".mp4", ".webm", ".mov", ".mkv", ".avi" };

        public const string EVENT_PHOTOS_FOLDER = "events/photos";
        public const string VENUE_PHOTOS_FOLDER = "venues/photos";
        public const string EVENT_VIDEOS_FOLDER = "events/videos";
        public const string VENUE_VIDEOS_FOLDER = "venues/videos";

        public PhotoGalleryService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorage,
            ILogger<PhotoGalleryService> logger,
            IEgyptTimeService timeService)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _logger = logger;
            _timeService = timeService;
        }

        #region Validation

        public (bool IsValid, string ErrorMessage) ValidatePhotoUpload(IEnumerable<object> files)
        {
            var fileList = files.ToList();

            if (!fileList.Any())
                return (false, "No files provided");

            if (fileList.Count > MaxPhotosPerGallery)
                return (false, $"Maximum {MaxPhotosPerGallery} photos allowed per gallery");

            foreach (var file in fileList)
            {
                if (file is not IFormFile formFile)
                    return (false, "Invalid file format");

                var extension = Path.GetExtension(formFile.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(extension))
                    return (false, $"Only image files are allowed: {string.Join(", ", _allowedImageExtensions)}");

                if (formFile.Length > MaxPhotoSize)
                    return (false, $"Photo size exceeds maximum allowed size of 20 MB");

                if (formFile.Length == 0)
                    return (false, "Empty file detected");
            }

            return (true, string.Empty);
        }

        // ✅ NEW: Video Validation
        public (bool IsValid, string ErrorMessage) ValidateVideoUpload(IEnumerable<object> files)
        {
            var fileList = files.ToList();

            if (!fileList.Any())
                return (false, "No files provided");

            if (fileList.Count > MaxVideosPerGallery)
                return (false, $"Maximum {MaxVideosPerGallery} videos allowed per gallery");

            foreach (var file in fileList)
            {
                if (file is not IFormFile formFile)
                    return (false, "Invalid file format");

                var extension = Path.GetExtension(formFile.FileName).ToLowerInvariant();
                if (!_allowedVideoExtensions.Contains(extension))
                    return (false, $"Only video files are allowed: {string.Join(", ", _allowedVideoExtensions)}");

                if (formFile.Length > MaxVideoSize)
                    return (false, $"Video size exceeds maximum allowed size of 250 MB");

                if (formFile.Length == 0)
                    return (false, "Empty file detected");
            }

            return (true, string.Empty);
        }

        #endregion

        #region Event Photos

        public async Task<(bool Success, List<int> PhotoIds, string Message)> UploadEventPhotosAsync(
            int eventId,
            IEnumerable<object> files,
            int uploadedByUserId)
        {
            try
            {
                var fileList = files.ToList();

                var validation = ValidatePhotoUpload(fileList);
                if (!validation.IsValid)
                    return (false, new List<int>(), validation.ErrorMessage);

                var eventExists = _unitOfWork.Events.GetById(eventId);
                if (eventExists == null)
                    return (false, new List<int>(), "Event not found");

                var existingPhotos = _unitOfWork.EventPhotos.GetByEventId(eventId);
                if (existingPhotos.Count + fileList.Count > MaxPhotosPerGallery)
                    return (false, new List<int>(),
                        $"Cannot upload {fileList.Count} photos. Maximum {MaxPhotosPerGallery} photos allowed (currently have {existingPhotos.Count})");

                var uploadedPhotoIds = new List<int>();

                var uploadResult = await _fileStorage.SaveFilesAsync(fileList, EVENT_PHOTOS_FOLDER);
                if (!uploadResult.Success)
                    return (false, new List<int>(), uploadResult.ErrorMessage);

                var nextDisplayOrder = existingPhotos.Any()
                    ? existingPhotos.Max(p => p.DisplayOrder) + 1
                    : 0;

                for (int i = 0; i < uploadResult.FilePaths.Count; i++)
                {
                    var formFile = fileList[i] as IFormFile;
                    var photo = new EventPhoto
                    {
                        EventId = eventId,
                        FilePath = uploadResult.FilePaths[i],
                        OriginalFileName = formFile?.FileName,
                        DisplayOrder = nextDisplayOrder + i,
                        IsPrimary = existingPhotos.Count == 0 && i == 0,
                        UploadedByUserId = uploadedByUserId,
                        UploadedAt = DateTime.UtcNow,
                        FileSize = formFile?.Length ?? 0,
                        ContentType = formFile?.ContentType
                    };

                    _unitOfWork.EventPhotos.Add(photo);
                    _unitOfWork.Complete();
                    uploadedPhotoIds.Add(photo.EventPhotoId);
                }

                _logger.LogInformation($"Uploaded {uploadedPhotoIds.Count} photos for event {eventId}");
                return (true, uploadedPhotoIds, $"Successfully uploaded {uploadedPhotoIds.Count} photo(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading photos for event {eventId}");
                return (false, new List<int>(), $"Error uploading photos: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteEventPhotoAsync(int photoId, int userId, bool isAdmin)
        {
            try
            {
                var photo = _unitOfWork.EventPhotos.GetById(photoId);
                if (photo == null)
                    return (false, "Photo not found");

                var eventEntity = _unitOfWork.Events.GetById(photo.EventId);
                if (!isAdmin && eventEntity?.OrganizerId != userId)
                    return (false, "You don't have permission to delete this photo");

                _fileStorage.DeleteFile(photo.FilePath);

                if (photo.IsPrimary)
                {
                    var remainingPhotos = _unitOfWork.EventPhotos.GetByEventId(photo.EventId)
                        .Where(p => p.EventPhotoId != photoId)
                        .OrderBy(p => p.DisplayOrder)
                        .ToList();

                    if (remainingPhotos.Any())
                    {
                        remainingPhotos.First().IsPrimary = true;
                    }
                }

                _unitOfWork.EventPhotos.Delete(photo);
                _unitOfWork.Complete();

                _logger.LogInformation($"Deleted photo {photoId} from event {photo.EventId}");
                return (true, "Photo deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting photo {photoId}");
                return (false, $"Error deleting photo: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> SetEventPrimaryPhotoAsync(int eventId, int photoId)
        {
            try
            {
                var photo = _unitOfWork.EventPhotos.GetById(photoId);
                if (photo == null || photo.EventId != eventId)
                    return (false, "Photo not found");

                _unitOfWork.EventPhotos.SetPrimaryPhoto(eventId, photoId);

                _logger.LogInformation($"Set photo {photoId} as primary for event {eventId}");
                return (true, "Primary photo updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting primary photo for event {eventId}");
                return (false, $"Error setting primary photo: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ReorderEventPhotosAsync(int eventId, List<int> photoIds)
        {
            try
            {
                _unitOfWork.EventPhotos.ReorderPhotos(eventId, photoIds);

                _logger.LogInformation($"Reordered photos for event {eventId}");
                return (true, "Photos reordered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reordering photos for event {eventId}");
                return (false, $"Error reordering photos: {ex.Message}");
            }
        }

        public List<EventPhoto> GetEventPhotos(int eventId)
        {
            return _unitOfWork.EventPhotos.GetByEventId(eventId);
        }

        public EventPhoto? GetEventPrimaryPhoto(int eventId)
        {
            return _unitOfWork.EventPhotos.GetPrimaryPhoto(eventId);
        }

        #endregion

        #region Event Videos

        // ✅ NEW: Upload Event Videos
        public async Task<(bool Success, List<int> VideoIds, string Message)> UploadEventVideosAsync(
            int eventId,
            IEnumerable<object> files,
            int uploadedByUserId)
        {
            try
            {
                var fileList = files.ToList();

                var validation = ValidateVideoUpload(fileList);
                if (!validation.IsValid)
                    return (false, new List<int>(), validation.ErrorMessage);

                var eventExists = _unitOfWork.Events.GetById(eventId);
                if (eventExists == null)
                    return (false, new List<int>(), "Event not found");

                var existingVideos = _unitOfWork.EventVideos.GetByEventId(eventId);
                if (existingVideos.Count + fileList.Count > MaxVideosPerGallery)
                    return (false, new List<int>(),
                        $"Cannot upload {fileList.Count} videos. Maximum {MaxVideosPerGallery} videos allowed (currently have {existingVideos.Count})");

                var uploadedVideoIds = new List<int>();

                var uploadResult = await _fileStorage.SaveFilesAsync(fileList, EVENT_VIDEOS_FOLDER);
                if (!uploadResult.Success)
                    return (false, new List<int>(), uploadResult.ErrorMessage);

                var nextDisplayOrder = existingVideos.Any()
                    ? existingVideos.Max(v => v.DisplayOrder) + 1
                    : 0;

                for (int i = 0; i < uploadResult.FilePaths.Count; i++)
                {
                    var formFile = fileList[i] as IFormFile;
                    var video = new EventVideo
                    {
                        EventId = eventId,
                        FilePath = uploadResult.FilePaths[i],
                        OriginalFileName = formFile?.FileName,
                        DisplayOrder = nextDisplayOrder + i,
                        IsPrimary = existingVideos.Count == 0 && i == 0,
                        UploadedByUserId = uploadedByUserId,
                        UploadedAt = DateTime.UtcNow,
                        FileSize = formFile?.Length ?? 0,
                        ContentType = formFile?.ContentType
                    };

                    _unitOfWork.EventVideos.Add(video);
                    _unitOfWork.Complete();
                    uploadedVideoIds.Add(video.EventVideoId);
                }

                _logger.LogInformation($"Uploaded {uploadedVideoIds.Count} videos for event {eventId}");
                return (true, uploadedVideoIds, $"Successfully uploaded {uploadedVideoIds.Count} video(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading videos for event {eventId}");
                return (false, new List<int>(), $"Error uploading videos: {ex.Message}");
            }
        }

        // ✅ NEW: Delete Event Video
        public async Task<(bool Success, string Message)> DeleteEventVideoAsync(int videoId, int userId, bool isAdmin)
        {
            try
            {
                var video = _unitOfWork.EventVideos.GetById(videoId);
                if (video == null)
                    return (false, "Video not found");

                var eventEntity = _unitOfWork.Events.GetById(video.EventId);
                if (!isAdmin && eventEntity?.OrganizerId != userId)
                    return (false, "You don't have permission to delete this video");

                _fileStorage.DeleteFile(video.FilePath);

                if (video.IsPrimary)
                {
                    var remainingVideos = _unitOfWork.EventVideos.GetByEventId(video.EventId)
                        .Where(v => v.EventVideoId != videoId)
                        .OrderBy(v => v.DisplayOrder)
                        .ToList();

                    if (remainingVideos.Any())
                    {
                        remainingVideos.First().IsPrimary = true;
                    }
                }

                _unitOfWork.EventVideos.Delete(video);
                _unitOfWork.Complete();

                _logger.LogInformation($"Deleted video {videoId} from event {video.EventId}");
                return (true, "Video deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting video {videoId}");
                return (false, $"Error deleting video: {ex.Message}");
            }
        }

        public List<EventVideo> GetEventVideos(int eventId)
        {
            return _unitOfWork.EventVideos.GetByEventId(eventId);
        }

        #endregion

        #region Venue Photos

        public async Task<(bool Success, List<int> PhotoIds, string Message)> UploadVenuePhotosAsync(
            int venueId,
            IEnumerable<object> files,
            int uploadedByUserId)
        {
            try
            {
                var fileList = files.ToList();

                var validation = ValidatePhotoUpload(fileList);
                if (!validation.IsValid)
                    return (false, new List<int>(), validation.ErrorMessage);

                var venueExists = _unitOfWork.Venues.GetById(venueId);
                if (venueExists == null)
                    return (false, new List<int>(), "Venue not found");

                var existingPhotos = _unitOfWork.VenuePhotos.GetByVenueId(venueId);
                if (existingPhotos.Count + fileList.Count > MaxPhotosPerGallery)
                    return (false, new List<int>(),
                        $"Cannot upload {fileList.Count} photos. Maximum {MaxPhotosPerGallery} photos allowed (currently have {existingPhotos.Count})");

                var uploadedPhotoIds = new List<int>();

                var uploadResult = await _fileStorage.SaveFilesAsync(fileList, VENUE_PHOTOS_FOLDER);
                if (!uploadResult.Success)
                    return (false, new List<int>(), uploadResult.ErrorMessage);

                var nextDisplayOrder = existingPhotos.Any()
                    ? existingPhotos.Max(p => p.DisplayOrder) + 1
                    : 0;

                for (int i = 0; i < uploadResult.FilePaths.Count; i++)
                {
                    var formFile = fileList[i] as IFormFile;
                    var photo = new VenuePhoto
                    {
                        VenueId = venueId,
                        FilePath = uploadResult.FilePaths[i],
                        OriginalFileName = formFile?.FileName,
                        DisplayOrder = nextDisplayOrder + i,
                        IsPrimary = existingPhotos.Count == 0 && i == 0,
                        UploadedByUserId = uploadedByUserId,
                        UploadedAt = DateTime.UtcNow,
                        FileSize = formFile?.Length ?? 0,
                        ContentType = formFile?.ContentType
                    };

                    _unitOfWork.VenuePhotos.Add(photo);
                    _unitOfWork.Complete();
                    uploadedPhotoIds.Add(photo.VenuePhotoId);
                }

                _logger.LogInformation($"Uploaded {uploadedPhotoIds.Count} photos for venue {venueId}");
                return (true, uploadedPhotoIds, $"Successfully uploaded {uploadedPhotoIds.Count} photo(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading photos for venue {venueId}");
                return (false, new List<int>(), $"Error uploading photos: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteVenuePhotoAsync(int photoId, int userId, bool isAdmin)
        {
            try
            {
                var photo = _unitOfWork.VenuePhotos.GetById(photoId);
                if (photo == null)
                    return (false, "Photo not found");

                var venue = _unitOfWork.Venues.GetById(photo.VenueId);
                if (!isAdmin && venue?.CreatedByUserId != userId)
                    return (false, "You don't have permission to delete this photo");

                _fileStorage.DeleteFile(photo.FilePath);

                if (photo.IsPrimary)
                {
                    var remainingPhotos = _unitOfWork.VenuePhotos.GetByVenueId(photo.VenueId)
                        .Where(p => p.VenuePhotoId != photoId)
                        .OrderBy(p => p.DisplayOrder)
                        .ToList();

                    if (remainingPhotos.Any())
                    {
                        remainingPhotos.First().IsPrimary = true;
                    }
                }

                _unitOfWork.VenuePhotos.Delete(photo);
                _unitOfWork.Complete();

                _logger.LogInformation($"Deleted photo {photoId} from venue {photo.VenueId}");
                return (true, "Photo deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting photo {photoId}");
                return (false, $"Error deleting photo: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> SetVenuePrimaryPhotoAsync(int venueId, int photoId)
        {
            try
            {
                var photo = _unitOfWork.VenuePhotos.GetById(photoId);
                if (photo == null || photo.VenueId != venueId)
                    return (false, "Photo not found");

                _unitOfWork.VenuePhotos.SetPrimaryPhoto(venueId, photoId);

                _logger.LogInformation($"Set photo {photoId} as primary for venue {venueId}");
                return (true, "Primary photo updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting primary photo for venue {venueId}");
                return (false, $"Error setting primary photo: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ReorderVenuePhotosAsync(int venueId, List<int> photoIds)
        {
            try
            {
                _unitOfWork.VenuePhotos.ReorderPhotos(venueId, photoIds);

                _logger.LogInformation($"Reordered photos for venue {venueId}");
                return (true, "Photos reordered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reordering photos for venue {venueId}");
                return (false, $"Error reordering photos: {ex.Message}");
            }
        }

        public List<VenuePhoto> GetVenuePhotos(int venueId)
        {
            return _unitOfWork.VenuePhotos.GetByVenueId(venueId);
        }

        public VenuePhoto? GetVenuePrimaryPhoto(int venueId)
        {
            return _unitOfWork.VenuePhotos.GetPrimaryPhoto(venueId);
        }

        #endregion

        #region Venue Videos

        // ✅ NEW: Upload Venue Videos
        public async Task<(bool Success, List<int> VideoIds, string Message)> UploadVenueVideosAsync(
            int venueId,
            IEnumerable<object> files,
            int uploadedByUserId)
        {
            try
            {
                var fileList = files.ToList();

                var validation = ValidateVideoUpload(fileList);
                if (!validation.IsValid)
                    return (false, new List<int>(), validation.ErrorMessage);

                var venueExists = _unitOfWork.Venues.GetById(venueId);
                if (venueExists == null)
                    return (false, new List<int>(), "Venue not found");

                var existingVideos = _unitOfWork.VenueVideos.GetByVenueId(venueId);
                if (existingVideos.Count + fileList.Count > MaxVideosPerGallery)
                    return (false, new List<int>(),
                        $"Cannot upload {fileList.Count} videos. Maximum {MaxVideosPerGallery} videos allowed (currently have {existingVideos.Count})");

                var uploadedVideoIds = new List<int>();

                var uploadResult = await _fileStorage.SaveFilesAsync(fileList, VENUE_VIDEOS_FOLDER);
                if (!uploadResult.Success)
                    return (false, new List<int>(), uploadResult.ErrorMessage);

                var nextDisplayOrder = existingVideos.Any()
                    ? existingVideos.Max(v => v.DisplayOrder) + 1
                    : 0;

                for (int i = 0; i < uploadResult.FilePaths.Count; i++)
                {
                    var formFile = fileList[i] as IFormFile;
                    var video = new VenueVideo
                    {
                        VenueId = venueId,
                        FilePath = uploadResult.FilePaths[i],
                        OriginalFileName = formFile?.FileName,
                        DisplayOrder = nextDisplayOrder + i,
                        IsPrimary = existingVideos.Count == 0 && i == 0,
                        UploadedByUserId = uploadedByUserId,
                        UploadedAt = DateTime.UtcNow,
                        FileSize = formFile?.Length ?? 0,
                        ContentType = formFile?.ContentType
                    };

                    _unitOfWork.VenueVideos.Add(video);
                    _unitOfWork.Complete();
                    uploadedVideoIds.Add(video.VenueVideoId);
                }

                _logger.LogInformation($"Uploaded {uploadedVideoIds.Count} videos for venue {venueId}");
                return (true, uploadedVideoIds, $"Successfully uploaded {uploadedVideoIds.Count} video(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading videos for venue {venueId}");
                return (false, new List<int>(), $"Error uploading videos: {ex.Message}");
            }
        }

        // ✅ NEW: Delete Venue Video
        public async Task<(bool Success, string Message)> DeleteVenueVideoAsync(int videoId, int userId, bool isAdmin)
        {
            try
            {
                var video = _unitOfWork.VenueVideos.GetById(videoId);
                if (video == null)
                    return (false, "Video not found");

                var venue = _unitOfWork.Venues.GetById(video.VenueId);
                if (!isAdmin && venue?.CreatedByUserId != userId)
                    return (false, "You don't have permission to delete this video");

                _fileStorage.DeleteFile(video.FilePath);

                if (video.IsPrimary)
                {
                    var remainingVideos = _unitOfWork.VenueVideos.GetByVenueId(video.VenueId)
                        .Where(v => v.VenueVideoId != videoId)
                        .OrderBy(v => v.DisplayOrder)
                        .ToList();

                    if (remainingVideos.Any())
                    {
                        remainingVideos.First().IsPrimary = true;
                    }
                }

                _unitOfWork.VenueVideos.Delete(video);
                _unitOfWork.Complete();

                _logger.LogInformation($"Deleted video {videoId} from venue {video.VenueId}");
                return (true, "Video deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting video {videoId}");
                return (false, $"Error deleting video: {ex.Message}");
            }
        }

        public List<VenueVideo> GetVenueVideos(int venueId)
        {
            return _unitOfWork.VenueVideos.GetByVenueId(venueId);
        }

        #endregion
    }
}