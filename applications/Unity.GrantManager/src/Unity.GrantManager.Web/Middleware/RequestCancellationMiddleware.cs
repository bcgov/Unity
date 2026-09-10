using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Unity.GrantManager.Web.Middleware;

public class RequestCancellationMiddleware
{
    private readonly RequestDelegate _next;

    public RequestCancellationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            return;
        }
    }
}