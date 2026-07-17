using Core.Models.EventModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface ITourismActivityRepo
    {
        void Add(TourismActivity tourismActivity);
        void Update(TourismActivity tourismActivity);
        void Delete(TourismActivity tourismActivity);
        TourismActivity? GetById(int id);
        List<TourismActivity> GetAll();
        void Save();
    }
}
