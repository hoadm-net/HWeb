using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            ViewBag.Message = "Chào mừng Admin đến với Dashboard!";
            return View();
        }
    }
}
