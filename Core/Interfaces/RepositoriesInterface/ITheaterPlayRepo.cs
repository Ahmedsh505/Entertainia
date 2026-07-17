using Core.Models.EventModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface ITheaterPlayRepo
    {
        void Add(TheaterPlay theaterPlay);
        void Update(TheaterPlay theaterPlay);
        void Delete(TheaterPlay theaterPlay);
        TheaterPlay? GetById(int id);
        List<TheaterPlay> GetAll();
        void Save();
    }
}
