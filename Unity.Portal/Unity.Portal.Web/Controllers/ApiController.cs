using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using org.apache.zookeeper.data;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Unity.Portal.Web.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class ApiController : ControllerBase
    {
        [HttpPost]
        public ActionResult Post([FromBody] object res) {
            Console.WriteLine(res.ToString());
            const string Value = "success";
            return base.Ok(Value);
        }
       
    }
}

