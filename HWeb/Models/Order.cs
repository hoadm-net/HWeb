using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HWeb.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }

    public class Order
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string OrderNumber { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        [Required]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required]
        public string CustomerEmail { get; set; } = string.Empty;
        
        [Required]
        public string CustomerPhone { get; set; } = string.Empty;
        
        [Required]
        public string ShippingAddress { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
