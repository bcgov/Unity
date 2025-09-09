using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Shouldly;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.CurrencyDefinitionWidget;
using Unity.Flex.Worksheets.Definitions;
using Unity.GrantManager;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    public class CurrencyDefinitionWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public CurrencyDefinitionWidgetTests()
        {
            // Remove EventLog logger to avoid ObjectDisposedException in test environment
            var loggerFactory = GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            foreach (var provider in loggerFactory
                .GetType()
                .GetField("_providers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(loggerFactory) as System.Collections.IEnumerable ?? new object[0])
            {
                if (provider.GetType().Name.Contains("EventLogLoggerProvider"))
                {
                    (provider as IDisposable)?.Dispose();
                }
            }
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task CurrencyDefinitionWidgetReturnsCorrectLimits()
        {
            // Arrange
            var viewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new CurrencyDefinitionWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            // Act
            var result = await viewComponent.InvokeAsync(JsonSerializer.Serialize(new CurrencyDefinition())) as ViewViewComponentResult;
            CurrencyDefinitionViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as CurrencyDefinitionViewModel;

            // Assert
            resultModel!.Min.ToString().ShouldBe(decimal.MinValue.ToString());
            resultModel!.Max.ToString().ShouldBe(decimal.MaxValue.ToString());
        }
    }
}
