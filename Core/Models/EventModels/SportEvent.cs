namespace Core.Models.EventModels
{
    public class SportEvent
    {
        public int SportId { get; set; }   // PK = FK
        public string? SportType { get; set; } 
        public string? TeamA { get; set; } 
        public string? TeamB { get; set; } 
        public string? LeagueName { get; set; } 

        public EventBase? EventBase { get; set; } 
    }
}
