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
        PayPal = 2,
        
        [Display(Name = "Chuyển khoản ngân hàng")]
        BankTransfer = 3
    }

    public enum PaymentStatus
    {
        [Display(Name = "Chờ thanh toán")]
        Pending = 1,
        
        [Display(Name = "Đã thanh toán")]
        Paid = 2,
        
        [Display(Name = "Thanh toán thất bại")]
        Failed = 3,
        
        [Display(Name = "Đã hoàn tiền")]
        Refunded = 4
    }

    public class Order
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(450)]
        public string? UserId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string OrderNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? District { get; set; }
        
        [MaxLength(100)]
        public string? City { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        
        [MaxLength(100)]
        public string? PaymentTransactionId { get; set; }
        
        public bool IsPaid { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? ConfirmedAt { get; set; }
        
        public DateTime? ShippedAt { get; set; }
        
        public DateTime? DeliveredAt { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        
        [MaxLength(500)]
        public string? CancelReason { get; set; }
        
        // Computed properties for view compatibility
        public string FullName => CustomerName;
        public string PhoneNumber => CustomerPhone;
        public string Email => CustomerEmail;
        
        // Navigation Properties
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        
        // Calculated Properties
        public int TotalItems => OrderItems.Sum(oi => oi.Quantity);
        
        public string StatusDisplayName => Status.GetDisplayName();
        
        public string PaymentMethodDisplayName => PaymentMethod.GetDisplayName();

        public override string ToString()
        {
            return $"Đơn hàng #{OrderNumber} - {CustomerName} - {TotalAmount:C}";
        }
    }
    
    // Extension method for enum display names
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            var displayAttribute = enumValue.GetType()
                .GetField(enumValue.ToString())
                ?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;
            
            return displayAttribute?.Name ?? enumValue.ToString();
        }
    }
}
