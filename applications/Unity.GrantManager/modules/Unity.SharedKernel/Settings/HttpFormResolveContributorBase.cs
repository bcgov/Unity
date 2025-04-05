using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Unity.Modules.Shared.Settings;

// Abstract base class for HTTP-based form ID resolvers
public abstract class HttpFormResolveContributorBase : IFormResolveContributor
{
    public abstract string Name { get; }
    public virtual Task ResolveAsync(IFormResolveContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        return ResolveFromHttpContextAsync(context, httpContext);
    }

    protected virtual async Task ResolveFromHttpContextAsync(IFormResolveContext context, HttpContext httpContext)
    {
        var formId = await GetFormIdFromHttpContextOrNullAsync(context, httpContext);
        if (formId != null)
        {
            context.FormId = formId;
            context.Handled = true;
        }
    }

    protected abstract Task<string?> GetFormIdFromHttpContextOrNullAsync(IFormResolveContext context, HttpContext httpContext);
}

// Interface for the form resolution context
public interface IFormResolveContext
{
    string? FormId { get; set; }
    bool Handled { get; set; }
    IServiceProvider ServiceProvider { get; }
}

// Interface for form resolve contributors
public interface IFormResolveContributor
{
    string Name { get; }
    Task ResolveAsync(IFormResolveContext context);
}

// Extension method to get HttpContext from form resolve context
public static class FormResolveContextExtensions
{
    public static HttpContext? GetHttpContext(this IFormResolveContext context)
    {
        return context.ServiceProvider.GetService(typeof(IHttpContextAccessor)) is IHttpContextAccessor httpContextAccessor
            ? httpContextAccessor.HttpContext
            : null;
    }

    public static FormResolutionOptions GetFormResolutionOptions(this IFormResolveContext context)
    {
        return context.ServiceProvider.GetRequiredService<FormResolutionOptions>();
    }
}

// Options class for form resolution
public class FormResolutionOptions
{
    public string FormIdKey { get; set; } = "formId";
    public string ApplicationIdKey { get; set; } = "applicationId";
}
