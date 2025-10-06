using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HWeb.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        public string? ShortDescription { get; set; }
        
        public string? DetailDescription { get; set; }
        
        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public int Stock { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Foreign Key
        [Required(ErrorMessage = "Danh mục sản phẩm là bắt buộc")]
        public int CategoryId { get; set; }
        
        // Navigation properties
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
