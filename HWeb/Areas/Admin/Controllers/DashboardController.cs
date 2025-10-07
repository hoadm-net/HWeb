using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;

namespace HWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
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

            // Sản phẩm mới nhất
            var recentProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentProducts = recentProducts;

            return View();
        }
    }
}
