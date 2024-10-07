using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Shouldly;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.TextAreaDefinitionWidget;
using Unity.Flex.Worksheets.Definitions;
using Unity.GrantManager;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    public class TextAreaDefinitionWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public TextAreaDefinitionWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task TextAreaDefinitionWidgetReturnsCorrectRowsCount()
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

            var viewComponent = new TextAreaDefinitionWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            var definition = JsonSerializer.Serialize(new TextAreaDefinition()
            {
                Rows = 1
            });

            // Act
            var result = await viewComponent.InvokeAsync(definition) as ViewViewComponentResult;
            TextAreaDefinitionViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as TextAreaDefinitionViewModel;

            // Assert
            resultModel!.Rows.ToString().ShouldBe("1");
        }
    }
}
