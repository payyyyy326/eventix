using Eventix.Share.Event;

namespace Eventix.Web.Models;

public class DashboardViewModel
{
    // Stat cards
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int TotalTicketsSold { get; set; }
    public decimal TotalRevenue { get; set; }

    // Biểu đồ doanh thu + vé theo tháng (12 tháng gần nhất)
    public List<string> MonthLabels { get; set; } = new();
    public List<decimal> RevenueByMonth { get; set; } = new();
    public List<int> TicketsByMonth { get; set; } = new();

    // Biểu đồ donut trạng thái
    public Dictionary<string, int> EventsByStatus { get; set; } = new();
}
