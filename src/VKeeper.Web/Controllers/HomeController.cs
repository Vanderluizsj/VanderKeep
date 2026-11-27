using Microsoft.AspNetCore.Mvc;

namespace VKeeper.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
