using AutofacSample.Domain;
using Microsoft.AspNet.Mvc;

namespace Web2.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(IOrder order)
        {
            // Sample to indicate order was hydrated from IoC container.
            var name = order.Name;
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}