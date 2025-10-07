using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HWeb.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        public int ProductId { get; set; }
        
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual Product Product { get; set; } = null!;
        
        // Calculated properties
        [NotMapped]
        public decimal Total => Price * Quantity;
    }
}
