using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.DataGridDefinitionWidget;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    [Collection(ComponentTestCollection.Name)]
    public class DataGridDefinitionWidgetTests
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public DataGridDefinitionWidgetTests(ComponentTestFixture fixture)
        {
            lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
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
