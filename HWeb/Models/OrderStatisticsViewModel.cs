using System.ComponentModel.DataAnnotations;

namespace HWeb.Models
{
    public class OrderStatisticsViewModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersThisMonth { get; set; }
        public double AverageOrderValue { get; set; }
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();
        public List<Order> RecentOrders { get; set; } = new List<Order>();
    }

    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public Product? Product { get; set; }
    }
}
