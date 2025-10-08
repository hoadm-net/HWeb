using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HWeb.Models
{
    public class Review
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        [MaxLength(450)]
        public string? UserId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        public int Rating { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;
        
        public bool IsApproved { get; set; } = false;
        
        public DateTime? ApprovedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Product Product { get; set; } = null!;
        public virtual ApplicationUser? User { get; set; }

        public override string ToString()
        {
            return $"{Title} - {Rating}/5 sao - {CustomerName} ({CreatedAt:dd/MM/yyyy})";
        }
    }
}
