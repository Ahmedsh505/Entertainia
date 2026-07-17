using Core.Models.BookingModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IBookingRepo
    {
        void Add(Booking booking);
        void Update(Booking booking);
        void Delete(Booking booking);
        List<Booking> GetAll();
        Booking GetById(int id);
        List<Booking> GetByUser(int userId);
        List<Booking> GetByEvent(int eventId);
        void Save();
    }
}
