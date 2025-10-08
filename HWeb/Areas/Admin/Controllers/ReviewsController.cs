using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;

namespace HWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Reviews
        public async Task<IActionResult> Index(string? status = "pending", int page = 1)
        {
            const int pageSize = 10;
            
            var query = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            // Filter by status
            switch (status?.ToLower())
            {
                case "approved":
                    query = query.Where(r => r.IsApproved);
                    break;
                case "pending":
                default:
                    query = query.Where(r => !r.IsApproved);
                    break;
                case "all":
                    // No filter
                    break;
            }

            var totalReviews = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentStatus"] = status;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalReviews / pageSize);
            ViewData["TotalReviews"] = totalReviews;

            return View(reviews);
        }

        // GET: Admin/Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Product)
                    .ThenInclude(p => p.Category)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Admin/Reviews/Approve/5
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá." });
            }

            review.IsApproved = true;
            review.ApprovedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đánh giá đã được duyệt thành công." });
        }

        // POST: Admin/Reviews/Reject/5
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá." });
            }

            review.IsApproved = false;
            review.ApprovedAt = null;
            
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đánh giá đã bị từ chối." });
        }

        // GET: Admin/Reviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Admin/Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Đánh giá đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Reviews/ProductReviews/5
        public async Task<IActionResult> ProductReviews(int productId, int page = 1)
        {
            const int pageSize = 10;

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var query = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId);

            var totalReviews = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["Product"] = product;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalReviews / pageSize);
            ViewData["TotalReviews"] = totalReviews;

            return View(reviews);
        }

        // GET: Admin/Reviews/Statistics
        public async Task<IActionResult> Statistics()
        {
            var stats = new
            {
                TotalReviews = await _context.Reviews.CountAsync(),
                PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved),
                ApprovedReviews = await _context.Reviews.CountAsync(r => r.IsApproved),
                AverageRating = await _context.Reviews.Where(r => r.IsApproved).AverageAsync(r => (double?)r.Rating) ?? 0,
                ReviewsToday = await _context.Reviews.CountAsync(r => r.CreatedAt.Date == DateTime.Today),
                ReviewsThisMonth = await _context.Reviews.CountAsync(r => r.CreatedAt.Month == DateTime.Now.Month && r.CreatedAt.Year == DateTime.Now.Year)
            };

            // Top rated products
            var topRatedProducts = await _context.Products
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .Where(p => p.Reviews.Any(r => r.IsApproved))
                .Select(p => new
                {
                    Product = p,
                    AverageRating = p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating),
                    ReviewCount = p.Reviews.Count(r => r.IsApproved)
                })
                .OrderByDescending(x => x.AverageRating)
                .ThenByDescending(x => x.ReviewCount)
                .Take(10)
                .ToListAsync();

            ViewBag.Stats = stats;
            ViewBag.TopRatedProducts = topRatedProducts;

            return View();
        }
    }
}
