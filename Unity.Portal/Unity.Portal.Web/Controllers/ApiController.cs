using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http;


// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Unity.Portal.Web.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class ApiController : ControllerBase
    {

        private readonly HttpClient _httpClient;

        public ApiController(HttpClient httpClient)
        {
         
            // Uncomment once valid certs
            //_httpClient = httpClient;


            // This deals with insecure certificate errors 
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };

            _httpClient = new HttpClient(handler);

        }

        [HttpPost]
        public async Task<ActionResult> PostAsync([FromBody] object request) {
            Console.WriteLine(request.ToString());
            var url = new Uri("https://localhost:7102/UnityChefs", UriKind.Absolute);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", "89abddfb-2cff-4fda-83e6-13221f0c3d4f");
            var response = await _httpClient.PostAsJsonAsync(url, request);

            const string Value = "success";
            return base.Ok(Value);
        }
    }
}

