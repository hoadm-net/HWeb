using Microsoft.AspNetCore.Mvc;

namespace HWeb.Controllers
{
    public class PoliciesController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Chính sách";
            return View();
        }
    }
}
