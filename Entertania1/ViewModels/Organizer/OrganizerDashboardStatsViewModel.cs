using Core.Interfaces.ServicesInterface;

namespace Entertania1.ViewModels.Organizer
{
    /// <summary>
    /// ViewModel for Organizer Dashboard
    /// </summary>
    public class OrganizerDashboardStatsViewModel
    {
        public OrganizerDashboardStatistics Statistics { get; set; } = new();
        public string SelectedPeriod { get; set; } = "last6months";
        public DateTime FilterStartDate { get; set; }
        public DateTime FilterEndDate { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
    }
}