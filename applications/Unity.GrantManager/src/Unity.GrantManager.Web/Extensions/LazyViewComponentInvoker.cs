using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
            "ApplicationAttachments",
            "CustomTabWidgetViewComponent"
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

            // Normal component invocation if not lazy loaded
            return await componentHelper.InvokeAsync(componentName, arguments);
        }

        // Overload to accept Type for CustomTabWidgetViewComponent
        public static async Task<IHtmlContent> InvokeAsyncWithSkeleton(
            this IViewComponentHelper componentHelper,
            Type componentType,
            object arguments,
            bool isLazyLoad = true,
            string tabName = "")
        {
            var componentName = componentType.Name;

            // Check if this component should be lazy loaded
            if (isLazyLoad && LazyComponents.Contains(componentName))
            {
                var skeletonHtml = GenerateSkeletonHtml(componentName, arguments, tabName);
                System.Diagnostics.Debug.WriteLine($"Generated skeleton for {componentName}: {skeletonHtml.Substring(0, Math.Min(100, skeletonHtml.Length))}...");
                return new HtmlString(skeletonHtml);
            }

            // Normal component invocation if not lazy loaded
            return await componentHelper.InvokeAsync(componentType, arguments);
        }

        private static string GenerateSkeletonHtml(string componentName, object arguments, string tabName)
        {
            var handlerComponentName = componentName == "CustomTabWidgetViewComponent" 
                ? "CustomTabWidget"
                : componentName;
            var loadUrl = $"/GrantApplications/DetailsV2?handler=LoadComponent&component={handlerComponentName}&tab={tabName}";

            // Create query string from arguments
            var queryParams = BuildQueryString(arguments);
            if (!string.IsNullOrEmpty(queryParams))
            {
                loadUrl += "&" + queryParams;
            }

            var props = arguments?.GetType().GetProperties();
            if (props != null)
            {
                var instanceCorrelationId = props.FirstOrDefault(p => p.Name == "instanceCorrelationId")?.GetValue(arguments);
                var sheetCorrelationId = props.FirstOrDefault(p => p.Name == "sheetCorrelationId")?.GetValue(arguments);
                
                if (instanceCorrelationId != null)
                {
                    loadUrl += $"&applicationId={instanceCorrelationId}";
                }
                
                if (sheetCorrelationId != null)
                {
                    loadUrl += $"&applicationFormVersionId={sheetCorrelationId}";
                }
            }


            var skeletonContent = componentName == "CustomTabWidgetViewComponent"
                ? GenerateCustomTabSkeleton()
                : GenerateGenericSkeleton();

            return $@"
                <div class='lazy-component-container' data-component='{handlerComponentName}' data-load-url='{loadUrl}' data-tab='{tabName}'>
                    {skeletonContent}
                    <div class='component-content' style='display: none;'></div>
                </div>";
        }
        
        private static string GenerateGenericSkeleton()
        {
            const string skeletonHtml = @"
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
            </div>";
            return skeletonHtml;
        }

        private static string GenerateCustomTabSkeleton()
        {
            const string skeletonHtml = @"
                <div class='skeleton-loader skeleton-custom-tab'>
                    <div class='skeleton-card'>
                        <div class='skeleton-header-large'></div>
                        <div class='skeleton-form-group'>
                            <div class='skeleton-label'></div>
                            <div class='skeleton-input'></div>
                        </div>
                        <div class='skeleton-form-group'>
                            <div class='skeleton-label'></div>
                            <div class='skeleton-input'></div>
                        </div>
                        <div class='skeleton-form-group'>
                            <div class='skeleton-label short'></div>
                            <div class='skeleton-textarea'></div>
                        </div>
                        <div class='skeleton-button-group'>
                            <div class='skeleton-button'></div>
                            <div class='skeleton-button'></div>
                        </div>
                    </div>
                </div>"; 
            return skeletonHtml;
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
                    queryParts.Add($"{prop.Name}={Uri.EscapeDataString(value?.ToString() ?? string.Empty)}");
                }
            }

            return string.Join("&", queryParts);
        }
    }
}