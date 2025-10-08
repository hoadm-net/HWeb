using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace HWeb.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PageSize = 8; // 8 sản phẩm mỗi trang

        public ProductsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Products
        public async Task<IActionResult> Index(int page = 1)
        {
            var totalProducts = await _context.Products.CountAsync();
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
            
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalProducts / PageSize);
            ViewData["TotalProducts"] = totalProducts;
            
            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy sản phẩm liên quan (cùng danh mục, khác sản phẩm hiện tại)
            var relatedProductsQuery = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .ToListAsync();

            // Random order ở client side
            var random = new Random();
            var relatedProducts = relatedProductsQuery
                .OrderBy(p => random.Next())
                .Take(4) // Lấy 4 sản phẩm liên quan
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;
            
            return View(product);
        }

        // GET: Products/AoSoMi - Áo sơ mi category page
        public async Task<IActionResult> AoSoMi(int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Category != null && 
                           (p.Category.Name.ToLower().Contains("áo") || 
                            p.Category.Name.ToLower().Contains("sơ mi")));

            var totalProducts = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["CategoryName"] = "Áo sơ mi";
            ViewData["CategoryDescription"] = "Bộ sưu tập áo sơ mi công sở và thời trang với nhiều kiểu dáng hiện đại, chất liệu cao cấp.";
            ViewData["CategoryIcon"] = "fas fa-tshirt";
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalProducts / PageSize);
            ViewData["TotalProducts"] = totalProducts;
            ViewData["CategoryAction"] = "AoSoMi";
            
            return View("Category", products);
        }

        // GET: Products/QuanTay - Quần tây category page  
        public async Task<IActionResult> QuanTay(int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Category != null && 
                           (p.Category.Name.ToLower().Contains("quần") || 
                            p.Category.Name.ToLower().Contains("tây")));

            var totalProducts = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["CategoryName"] = "Quần tây";
            ViewData["CategoryDescription"] = "Bộ sưu tập quần tây nam nữ công sở, thanh lịch và sang trọng phù hợp mọi dịp.";
            ViewData["CategoryIcon"] = "fas fa-user-tie";
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalProducts / PageSize);
            ViewData["TotalProducts"] = totalProducts;
            ViewData["CategoryAction"] = "QuanTay";
            
            return View("Category", products);
        }

        // GET: Products/ChanVay - Chân váy category page
        public async Task<IActionResult> ChanVay(int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Category != null && 
                           (p.Category.Name.ToLower().Contains("váy") || 
                            p.Category.Name.ToLower().Contains("chân")));

            var totalProducts = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["CategoryName"] = "Chân váy";
            ViewData["CategoryDescription"] = "Bộ sưu tập chân váy nữ đa dạng từ công sở đến dạo phố, tôn lên vẻ đẹp quyến rũ.";
            ViewData["CategoryIcon"] = "fas fa-female";
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalProducts / PageSize);
            ViewData["TotalProducts"] = totalProducts;
            ViewData["CategoryAction"] = "ChanVay";
            
            return View("Category", products);
        }

        // POST: Products/AddReview
        [HttpPost]
        public async Task<IActionResult> AddReview(int productId, int rating, string title, string comment, string? customerName = null)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                var review = new Review
                {
                    ProductId = productId,
                    Rating = rating,
                    Title = title,
                    Comment = comment,
                    CreatedAt = DateTime.Now,
                    IsApproved = false // Cần được duyệt trước khi hiển thị
                };

                // Nếu user đã đăng nhập
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        review.UserId = userId;
                        var user = await _userManager.FindByIdAsync(userId);
                        review.CustomerName = user?.UserName ?? "Khách hàng";
                    }
                    else
                    {
                        review.CustomerName = "Khách hàng";
                    }
                }
                else
                {
                    // Nếu chưa đăng nhập, lưu tên khách hàng
                    review.CustomerName = !string.IsNullOrEmpty(customerName) ? customerName : "Khách hàng";
                }

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đánh giá của bạn đã được gửi thành công. Chúng tôi sẽ duyệt và hiển thị sớm nhất có thể." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        // GET: Products/Search
        public async Task<IActionResult> Search(string q, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index");
            }

            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Name.Contains(q) || 
                           (!string.IsNullOrEmpty(p.ShortDescription) && p.ShortDescription.Contains(q)) || 
                           (!string.IsNullOrEmpty(p.DetailDescription) && p.DetailDescription.Contains(q)) ||
                           (p.Category != null && p.Category.Name.Contains(q)));

            var totalProducts = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["SearchQuery"] = q;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalProducts / PageSize);
            ViewData["TotalProducts"] = totalProducts;
            
            return View(products);
        }
    }
}
