namespace HWeb.Models
{
    public class ProductTag
    {
        public int ProductId { get; set; }
        public int TagId { get; set; }
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual Tag Tag { get; set; } = null!;
    }
}

