using Microsoft.AspNetCore.Mvc;

namespace elasticsearch_netcore.Controllers
{
    [Route("/")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet("/")]
        public IActionResult Welcome()
        {
            return Content("<h1 style=\"text-align: center\">Welcome !!!</h1>", "text/html");
        }
    }
}
