using Microsoft.AspNetCore.Identity;
using HWeb.Models;

namespace HWeb.Services
{
    public class DataSeeder
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DataSeeder(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            // Tạo role Admin nếu chưa có
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Tạo tài khoản Admin nếu chưa có
            var adminEmail = "admin@demo.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };
                
                var result = await _userManager.CreateAsync(adminUser, "123");
                
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
