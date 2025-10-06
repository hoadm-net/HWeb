using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HWeb.Data;
using HWeb.Models;
using HWeb.Services;

namespace HWeb;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // Add Entity Framework
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Add Identity services
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings cho demo
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 3;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Configure application cookie
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        // Register DataSeeder
        builder.Services.AddScoped<DataSeeder>();

        var app = builder.Build();

        // Seed database
        using (var scope = app.Services.CreateScope())
        {
            var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await dataSeeder.SeedAsync();
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        // Add Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}