namespace Core.Models.EventModels
{
    public class Concert
    {
        public int ConcertId { get; set; }   // PK = FK to EventBase
        public string? ArtistName { get; set; }
        public string? Genre { get; set; } 
        public int BandCount { get; set; }

        public EventBase? EventBase { get; set; }
    }
}
