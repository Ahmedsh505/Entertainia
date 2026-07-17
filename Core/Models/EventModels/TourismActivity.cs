namespace Core.Models.EventModels
{
    public class TourismActivity
    {
        public int TourismId { get; set; }   // PK = FK
        public string? GuideName { get; set; } 
        public string? TourType { get; set; } // Historical, Adventure...
        public int DurationHours { get; set; }

        public EventBase? EventBase { get; set; } = null!;
    }
}
