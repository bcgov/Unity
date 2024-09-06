using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Shouldly;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.SelectListDefinitionWidget;
using Unity.Flex.Worksheets.Definitions;
using Unity.GrantManager;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    public class SelectListDefinitionWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public SelectListDefinitionWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task SelectListDefinitionWidgetReturnsCorrectOptionsCount()
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

            var viewComponent = new SelectListDefinitionWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            var definition = JsonSerializer.Serialize(new SelectListDefinition()
            {
                Options =
                [
                    new SelectListOption() { Key = "Key1", Value = "Label1" }
                ]
            });

            // Act
            var result = await viewComponent.InvokeAsync(definition) as ViewViewComponentResult;
            SelectListDefinitionViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as SelectListDefinitionViewModel;

            // Assert
            resultModel!.Options.Count.ShouldBe(1);
        }
    }
}
