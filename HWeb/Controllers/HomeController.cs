using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Models;
using HWeb.Data;

namespace HWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // First check if we can connect to database
            var totalProducts = await _context.Products.CountAsync();
            _logger.LogInformation($"Total products in database: {totalProducts}");
            
            // Get featured products (first 4 products from database)
            var featuredProducts = await _context.Products
                .OrderBy(p => p.Id)
                .Take(4)
                .ToListAsync();
            
            _logger.LogInformation($"Retrieved {featuredProducts.Count} featured products");
            
            // Try to load categories separately if needed
            foreach (var product in featuredProducts)
            {
                if (product.CategoryId.HasValue)
                {
                    product.Category = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Id == product.CategoryId);
                }
            }
            
            return View(featuredProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products from database");
            return View(new List<Product>());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}