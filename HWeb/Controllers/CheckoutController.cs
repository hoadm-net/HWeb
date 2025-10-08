using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;
using System.Security.Claims;

namespace HWeb.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Checkout
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            // Lấy giỏ hàng của user
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy thông tin user để pre-fill form
            var user = await _context.Users.FindAsync(userId);
            
            var subtotal = cartItems.Sum(x => x.Total);
            var shippingFee = 30000m; // Fixed shipping fee
            var total = subtotal + shippingFee;

            var model = new CheckoutViewModel
            {
                FullName = $"{user?.FirstName} {user?.LastName}".Trim(),
                Email = user?.Email ?? string.Empty,
                PhoneNumber = user?.PhoneNumber ?? string.Empty,
                CartItems = cartItems,
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Total = total,
                PaymentMethod = PaymentMethod.COD // Default to COD
            };

            return View(model);
        }

        // POST: Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Lấy lại giỏ hàng để validate
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Cập nhật lại cart summary cho model
            model.CartItems = cartItems;
            model.Subtotal = cartItems.Sum(x => x.Total);
            model.ShippingFee = 30000m;
            model.Total = model.Subtotal + model.ShippingFee;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tạo order number unique
            var orderNumber = GenerateOrderNumber();

            // Tạo order mới
            var order = new Order
            {
                UserId = userId,
                OrderNumber = orderNumber,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                ShippingAddress = model.Address,
                City = model.City,
                District = model.District,
                Notes = model.Notes,
                Subtotal = model.Subtotal,
                ShippingFee = model.ShippingFee,
                TotalAmount = model.Total,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = model.PaymentMethod == PaymentMethod.COD ? PaymentStatus.Pending : PaymentStatus.Pending,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Tạo order items
            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.Name,
                    Price = cartItem.Price,
                    Quantity = cartItem.Quantity
                };
                _context.OrderItems.Add(orderItem);
            }

            // Xóa cart items sau khi đã tạo order
            _context.CartItems.RemoveRange(cartItems);
            
            await _context.SaveChangesAsync();

            // Xử lý theo phương thức thanh toán
            if (model.PaymentMethod == PaymentMethod.PayPal)
            {
                return RedirectToAction("PayPalPayment", new { orderId = order.Id });
            }
            else
            {
                // COD - redirect to success page
                return RedirectToAction("Success", new { orderId = order.Id });
            }
        }

        // GET: Checkout/PayPalPayment
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PayPalPayment(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // TODO: Implement PayPal integration
            // For now, simulate PayPal payment
            ViewBag.OrderId = orderId;
            ViewBag.Amount = order.TotalAmount;
            
            return View(order);
        }

        // POST: Checkout/ProcessPayPalPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ProcessPayPalPayment(int orderId, string paymentId = "SIMULATED_PAYPAL_PAYMENT")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // TODO: Verify PayPal payment with PayPal API
            // For now, simulate successful payment
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaymentTransactionId = paymentId;
            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction("Success", new { orderId = order.Id });
        }

        // GET: Checkout/Success
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Success(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        private string GenerateOrderNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"HW{timestamp}{random}";
        }
    }
}
