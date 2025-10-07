namespace HWeb.Models
{
    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }
        public int TotalItems => Items.Sum(x => x.Quantity);
        public bool IsEmpty => !Items.Any();
    }
}

