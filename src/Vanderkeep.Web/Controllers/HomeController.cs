using Microsoft.AspNetCore.Mvc;

namespace Vanderkeep.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
