namespace Core.Models.EventModels
{
    public class CulturalEvent
    {
        public int CulturalId { get; set; }   // PK = FK
        public string? OrganizerName { get; set; } 
        public string? Topic { get; set; } 
        public string? Language { get; set; } 

        public EventBase? EventBase { get; set; } 
    }
}
