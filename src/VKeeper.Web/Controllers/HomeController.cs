using Microsoft.AspNetCore.Mvc;

namespace VKeeper.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Notes");
        }

        return View();
    }
}
