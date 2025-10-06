using System.ComponentModel.DataAnnotations;

namespace HWeb.Models
{
    public class Tag
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Tên tag là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên tag không được vượt quá 50 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
    }
}
