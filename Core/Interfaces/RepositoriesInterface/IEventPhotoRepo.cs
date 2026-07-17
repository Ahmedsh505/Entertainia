using Core.Models.PhotoModels;

namespace Core.Interfaces.RepositoriesInterface
{
    /// <summary>
    /// Repository interface for EventPhoto operations
    /// </summary>
    public interface IEventPhotoRepo
    {
        void Add(EventPhoto photo);
        void Update(EventPhoto photo);
        void Delete(EventPhoto photo);
        EventPhoto? GetById(int id);
        List<EventPhoto> GetByEventId(int eventId);
        EventPhoto? GetPrimaryPhoto(int eventId);
        void SetPrimaryPhoto(int eventId, int photoId);
        void ReorderPhotos(int eventId, List<int> photoIds);
        void Save();
    }
}