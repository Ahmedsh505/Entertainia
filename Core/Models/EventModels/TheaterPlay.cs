namespace Core.Models.EventModels
{
    public class TheaterPlay
    {
        public int TheaterId { get; set; }   // PK = FK
        public string? Director { get; set; } 
        public string? ActorsList { get; set; } 
        public int DurationMinutes { get; set; }

        public EventBase? EventBase { get; set; } 
    }
}
