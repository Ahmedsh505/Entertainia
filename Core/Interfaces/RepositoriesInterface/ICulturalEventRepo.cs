using Core.Models.EventModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface ICulturalEventRepo
    {
        void Add(CulturalEvent culturalEvent);
        void Update(CulturalEvent culturalEvent);
        void Delete(CulturalEvent culturalEvent);
        CulturalEvent? GetById(int id);
        List<CulturalEvent> GetAll();
        void Save();
    }
}
