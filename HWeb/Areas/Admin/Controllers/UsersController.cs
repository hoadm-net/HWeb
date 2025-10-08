using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;

namespace HWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(string? search = null, string? role = null, int page = 1)
        {
            const int pageSize = 10;
            
            var query = _userManager.Users.AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => 
                    u.UserName.Contains(search) ||
                    u.Email.Contains(search) ||
                    (u.FirstName != null && u.FirstName.Contains(search)) ||
                    (u.LastName != null && u.LastName.Contains(search)));
            }

            var totalUsers = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get user roles
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            // Filter by role if specified
            if (!string.IsNullOrEmpty(role) && role != "All")
            {
                var filteredUsers = new List<ApplicationUser>();
                foreach (var user in users)
                {
                    if (userRoles[user.Id].Contains(role))
                    {
                        filteredUsers.Add(user);
                    }
                }
                users = filteredUsers;
            }

            // Calculate locked users count for the view
            var currentTime = DateTimeOffset.UtcNow;
            
            // Fix LINQ translation issue by bringing users with lockout data into memory first
            var usersWithLockout = await _userManager.Users
                .Where(u => u.LockoutEnd != null)
                .Select(u => u.LockoutEnd)
                .ToListAsync();
            var lockedUsersCount = usersWithLockout.Count(lockoutEnd => lockoutEnd > currentTime);

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentRole"] = role;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalUsers / pageSize);
            ViewData["TotalUsers"] = totalUsers;
            ViewData["LockedUsersCount"] = lockedUsersCount;
            ViewData["UserRoles"] = userRoles;
            ViewData["AllRoles"] = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(users);
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userOrders = await _context.Orders
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            var userReviews = await _context.Reviews
                .Include(r => r.Product)
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.UserRoles = userRoles;
            ViewBag.UserOrders = userOrders;
            ViewBag.UserReviews = userReviews;
            ViewBag.TotalOrders = await _context.Orders.CountAsync(o => o.UserId == id);
            ViewBag.TotalReviews = await _context.Reviews.CountAsync(r => r.UserId == id);

            return View(user);
        }

        // POST: Admin/Users/ToggleLockout
        [HttpPost]
        public async Task<IActionResult> ToggleLockout(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                
                if (isLockedOut)
                {
                    // Unlock user
                    var result = await _userManager.SetLockoutEndDateAsync(user, null);
                    if (result.Succeeded)
                    {
                        return Json(new { success = true, message = "Đã mở khóa tài khoản.", isLocked = false });
                    }
                }
                else
                {
                    // Lock user for 1 year
                    var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(1));
                    if (result.Succeeded)
                    {
                        return Json(new { success = true, message = "Đã khóa tài khoản.", isLocked = true });
                    }
                }

                return Json(new { success = false, message = "Không thể thực hiện thao tác." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // POST: Admin/Users/AssignRole
        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    return Json(new { success = false, message = "Role không tồn tại." });
                }

                var isInRole = await _userManager.IsInRoleAsync(user, roleName);
                if (isInRole)
                {
                    return Json(new { success = false, message = "Người dùng đã có role này." });
                }

                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = $"Đã thêm role '{roleName}' cho người dùng." });
                }

                return Json(new { success = false, message = "Không thể thêm role." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // POST: Admin/Users/RemoveRole
        [HttpPost]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                var isInRole = await _userManager.IsInRoleAsync(user, roleName);
                if (!isInRole)
                {
                    return Json(new { success = false, message = "Người dùng không có role này." });
                }

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = $"Đã xóa role '{roleName}' của người dùng." });
                }

                return Json(new { success = false, message = "Không thể xóa role." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // GET: Admin/Users/Statistics
        public async Task<IActionResult> Statistics()
        {
            var currentTime = DateTimeOffset.UtcNow;
            
            var totalUsers = await _userManager.Users.CountAsync();
            var newUsersThisMonth = await _userManager.Users
                .CountAsync(u => u.CreatedAt.Month == DateTime.Now.Month && u.CreatedAt.Year == DateTime.Now.Year);
            var newUsersToday = await _userManager.Users
                .CountAsync(u => u.CreatedAt.Date == DateTime.Today);
                
            // Fix LINQ translation issue by bringing users with lockout data into memory first
            var usersWithLockout = await _userManager.Users
                .Where(u => u.LockoutEnd != null)
                .Select(u => u.LockoutEnd)
                .ToListAsync();
            var lockedUsers = usersWithLockout.Count(lockoutEnd => lockoutEnd > currentTime);

            // User roles statistics
            var roleStats = new Dictionary<string, int>();
            var roles = await _roleManager.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                roleStats[role.Name!] = usersInRole.Count;
            }

            // Recent registrations
            var recentUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.NewUsersThisMonth = newUsersThisMonth;
            ViewBag.NewUsersToday = newUsersToday;
            ViewBag.LockedUsers = lockedUsers;
            ViewBag.RoleStats = roleStats;
            ViewBag.RecentUsers = recentUsers;

            return View();
        }
    }
}
