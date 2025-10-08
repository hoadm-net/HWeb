using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HWeb.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Chờ xác nhận")]
        Pending = 1,
        
        [Display(Name = "Đã xác nhận")]
        Confirmed = 2,
        
        [Display(Name = "Đang giao hàng")]
        Shipping = 3,
        
        [Display(Name = "Đã giao hàng")]
        Delivered = 4,
        
        [Display(Name = "Đã hủy")]
        Cancelled = 5
    }

    public enum PaymentMethod
    {
        [Display(Name = "Thanh toán khi nhận hàng (COD)")]
        COD = 1,
        
        [Display(Name = "Thanh toán qua PayPal")]
        PayPal = 2
    }

    public enum PaymentStatus
    {
        [Display(Name = "Chưa thanh toán")]
        Pending = 1,
        
        [Display(Name = "Đã thanh toán")]
        Paid = 2,
        
        [Display(Name = "Thất bại")]
        Failed = 3,
        
        [Display(Name = "Hoàn tiền")]
        Refunded = 4
    }

    public class Order
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string OrderNumber { get; set; } = string.Empty;
        
        // Customer Information
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        // Shipping Information
        [Required]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string District { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        // Order Details
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        // Payment Information
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        
        [MaxLength(200)]
        public string? PaymentTransactionId { get; set; }
        
        // Order Status
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        
        // Navigation Properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
