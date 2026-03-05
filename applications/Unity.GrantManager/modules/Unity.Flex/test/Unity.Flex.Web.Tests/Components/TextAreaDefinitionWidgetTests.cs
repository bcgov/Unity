using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.TextAreaDefinitionWidget;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    [Collection(ComponentTestCollection.Name)]
    public class TextAreaDefinitionWidgetTests
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public TextAreaDefinitionWidgetTests(ComponentTestFixture fixture)
        {
            lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
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
