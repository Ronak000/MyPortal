using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyPortal.Models;

namespace MyPortal.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Customer_Home()
    {
        return View();
    }
    public IActionResult Change_Password()
    {
        return View();
    }
}
