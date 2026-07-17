using Core.Interfaces.RepositoriesInterface;
using Core.Models.VideoModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class EventVideoRepository : IEventVideoRepo
    {
        private readonly EntertaniaDbcontext _context;

        public EventVideoRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(EventVideo video)
        {
            _context.EventVideos.Add(video);
        }

        public void Update(EventVideo video)
        {
            _context.EventVideos.Update(video);
        }

        public void Delete(EventVideo video)
        {
            _context.EventVideos.Remove(video);
        }

        public EventVideo? GetById(int id)
        {
            return _context.EventVideos
                .Include(v => v.UploadedBy)
                .FirstOrDefault(v => v.EventVideoId == id);
        }

        public List<EventVideo> GetByEventId(int eventId)
        {
            return _context.EventVideos
                .Where(v => v.EventId == eventId)
                .OrderBy(v => v.DisplayOrder)
                .ToList();
        }

        public EventVideo? GetPrimaryVideo(int eventId)
        {
            return _context.EventVideos
                .FirstOrDefault(v => v.EventId == eventId && v.IsPrimary);
        }

        public void SetPrimaryVideo(int eventId, int videoId)
        {
            var videos = _context.EventVideos
                .Where(v => v.EventId == eventId)
                .ToList();

            foreach (var video in videos)
            {
                video.IsPrimary = video.EventVideoId == videoId;
            }

            _context.SaveChanges();
        }

        public void ReorderVideos(int eventId, List<int> videoIds)
        {
            var videos = _context.EventVideos
                .Where(v => v.EventId == eventId && videoIds.Contains(v.EventVideoId))
                .ToList();

            for (int i = 0; i < videoIds.Count; i++)
            {
                var video = videos.FirstOrDefault(v => v.EventVideoId == videoIds[i]);
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
