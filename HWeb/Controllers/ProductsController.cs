using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;

namespace HWeb.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 8; // 8 sản phẩm mỗi trang

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
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
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

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
    }
}
