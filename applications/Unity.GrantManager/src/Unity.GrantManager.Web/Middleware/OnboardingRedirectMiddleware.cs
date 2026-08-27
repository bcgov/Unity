using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Unity.Modules.Shared.Specializations;

namespace Unity.GrantManager.Web.Middleware;

public class OnboardingRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsGrantApplicationsList(context))
        {
            var checker = context.RequestServices.GetService<ISpecializationChecker>();
            if (checker != null && await checker.IsEnabledAsync(SpecializationConsts.Onboarding))
            {
                context.Response.Redirect("/TenantManagement/Onboarding");
                return;
            }
        }

        await next(context);
    }

    private static bool IsGrantApplicationsList(HttpContext context)
    {
        var path = context.Request.Path;
        return context.Request.Method == HttpMethods.Get
            && (path.Equals("/GrantApplications", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/GrantApplications/", StringComparison.OrdinalIgnoreCase));
    }
}
