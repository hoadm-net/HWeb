using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HWeb.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        
        public int ProductId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? ProductImageUrl { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        // Computed properties for view compatibility
        public decimal Price => UnitPrice;
        public decimal Total => TotalPrice;
        
        // Navigation Properties
        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
