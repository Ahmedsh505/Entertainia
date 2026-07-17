using Core.Models.PhotoModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IVenuePhotoRepo
    {
        void Add(VenuePhoto photo);
        void Update(VenuePhoto photo);
        void Delete(VenuePhoto photo);
        VenuePhoto? GetById(int id);
        List<VenuePhoto> GetByVenueId(int venueId);
        VenuePhoto? GetPrimaryPhoto(int venueId);
        void SetPrimaryPhoto(int venueId, int photoId);
        void ReorderPhotos(int venueId, List<int> photoIds);
        void Save();
    }
}