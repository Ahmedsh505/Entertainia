using Core.Models.VideoModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IEventVideoRepo
    {
        void Add(EventVideo video);
        void Update(EventVideo video);
        void Delete(EventVideo video);
        EventVideo? GetById(int id);
        List<EventVideo> GetByEventId(int eventId);
        EventVideo? GetPrimaryVideo(int eventId);
        void SetPrimaryVideo(int eventId, int videoId);
        void ReorderVideos(int eventId, List<int> videoIds);
        void Save();
    }
}
