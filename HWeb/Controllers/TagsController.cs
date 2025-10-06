using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;

namespace HWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TagsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TagsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tags
        public async Task<IActionResult> Index()
        {
            var tags = await _context.Tags
                .Include(t => t.ProductTags)
                .ThenInclude(pt => pt.Product)
                .OrderBy(t => t.Name)
                .ToListAsync();
            return View(tags);
        }

        // GET: Tags/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .Include(t => t.ProductTags)
                .ThenInclude(pt => pt.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        // GET: Tags/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tags/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,IsActive")] Tag tag)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra tag đã tồn tại chưa
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == tag.Name.ToLower());
                
                if (existingTag != null)
                {
                    ModelState.AddModelError("Name", "Tag này đã tồn tại!");
                    return View(tag);
                }
                
                tag.CreatedAt = DateTime.UtcNow;
                _context.Add(tag);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tag đã được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tags/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        // POST: Tags/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IsActive,CreatedAt")] Tag tag)
        {
            if (id != tag.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra tag đã tồn tại chưa (loại trừ tag hiện tại)
                    var existingTag = await _context.Tags
                        .FirstOrDefaultAsync(t => t.Name.ToLower() == tag.Name.ToLower() && t.Id != tag.Id);
                    
                    if (existingTag != null)
                    {
                        ModelState.AddModelError("Name", "Tag này đã tồn tại!");
                        return View(tag);
                    }
                    
                    _context.Update(tag);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tag đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TagExists(tag.Id))
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
            return View(tag);
        }

        // GET: Tags/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .Include(t => t.ProductTags)
                .ThenInclude(pt => pt.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        // POST: Tags/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tag = await _context.Tags
                .Include(t => t.ProductTags)
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (tag != null)
            {
                if (tag.ProductTags.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa tag này vì còn có sản phẩm đang sử dụng!";
                    return RedirectToAction(nameof(Index));
                }
                
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tag đã được xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TagExists(int id)
        {
            return _context.Tags.Any(e => e.Id == id);
        }
    }
}
