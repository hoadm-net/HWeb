using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;

namespace HWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .ToListAsync();
            
            // Build hierarchical list for display
            var hierarchicalCategories = BuildHierarchicalCategoryList(categories, null, "");
            
            return View(hierarchicalCategories);
        }

        // GET: Admin/Categories/Details/5
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

        // GET: Admin/Categories/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropDownsAsync();
            return View();
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,ParentId,IsActive")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra tên danh mục đã tồn tại chưa
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());
                
                if (existingCategory != null)
                {
                    ModelState.AddModelError("Name", "Danh mục này đã tồn tại!");
                    await PopulateDropDownsAsync(category.ParentId);
                    return View(category);
                }

                category.CreatedAt = DateTime.UtcNow;
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Danh mục đã được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            await PopulateDropDownsAsync(category.ParentId);
            return View(category);
        }

        // GET: Admin/Categories/Edit/5
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
            
            await PopulateDropDownsAsync(category.ParentId, category.Id);
            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ParentId,IsActive,CreatedAt")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra tên danh mục đã tồn tại chưa (loại trừ danh mục hiện tại)
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);
                    
                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "Danh mục này đã tồn tại!");
                        await PopulateDropDownsAsync(category.ParentId, category.Id);
                        return View(category);
                    }

                    // Kiểm tra không cho phép chọn chính nó làm parent
                    if (category.ParentId == category.Id)
                    {
                        ModelState.AddModelError("ParentId", "Danh mục không thể là con của chính nó!");
                        await PopulateDropDownsAsync(category.ParentId, category.Id);
                        return View(category);
                    }

                    // Kiểm tra không tạo vòng lặp parent-child
                    if (category.ParentId.HasValue)
                    {
                        var isCircular = await IsCircularReferenceAsync(category.Id, category.ParentId.Value);
                        if (isCircular)
                        {
                            ModelState.AddModelError("ParentId", "Không thể tạo vòng lặp trong cấu trúc danh mục!");
                            await PopulateDropDownsAsync(category.ParentId, category.Id);
                            return View(category);
                        }
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
            
            await PopulateDropDownsAsync(category.ParentId, category.Id);
            return View(category);
        }

        // GET: Admin/Categories/Delete/5
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

        // POST: Admin/Categories/Delete/5
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
                // Kiểm tra xem có danh mục con không
                if (category.Children.Any())
                {
                    TempData["ErrorMessage"] = $"Không thể xóa danh mục '{category.Name}' vì có {category.Children.Count} danh mục con!";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra xem có sản phẩm nào đang sử dụng không
                if (category.Products.Any())
                {
                    TempData["ErrorMessage"] = $"Không thể xóa danh mục '{category.Name}' vì có {category.Products.Count} sản phẩm đang sử dụng!";
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

        private async Task PopulateDropDownsAsync(int? selectedParentId = null, int? excludeId = null)
        {
            var categories = await _context.Categories
                .Where(c => !excludeId.HasValue || c.Id != excludeId.Value)
                .ToListAsync();

            var hierarchicalCategories = BuildHierarchicalSelectList(categories, null, "");
            
            // Thêm option mặc định ở đầu danh sách
            hierarchicalCategories.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Không có danh mục cha (Danh mục gốc) --"
            });
            
            ViewBag.ParentCategories = new SelectList(hierarchicalCategories, "Value", "Text", selectedParentId);
        }

        private List<SelectListItem> BuildHierarchicalSelectList(List<Category> categories, int? parentId, string prefix)
        {
            var result = new List<SelectListItem>();
            var children = categories.Where(c => c.ParentId == parentId).OrderBy(c => c.Name).ToList();

            foreach (var category in children)
            {
                result.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = prefix + category.Name
                });

                // Add child categories with increased indentation
                var childPrefix = prefix == "" ? "--- " : prefix.Replace("---", "------");
                result.AddRange(BuildHierarchicalSelectList(categories, category.Id, childPrefix));
            }

            return result;
        }

        private List<Category> BuildHierarchicalCategoryList(List<Category> categories, int? parentId, string prefix)
        {
            var result = new List<Category>();
            var children = categories.Where(c => c.ParentId == parentId).OrderBy(c => c.Name).ToList();

            foreach (var category in children)
            {
                // Create a copy with modified name for display
                var displayCategory = new Category
                {
                    Id = category.Id,
                    Name = prefix + category.Name,
                    Description = category.Description,
                    ParentId = category.ParentId,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    Parent = category.Parent,
                    Children = category.Children,
                    Products = category.Products
                };
                
                result.Add(displayCategory);

                // Add child categories with increased indentation
                var childPrefix = prefix == "" ? "--- " : prefix.Replace("---", "------");
                result.AddRange(BuildHierarchicalCategoryList(categories, category.Id, childPrefix));
            }

            return result;
        }

        private async Task<bool> IsCircularReferenceAsync(int categoryId, int parentId)
        {
            var parent = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == parentId);

            if (parent == null)
                return false;

            if (parent.Id == categoryId)
                return true;

            if (parent.ParentId.HasValue)
                return await IsCircularReferenceAsync(categoryId, parent.ParentId.Value);

            return false;
        }
    }
}
