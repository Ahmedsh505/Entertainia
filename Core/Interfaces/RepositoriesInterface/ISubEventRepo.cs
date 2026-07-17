using Core.Models;
using System.Collections.Generic;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface ISubEventRepo<T> where T : class
    {
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        List<T> GetAll();
        T GetById(int id);
        void Save();
    }
}
