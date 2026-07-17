using Core.Models.VideoModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IVenueVideoRepo
    {
        void Add(VenueVideo video);
        void Update(VenueVideo video);
        void Delete(VenueVideo video);
        VenueVideo? GetById(int id);
        List<VenueVideo> GetByVenueId(int venueId);
        VenueVideo? GetPrimaryVideo(int venueId);
        void SetPrimaryVideo(int venueId, int videoId);
        void ReorderVideos(int venueId, List<int> videoIds);
        void Save();
    }
}
