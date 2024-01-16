using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Volo.Abp.ExceptionHandling;

namespace Unity.GrantManager.Web.Exceptions.Handlers
{
    public class CustomExceptionSubscriber : ExceptionSubscriber
    {
        readonly IHttpContextAccessor _httpContextAccessor;

        public CustomExceptionSubscriber(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task HandleAsync(ExceptionNotificationContext context)
        {
            // do your stuff.

            _httpContextAccessor.HttpContext.Response.Redirect("/account/login");
        }
    }
}
