using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Controllers
{
    [Route("/")]
    [ApiController]
    public class OtherRouteController : ControllerBase
    {
        IWebHostEnvironment _env = null;
        public OtherRouteController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
        }

        [HttpGet]
        [Route("favicon.png")]
        public IActionResult Favicon_png()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "klc_favicon.png");
            return PhysicalFile(filePath, "image/png");
        }

        [HttpGet]
        [Route("favicon.ico")]
        public IActionResult Favicon_ico()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "klc_favicon.ico");
            return PhysicalFile(filePath, "image/x-icon");
        }
    }
}