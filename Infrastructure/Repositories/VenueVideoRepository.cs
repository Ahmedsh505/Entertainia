using Core.Interfaces.RepositoriesInterface;
using Core.Models.VideoModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Repositories
{
    public class VenueVideoRepository : IVenueVideoRepo
    {
        private readonly EntertaniaDbcontext _context;

        public VenueVideoRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(VenueVideo video)
        {
            _context.VenueVideos.Add(video);
        }

        public void Update(VenueVideo video)
        {
            _context.VenueVideos.Update(video);
        }

        public void Delete(VenueVideo video)
        {
            _context.VenueVideos.Remove(video);
        }

        public VenueVideo? GetById(int id)
        {
            return _context.VenueVideos
                .Include(v => v.UploadedBy)
                .FirstOrDefault(v => v.VenueVideoId == id);
        }

        public List<VenueVideo> GetByVenueId(int venueId)
        {
            return _context.VenueVideos
                .Where(v => v.VenueId == venueId)
                .OrderBy(v => v.DisplayOrder)
                .ToList();
        }

        public VenueVideo? GetPrimaryVideo(int venueId)
        {
            return _context.VenueVideos
                .FirstOrDefault(v => v.VenueId == venueId && v.IsPrimary);
        }

        public void SetPrimaryVideo(int venueId, int videoId)
        {
            var videos = _context.VenueVideos
                .Where(v => v.VenueId == venueId)
                .ToList();

            foreach (var video in videos)
            {
                video.IsPrimary = video.VenueVideoId == videoId;
            }

            _context.SaveChanges();
        }

        public void ReorderVideos(int venueId, List<int> videoIds)
        {
            var videos = _context.VenueVideos
                .Where(v => v.VenueId == venueId && videoIds.Contains(v.VenueVideoId))
                .ToList();

            for (int i = 0; i < videoIds.Count; i++)
            {
                var video = videos.FirstOrDefault(v => v.VenueVideoId == videoIds[i]);
                if (video != null)
                {
                    video.DisplayOrder = i;
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
