using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;

namespace HWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .OrderBy(c => c.ParentId ?? 0)
                .ThenBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.ParentCategories = await GetParentCategoriesSelectList();
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,IsActive,ParentId")] Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.UtcNow;
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Danh mục đã được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ParentCategories = await GetParentCategoriesSelectList();
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            
            ViewBag.ParentCategories = await GetParentCategoriesSelectList(category.Id);
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,IsActive,ParentId,CreatedAt")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra không cho phép chọn chính nó hoặc con của nó làm cha
                    if (category.ParentId.HasValue && await IsCircularReference(category.Id, category.ParentId.Value))
                    {
                        ModelState.AddModelError("ParentId", "Không thể chọn danh mục này hoặc danh mục con của nó làm danh mục cha!");
                        ViewBag.ParentCategories = await GetParentCategoriesSelectList(category.Id);
                        return View(category);
                    }
                    
                    category.UpdatedAt = DateTime.UtcNow;
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Danh mục đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ParentCategories = await GetParentCategoriesSelectList(category.Id);
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Children)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (category != null)
            {
                if (category.Products.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục này vì còn có sản phẩm liên kết!";
                    return RedirectToAction(nameof(Index));
                }
                
                if (category.Children.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục này vì còn có danh mục con!";
                    return RedirectToAction(nameof(Index));
                }
                
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Danh mục đã được xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }

        private async Task<bool> IsCircularReference(int categoryId, int parentId)
        {
            var parentCategory = await _context.Categories.FindAsync(parentId);
            if (parentCategory == null)
            {
                return false;
            }

            if (parentCategory.Id == categoryId)
            {
                return true;
            }

            if (parentCategory.ParentId.HasValue)
            {
                return await IsCircularReference(categoryId, parentCategory.ParentId.Value);
            }
            
            return false;
        }

        private async Task<SelectList> GetParentCategoriesSelectList(int? excludeId = null)
        {
            var categories = await _context.Categories
                .Where(c => c.Id != excludeId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var selectList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Chọn danh mục cha (tùy chọn) --" }
            };
            
            selectList.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }));

            return new SelectList(selectList, "Value", "Text");
        }
    }
}
