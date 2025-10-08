using HWeb.Data;
using HWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HWeb.Services
{
    public interface ICartSyncService
    {
        Task<bool> SyncSessionCartToDatabase(string userId, HttpContext httpContext);
        Task<bool> SyncDatabaseCartToSession(string userId, HttpContext httpContext);
        void ClearSessionCart(HttpContext httpContext);
    }

    public class CartSyncService : ICartSyncService
    {
        private readonly ApplicationDbContext _context;
        private const string SessionCartKey = "SessionCart";

        private class SessionCartItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        public CartSyncService(ApplicationDbContext context)
        {
            _context = context;
        }

        private List<SessionCartItem> GetSessionCart(HttpContext httpContext)
        {
            var json = httpContext.Session.GetString(SessionCartKey);
            if (string.IsNullOrEmpty(json)) return new List<SessionCartItem>();
            try
            {
                return JsonSerializer.Deserialize<List<SessionCartItem>>(json) ?? new List<SessionCartItem>();
            }
            catch
            {
                return new List<SessionCartItem>();
            }
        }

        private void SaveSessionCart(List<SessionCartItem> items, HttpContext httpContext)
        {
            var json = JsonSerializer.Serialize(items);
            httpContext.Session.SetString(SessionCartKey, json);
        }

        public async Task<bool> SyncSessionCartToDatabase(string userId, HttpContext httpContext)
        {
            try
            {
                var sessionCart = GetSessionCart(httpContext);
                if (!sessionCart.Any()) return true;

                var productIds = sessionCart.Select(s => s.ProductId).ToList();
                var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

                foreach (var sessionItem in sessionCart)
                {
                    var product = products.FirstOrDefault(p => p.Id == sessionItem.ProductId);
                    if (product == null) continue;

                    var existingCartItem = await _context.CartItems
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == sessionItem.ProductId);

                    if (existingCartItem != null)
                    {
                        // Cộng dồn số lượng từ session vào DB
                        existingCartItem.Quantity += sessionItem.Quantity;
                        existingCartItem.Price = product.Price; // Cập nhật giá mới nhất
                    }
                    else
                    {
                        // Tạo mới cart item trong DB
                        _context.CartItems.Add(new CartItem
                        {
                            UserId = userId,
                            ProductId = sessionItem.ProductId,
                            Quantity = sessionItem.Quantity,
                            Price = product.Price,
                            CreatedAt = sessionItem.CreatedAt
                        });
                    }
                }

                await _context.SaveChangesAsync();
                
                // Xóa session cart sau khi đã sync
                ClearSessionCart(httpContext);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SyncSessionCartToDatabase error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncDatabaseCartToSession(string userId, HttpContext httpContext)
        {
            try
            {
                var dbCartItems = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .ToListAsync();

                if (!dbCartItems.Any()) return true;

                var sessionCart = new List<SessionCartItem>();
                foreach (var dbItem in dbCartItems)
                {
                    sessionCart.Add(new SessionCartItem
                    {
                        ProductId = dbItem.ProductId,
                        Quantity = dbItem.Quantity,
                        Price = dbItem.Price,
                        CreatedAt = dbItem.CreatedAt
                    });
                }

                SaveSessionCart(sessionCart, httpContext);

                // Xóa cart items từ database sau khi đã sync
                _context.CartItems.RemoveRange(dbCartItems);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SyncDatabaseCartToSession error: {ex.Message}");
                return false;
            }
        }

        public void ClearSessionCart(HttpContext httpContext)
        {
            httpContext.Session.Remove(SessionCartKey);
        }
    }
}
