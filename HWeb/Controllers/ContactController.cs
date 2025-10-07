using Microsoft.AspNetCore.Mvc;

namespace HWeb.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Liên hệ";
            return View();
        }
    }
}
