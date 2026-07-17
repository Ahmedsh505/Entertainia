using Core.Interfaces.RepositoriesInterface;
using Core.Models.PhotoModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class VenuePhotoRepository : IVenuePhotoRepo
    {
        private readonly EntertaniaDbcontext _context;

        public VenuePhotoRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(VenuePhoto photo)
        {
            _context.VenuePhotos.Add(photo);
        }

        public void Update(VenuePhoto photo)
        {
            _context.VenuePhotos.Update(photo);
        }

        public void Delete(VenuePhoto photo)
        {
            _context.VenuePhotos.Remove(photo);
        }

        public VenuePhoto? GetById(int id)
        {
            return _context.VenuePhotos
                .Include(p => p.UploadedBy)
                .FirstOrDefault(p => p.VenuePhotoId == id);
        }

        public List<VenuePhoto> GetByVenueId(int venueId)
        {
            return _context.VenuePhotos
                .Where(p => p.VenueId == venueId)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        public VenuePhoto? GetPrimaryPhoto(int venueId)
        {
            return _context.VenuePhotos
                .FirstOrDefault(p => p.VenueId == venueId && p.IsPrimary);
        }

        public void SetPrimaryPhoto(int venueId, int photoId)
        {
            // Remove primary flag from all photos
            var photos = _context.VenuePhotos
                .Where(p => p.VenueId == venueId)
                .ToList();

            foreach (var photo in photos)
            {
                photo.IsPrimary = photo.VenuePhotoId == photoId;
            }

            _context.SaveChanges();
        }

        public void ReorderPhotos(int venueId, List<int> photoIds)
        {
            var photos = _context.VenuePhotos
                .Where(p => p.VenueId == venueId && photoIds.Contains(p.VenuePhotoId))
                .ToList();

            for (int i = 0; i < photoIds.Count; i++)
            {
                var photo = photos.FirstOrDefault(p => p.VenuePhotoId == photoIds[i]);
                if (photo != null)
                {
                    photo.DisplayOrder = i;
                }
            }

            _context.SaveChanges();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
