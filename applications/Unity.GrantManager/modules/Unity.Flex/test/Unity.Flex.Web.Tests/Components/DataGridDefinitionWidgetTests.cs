using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Shouldly;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.DataGridDefinitionWidget;
using Unity.Flex.Worksheets.Definitions;
using Unity.GrantManager;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    public class DataGridDefinitionWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public DataGridDefinitionWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task DataGridDefinitionWidgetReturnsCorrectLimits()
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

            var viewComponent = new DataGridDefinitionWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            // Act
            var result = await viewComponent.InvokeAsync(JsonSerializer.Serialize(new DataGridDefinition())) as ViewViewComponentResult;
            DataGridDefinitionViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as DataGridDefinitionViewModel;

            // Assert
            resultModel!.Dynamic.ToString().ShouldBe(bool.FalseString);
        }
    }
}
