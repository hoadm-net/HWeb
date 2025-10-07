using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;
using System.Security.Claims;
using System.Text.Json;

namespace HWeb.Controllers
{
    public class CartController : Controller
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

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        private List<SessionCartItem> GetSessionCart()
        {
            var json = HttpContext.Session.GetString(SessionCartKey);
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

        private void SaveSessionCart(List<SessionCartItem> items)
        {
            var json = JsonSerializer.Serialize(items);
            HttpContext.Session.SetString(SessionCartKey, json);
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated)
            {
                var sessionItems = GetSessionCart();
                var productIds = sessionItems.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products.Include(p => p.Category)
                    .Where(p => productIds.Contains(p.Id)).ToListAsync();
                var cartItems = new List<CartItem>();
                int tempId = -1;
                foreach (var s in sessionItems)
                {
                    var product = products.FirstOrDefault(p => p.Id == s.ProductId);
                    if (product == null) continue;
                    cartItems.Add(new CartItem
                    {
                        Id = tempId--, // negative temp id for session items
                        ProductId = product.Id,
                        Product = product,
                        Quantity = s.Quantity,
                        Price = s.Price,
                        CreatedAt = s.CreatedAt,
                        UserId = string.Empty
                    });
                }
                var vm = new CartViewModel
                {
                    Items = cartItems,
                    Subtotal = cartItems.Sum(x => x.Total),
                    ShippingFee = cartItems.Any() ? 30000m : 0m,
                    Total = cartItems.Sum(x => x.Total) + (cartItems.Any() ? 30000m : 0m)
                };
                return View(vm);
            }
            var cartItemsDb = await GetCartItemsAsync();
            var cartViewModel = new CartViewModel
            {
                Items = cartItemsDb,
                Subtotal = cartItemsDb.Sum(x => x.Total),
                ShippingFee = cartItemsDb.Any() ? 30000m : 0m,
                Total = cartItemsDb.Sum(x => x.Total) + (cartItemsDb.Any() ? 30000m : 0m)
            };
            return View(cartViewModel);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                Console.WriteLine($"AddToCart called: ProductId={productId}, Quantity={quantity}, IsAuthenticated={IsAuthenticated}");
                if (quantity < 1) quantity = 1;

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    Console.WriteLine($"AddToCart: Product {productId} not found");
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                if (!IsAuthenticated)
                {
                    Console.WriteLine("AddToCart path: SESSION (anonymous)");
                    var sessionCart = GetSessionCart();
                    var existing = sessionCart.FirstOrDefault(c => c.ProductId == productId);
                    if (existing != null)
                    {
                        existing.Quantity += quantity;
                        existing.Price = product.Price;
                        Console.WriteLine($"Session cart updated item ProductId={productId}, NewQuantity={existing.Quantity}");
                    }
                    else
                    {
                        sessionCart.Add(new SessionCartItem
                        {
                            ProductId = product.Id,
                            Quantity = quantity,
                            Price = product.Price,
                            CreatedAt = DateTime.UtcNow
                        });
                        Console.WriteLine($"Session cart added new item ProductId={productId}, Quantity={quantity}");
                    }
                    SaveSessionCart(sessionCart);
                    var count = sessionCart.Sum(x => x.Quantity);
                    return Json(new { success = true, message = $"Đã thêm {quantity} sản phẩm vào giỏ hàng!", cartCount = count });
                }

                // Authenticated user path
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                Console.WriteLine($"AddToCart path: AUTH userId={userId}, userExists={userExists}");
                if (!userExists)
                {
                    return Json(new { success = false, message = "Tài khoản không hợp lệ (user không tồn tại)." });
                }

                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                    existingCartItem.Price = product.Price;
                    Console.WriteLine($"Updated existing DB cart item Id={existingCartItem.Id}, NewQuantity={existingCartItem.Quantity}");
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = userId,
                        ProductId = product.Id,
                        Quantity = quantity,
                        Price = product.Price,
                        CreatedAt = DateTime.UtcNow
                    });
                    Console.WriteLine($"Added new DB cart item ProductId={productId}, Quantity={quantity}");
                }

                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine("AddToCart: SaveChanges OK");
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"AddToCart SaveChanges Exception: {saveEx.Message}\nInner: {saveEx.InnerException?.Message}\nStack: {saveEx.StackTrace}");
                    return Json(new { success = false, message = $"Lỗi lưu CSDL: {saveEx.InnerException?.Message ?? saveEx.Message}" });
                }

                var cartCountAuth = await GetCartCountAsync();
                return Json(new { success = true, message = $"Đã thêm {quantity} sản phẩm vào giỏ hàng!", cartCount = cartCountAuth });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddToCart Outer Exception: {ex.Message}\nInner: {ex.InnerException?.Message}\nStack: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào giỏ hàng (outer)." });
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity, int? productId = null)
        {
            try
            {
                if (quantity <= 0) quantity = 1;
                if (!IsAuthenticated)
                {
                    if (productId == null) return Json(new { success = false, message = "Thiếu mã sản phẩm." });
                    var sessionCart = GetSessionCart();
                    var item = sessionCart.FirstOrDefault(x => x.ProductId == productId);
                    if (item == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                    item.Quantity = quantity;
                    SaveSessionCart(sessionCart);
                    var subtotalS = sessionCart.Sum(x => x.Price * x.Quantity);
                    var totalS = subtotalS + (sessionCart.Any() ? 30000m : 0m);
                    return Json(new { success = true, itemTotal = (item.Price * item.Quantity).ToString("N0"), subtotal = subtotalS.ToString("N0"), total = totalS.ToString("N0"), cartCount = sessionCart.Sum(x => x.Quantity) });
                }
                // Authenticated user path
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
                if (cartItem == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();
                var cartItems = await GetCartItemsAsync();
                var subtotal = cartItems.Sum(x => x.Total);
                var total = subtotal + 30000m;
                return Json(new { success = true, itemTotal = cartItem.Total.ToString("N0"), subtotal = subtotal.ToString("N0"), total = total.ToString("N0"), cartCount = cartItems.Sum(x => x.Quantity) });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId, int? productId = null)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    if (productId == null) return Json(new { success = false, message = "Thiếu mã sản phẩm." });
                    var sessionCart = GetSessionCart();
                    var idx = sessionCart.FindIndex(x => x.ProductId == productId);
                    if (idx == -1) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                    sessionCart.RemoveAt(idx);
                    SaveSessionCart(sessionCart);
                    var subtotalS = sessionCart.Sum(x => x.Price * x.Quantity);
                    var totalS = subtotalS + (sessionCart.Any() ? 30000m : 0m);
                    return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng.", subtotal = subtotalS.ToString("N0"), total = totalS.ToString("N0"), cartCount = sessionCart.Sum(x => x.Quantity), isEmpty = !sessionCart.Any() });
                }
                // Authenticated user path
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
                if (cartItem == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                var cartItems = await GetCartItemsAsync();
                var subtotal = cartItems.Sum(x => x.Total);
                var total = subtotal + (cartItems.Any() ? 30000m : 0m);
                return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng.", subtotal = subtotal.ToString("N0"), total = total.ToString("N0"), cartCount = cartItems.Sum(x => x.Quantity), isEmpty = !cartItems.Any() });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // GET: Cart/GetCartCount (for AJAX)
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            if (!IsAuthenticated)
            {
                var sessionCart = GetSessionCart();
                return Json(new { count = sessionCart.Sum(x => x.Quantity) });
            }
            var count = await GetCartCountAsync();
            return Json(new { count });
        }

        // POST: Cart/ClearCart
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    HttpContext.Session.Remove(SessionCartKey);
                    return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng." });
                }
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var cartItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng." });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // Private helper methods
        private string GetUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
            
            // For anonymous users, use session ID
            var sessionId = HttpContext.Session.Id;
            if (string.IsNullOrEmpty(sessionId))
            {
                HttpContext.Session.SetString("CartSessionId", Guid.NewGuid().ToString());
                sessionId = HttpContext.Session.GetString("CartSessionId") ?? Guid.NewGuid().ToString();
            }
            return $"anonymous_{sessionId}";
        }

        private async Task<List<CartItem>> GetCartItemsAsync()
        {
            var userId = GetUserId();
            return await _context.CartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        private async Task<int> GetCartCountAsync()
        {
            var userId = GetUserId();
            return await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);
        }
    }
}
