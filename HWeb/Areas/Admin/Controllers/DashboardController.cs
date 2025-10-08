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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Thống kê tổng quan
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.ActiveProducts = await _context.Products.CountAsync(p => p.IsActive);
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            ViewBag.ActiveCategories = await _context.Categories.CountAsync(c => c.IsActive);
            ViewBag.TotalTags = await _context.Tags.CountAsync();
            ViewBag.ActiveTags = await _context.Tags.CountAsync(t => t.IsActive);
            ViewBag.OutOfStockProducts = await _context.Products.CountAsync(p => p.Stock == 0);
            ViewBag.LowStockProducts = await _context.Products.CountAsync(p => p.Stock > 0 && p.Stock < 10);

            // Thống kê reviews
            ViewBag.TotalReviews = await _context.Reviews.CountAsync();
            ViewBag.PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved);
            ViewBag.ApprovedReviews = await _context.Reviews.CountAsync(r => r.IsApproved);
            ViewBag.ReviewsToday = await _context.Reviews.CountAsync(r => r.CreatedAt.Date == DateTime.Today);
            ViewBag.AverageRating = await _context.Reviews.Where(r => r.IsApproved).AverageAsync(r => (double?)r.Rating) ?? 0;

            // Thống kê đơn hàng
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            ViewBag.TodayOrders = await _context.Orders.CountAsync(o => o.CreatedAt.Date == DateTime.Today);
            
            // Fix SumAsync syntax
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount);
            ViewBag.TotalRevenue = totalRevenue ?? 0;

            // Thống kê người dùng
            var currentTime = DateTimeOffset.UtcNow;
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.NewUsersToday = await _userManager.Users.CountAsync(u => u.CreatedAt.Date == DateTime.Today);
            ViewBag.NewUsersThisMonth = await _userManager.Users.CountAsync(u => u.CreatedAt.Month == DateTime.Now.Month && u.CreatedAt.Year == DateTime.Now.Year);
            
            // Fix LINQ translation issue by bringing users with lockout data into memory first
            var usersWithLockout = await _userManager.Users
                .Where(u => u.LockoutEnd != null)
                .Select(u => u.LockoutEnd)
                .ToListAsync();
            ViewBag.LockedUsers = usersWithLockout.Count(lockoutEnd => lockoutEnd > currentTime);

            // Sản phẩm mới nhất
            var recentProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentProducts = recentProducts;

            // Reviews mới nhất cần duyệt
            var pendingReviews = await _context.Reviews
                .Include(r => r.Product)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.PendingReviews = pendingReviews;

            // Đơn hàng mới nhất cần xử lý
            var pendingOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Status == OrderStatus.Pending)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.PendingOrders = pendingOrders;

            return View();
        }
    }
}
