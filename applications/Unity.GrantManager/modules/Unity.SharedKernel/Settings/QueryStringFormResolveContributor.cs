using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Modules.Shared.Settings;

/// <summary>
/// Resolves form ID from the query string parameters.
/// </summary>
public class QueryStringFormResolveContributor : HttpFormResolveContributorBase
{
    public const string ContributorName = "QueryString";
    public override string Name => ContributorName;

    protected override Task<string?> GetFormIdFromHttpContextOrNullAsync(IFormResolveContext context, HttpContext httpContext)
    {
        if (httpContext.Request.QueryString.HasValue)
        {
            var formIdKey = context.GetFormResolutionOptions().FormIdKey;
            if (httpContext.Request.Query.ContainsKey(formIdKey))
            {
                var formIdValue = httpContext.Request.Query[formIdKey].ToString();
                if (string.IsNullOrWhiteSpace(formIdValue))
                {
                    context.Handled = true;
                    return Task.FromResult<string?>(null);
                }

                return Task.FromResult(formIdValue)!;
            }
        }

        return Task.FromResult<string?>(null);
    }
}
