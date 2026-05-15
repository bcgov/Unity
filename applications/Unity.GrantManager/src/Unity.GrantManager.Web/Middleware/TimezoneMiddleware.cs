using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Unity.GrantManager.Web.Middleware;

public class TimezoneMiddleware(RequestDelegate next)
{
    private const string CookieName = "timezoneoffset";

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldIntercept(context))
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("""
                <!DOCTYPE html>
                <html>
                <head><title></title></head>
                <body>
                <script src="/js/TimezoneUtils.js"></script>
                <script src="/js/TimezoneBootstrap.js"></script>
                </body>
                </html>
                """);
            return;
        }

        await next(context);
    }

    private static bool ShouldIntercept(HttpContext context)
    {
        var request = context.Request;

        return request.Method == HttpMethods.Get
            && !request.Cookies.ContainsKey(CookieName)
            && !request.Path.StartsWithSegments("/api")
            && !request.Headers.ContainsKey("X-Requested-With")
            && request.Headers.Accept.ToString().Contains("text/html");
    }
}
