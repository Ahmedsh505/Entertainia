using Core.Interfaces.RepositoriesInterface;
using Core.Models.PhotoModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class EventPhotoRepository : IEventPhotoRepo
    {
        private readonly EntertaniaDbcontext _context;

        public EventPhotoRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(EventPhoto photo)
        {
            _context.EventPhotos.Add(photo);
        }

        public void Update(EventPhoto photo)
        {
            _context.EventPhotos.Update(photo);
        }

        public void Delete(EventPhoto photo)
        {
            _context.EventPhotos.Remove(photo);
        }

        public EventPhoto? GetById(int id)
        {
            return _context.EventPhotos
                .Include(p => p.UploadedBy)
                .FirstOrDefault(p => p.EventPhotoId == id);
        }

        public List<EventPhoto> GetByEventId(int eventId)
        {
            return _context.EventPhotos
                .Where(p => p.EventId == eventId)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

        public EventPhoto? GetPrimaryPhoto(int eventId)
        {
            return _context.EventPhotos
                .FirstOrDefault(p => p.EventId == eventId && p.IsPrimary);
        }

        public void SetPrimaryPhoto(int eventId, int photoId)
        {
            // Remove primary flag from all photos
            var photos = _context.EventPhotos
                .Where(p => p.EventId == eventId)
                .ToList();

            foreach (var photo in photos)
            {
                photo.IsPrimary = photo.EventPhotoId == photoId;
            }

            _context.SaveChanges();
        }

        public void ReorderPhotos(int eventId, List<int> photoIds)
        {
            var photos = _context.EventPhotos
                .Where(p => p.EventId == eventId && photoIds.Contains(p.EventPhotoId))
                .ToList();

            for (int i = 0; i < photoIds.Count; i++)
            {
                var photo = photos.FirstOrDefault(p => p.EventPhotoId == photoIds[i]);
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
