using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using graph_office.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System.Diagnostics;
using System.Threading.Tasks;
using graph_office.Graph;
using graph_office.Alerts;

namespace graph_office.Controllers
{
    public class HomeController : Controller
    {
        
        private readonly ILogger<HomeController> _logger;


        // dependency injection
        public HomeController(            
            ILogger<HomeController> logger)
        {            
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult ErrorWithMessage(string message, string debug)
        {
            return View("Index").WithError(message, debug);
        }
    }
    
}
