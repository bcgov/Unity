using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Web.Extensions
{
    public static class LazyViewComponentInvoker
    {
        private static readonly HashSet<string> LazyComponents = new()
        {
            "SummaryWidget",
            "AssessmentResults",
            "ProjectInfo",
            "ApplicantInfo",
            "FundingAgreementInfo",
            "PaymentInfo",
            "ReviewList",
            "HistoryWidget",
            "ApplicationAttachments"
        };

       public static async Task<IHtmlContent> InvokeAsyncWithSkeleton(
            this IViewComponentHelper componentHelper,
            string componentName,
            object arguments,
            bool isLazyLoad = true,
            string tabName = "")
        {
            // Check if this component should be lazy loaded
            if (isLazyLoad && LazyComponents.Contains(componentName))
            {
                var skeletonHtml = GenerateSkeletonHtml(componentName, arguments, tabName);
                System.Diagnostics.Debug.WriteLine($"Generated skeleton for {componentName}: {skeletonHtml.Substring(0, Math.Min(100, skeletonHtml.Length))}...");
                return new HtmlString(skeletonHtml);
            }

            // Normal component invocation - ADD THIS LINE
            return await componentHelper.InvokeAsync(componentName, arguments);
        }

        private static string GenerateSkeletonHtml(string componentName, object arguments, string tabName)
        {
            // var componentId = $"{componentName}_{Guid.NewGuid():N}";
            var loadUrl = $"/GrantApplications/DetailsV2?handler=LoadComponent&component={componentName}&tab={tabName}";
            
            // Create query string from arguments
            var queryParams = BuildQueryString(arguments);
            if (!string.IsNullOrEmpty(queryParams))
            {
                loadUrl += "&" + queryParams;
            }

            return $@"
                <div class='lazy-component-container' data-component='{componentName}' data-load-url='{loadUrl}' data-tab='{tabName}'>
                    <div class='skeleton-loader skeleton-generic'>
                        <div class='skeleton-card'>
                            <div class='skeleton-header'></div>
                            <div class='skeleton-line'></div>
                            <div class='skeleton-line short'></div>
                            <div class='skeleton-line'></div>
                            <div class='skeleton-line'></div>
                            <div class='skeleton-line short'></div>
                            <div class='skeleton-line'></div>
                        </div>
                    </div>
                    <div class='component-content' style='display: none;'></div>
                </div>";
        }

        private static string BuildQueryString(object arguments)
        {
            if (arguments == null) return string.Empty;
            
            var props = arguments.GetType().GetProperties();
            var queryParts = new List<string>();
            
            foreach (var prop in props)
            {
                var value = prop.GetValue(arguments);
                if (value != null)
                {
                    queryParts.Add($"{prop.Name}={Uri.EscapeDataString(value.ToString())}");
                }
            }
            
            return string.Join("&", queryParts);
        }
    }
}