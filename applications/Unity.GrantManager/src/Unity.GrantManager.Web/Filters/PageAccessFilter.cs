using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Unity.GrantManager.Web.Menus;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Filters;

// Page filters run only for Razor Pages, leaving API endpoint authorization unchanged.
public class PageAccessFilter(UserNavigationService navigation, ICurrentUser currentUser) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        var pagePath = context.ActionDescriptor.ViewEnginePath;
        if (!currentUser.IsAuthenticated || IsAccessPage(pagePath))
        {
            await next();
            return;
        }

        if (await navigation.GetLandingPageAsync() == null
            || !await navigation.CanAccessPageAsync(pagePath))
        {
            context.Result = new LocalRedirectResult(UserNavigationService.NoAccessUrl);
            return;
        }

        await next();
    }

    private static bool IsAccessPage(string pagePath)
        => pagePath.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase)
            || pagePath.Equals("/GrantPrograms/Index", StringComparison.OrdinalIgnoreCase)
            || pagePath.Equals("/Error", StringComparison.OrdinalIgnoreCase);
}
