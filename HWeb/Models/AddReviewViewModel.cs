using System.ComponentModel.DataAnnotations;

namespace HWeb.Models
{
    public class AddReviewViewModel
    {
        public int ProductId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề đánh giá")]
        [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề đánh giá")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        [Display(Name = "Đánh giá")]
        public int Rating { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [MaxLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
        [Display(Name = "Nội dung đánh giá")]
        public string Comment { get; set; } = string.Empty;
        
        // For display
        public string ProductName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }
}
