using Core.Interfaces.ServicesInterface;

namespace Entertania1.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for Admin Dashboard Statistics View
    /// </summary>
    public class AdminDashboardStatsViewModel
    {
        public AdminDashboardStatistics Statistics { get; set; } = new();
        public string SelectedPeriod { get; set; } = "last30days";
        public DateTime FilterStartDate { get; set; }
        public DateTime FilterEndDate { get; set; }
    }
}