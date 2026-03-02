using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.CheckboxGroupDefinitionWidget;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    [Collection(ComponentTestCollection.Name)]
    public class CheckboxGroupDefinitionWidgetTests
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public CheckboxGroupDefinitionWidgetTests(ComponentTestFixture fixture)
        {
            lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task CheckboxGroupDefinitionWidgetReturnsCorrectOptionsCount()
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

            var viewComponent = new CheckboxGroupDefinitionWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            var definition = JsonSerializer.Serialize(new CheckboxGroupDefinition()
            {
                Options =
                [
                    new CheckboxGroupDefinitionOption() { Key = "Key1", Label = "Label1", Value = true }
                ]                
            });            

            // Act
            var result = await viewComponent.InvokeAsync(definition) as ViewViewComponentResult;
            CheckboxGroupDefinitionViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as CheckboxGroupDefinitionViewModel;

            // Assert
            resultModel!.CheckboxOptions.Count.ShouldBe(1);
        }
    }
}
