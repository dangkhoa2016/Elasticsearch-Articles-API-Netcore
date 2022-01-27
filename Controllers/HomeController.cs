using System.Net.Mime;
using DynamicExpresso;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using Nest;

namespace elasticsearch_netcore.Controllers
{
    [Route("/")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        static Interpreter interpreter = new Interpreter().EnableReflection();

        readonly ILogger<HomeController> _logger;
        readonly IElasticClient _elasticClient;
        readonly Helpers.Helper _helper;
        public HomeController(ILogger<HomeController> logger, IElasticClient elasticClient, Helpers.Helper helper)
        {
            _logger = logger;
            _elasticClient = elasticClient;
            _helper = helper;
            if (!interpreter.Identifiers.Any(val => val.Name == "helper"))
                interpreter.SetVariable("helper", _helper);
            if (!interpreter.Identifiers.Any(val => val.Name == "client"))
                interpreter.SetVariable("client", _elasticClient);
            // if (!interpreter.Identifiers.Any(val => val.Name == "lowLevelClient"))
            //     interpreter.SetVariable("lowLevelClient", _elasticClient.LowLevel);
        }

        [HttpGet("/")]
        public IActionResult Welcome()
        {
            return Content("<h1 style=\"text-align: center\">Welcome !!!</h1>", "text/html");
        }

        static bool IsPropertyExist(dynamic obj, string name)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);

            return obj.GetType().GetProperty(name) != null;
        }

        [HttpPost("eval")]
        public IActionResult Eval([FromForm] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new JsonResult(new { error = "Please provide code." });

            // client.RootNodeInfo();
            // client.Cat.Nodes();
            //client.Indices.Get(Indices.Index("restaurants"));

            Func<dynamic, ContentResult> response = (value) =>
            {
                var responseData = Convert.ToString(value);
                _logger.LogInformation((string)responseData);
                return Content(responseData, MediaTypeNames.Text.Plain);
            };

            dynamic result = interpreter.Parse(content).Invoke();
            if (IsPropertyExist(result, "ApiCall"))
            {
                var z = result.ApiCall;

                _logger.LogInformation((string)result.DebugInformation);
                return Content(System.Text.Encoding.UTF8.GetString(z.ResponseBodyInBytes), MediaTypeNames.Application.Json);
            }
            else if (IsPropertyExist(result, "Result"))
                return response(result.Result);

            return response(result);
        }
    }
}