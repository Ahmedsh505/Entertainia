using Core.Models.EventModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface ISportEventRepo
    {
        void Add(SportEvent sportEvent);
        void Update(SportEvent sportEvent);
        void Delete(SportEvent sportEvent);
        SportEvent? GetById(int id);
        List<SportEvent> GetAll();
        void Save();
    }
}
