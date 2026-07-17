using Core.Models.EventModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IConcertRepo
    {
        void Add(Concert concert);
        void Update(Concert concert);
        void Delete(Concert concert);
        Concert? GetById(int id);
        List<Concert> GetAll();
        void Save();
    }
}
