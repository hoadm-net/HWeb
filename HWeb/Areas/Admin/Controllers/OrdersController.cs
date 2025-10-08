using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;
using Microsoft.AspNetCore.Identity;

namespace HWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index(OrderStatus? status = null, int page = 1)
        {
            const int pageSize = 10;
            
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Filter by status if specified
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var totalOrders = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentStatus"] = status;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalOrders / pageSize);
            ViewData["TotalOrders"] = totalOrders;

            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Admin/Orders/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus, string? cancelReason = null)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                }

                var oldStatus = order.Status;
                order.Status = newStatus;

                // Update timestamps based on status
                switch (newStatus)
                {
                    case OrderStatus.Confirmed:
                        order.ConfirmedAt = DateTime.Now;
                        break;
                    case OrderStatus.Shipping:
                        order.ShippedAt = DateTime.Now;
                        break;
                    case OrderStatus.Delivered:
                        order.DeliveredAt = DateTime.Now;
                        order.IsPaid = true; // Assume payment is completed on delivery
                        break;
                    case OrderStatus.Cancelled:
                        order.CancelledAt = DateTime.Now;
                        order.CancelReason = cancelReason;
                        break;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Đã cập nhật trạng thái đơn hàng từ '{oldStatus.GetDisplayName()}' thành '{newStatus.GetDisplayName()}'" 
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // GET: Admin/Orders/Statistics
        public async Task<IActionResult> Statistics()
        {
            var stats = new OrderStatisticsViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                ConfirmedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Confirmed),
                ShippingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipping),
                DeliveredOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered),
                CancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = await _context.Orders.Where(o => o.Status == OrderStatus.Delivered).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
                OrdersToday = await _context.Orders.CountAsync(o => o.CreatedAt.Date == DateTime.Today),
                OrdersThisMonth = await _context.Orders.CountAsync(o => o.CreatedAt.Month == DateTime.Now.Month && o.CreatedAt.Year == DateTime.Now.Year),
                AverageOrderValue = await _context.Orders.Where(o => o.Status == OrderStatus.Delivered).AverageAsync(o => (double?)o.TotalAmount) ?? 0
            };

            // Top selling products
            var topProductsQuery = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.Status == OrderStatus.Delivered)
                .GroupBy(oi => new { oi.ProductId, oi.ProductName })
                .Select(g => new TopProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice),
                    Product = g.First().Product
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(10)
                .ToListAsync();

            // Recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            stats.TopProducts = topProductsQuery;
            stats.RecentOrders = recentOrders;

            return View(stats);
        }

        // GET: Admin/Orders/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);
                
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                }

                // Only allow deletion of cancelled orders
                if (order.Status != OrderStatus.Cancelled)
                {
                    return Json(new { success = false, message = "Chỉ có thể xóa đơn hàng đã bị hủy." });
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa đơn hàng thành công." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }
    }
}
