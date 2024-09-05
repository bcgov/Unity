using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Shouldly;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.TextDefinitionWidget;
using Unity.Flex.Worksheets.Definitions;
using Unity.GrantManager;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    public class TextDefinitionWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public TextDefinitionWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task TextDefinitionWidgetReturnsCorrectLimits()
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

            var viewComponent = new TextDefinitionWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            // Act
            var result = await viewComponent.InvokeAsync(JsonSerializer.Serialize(new TextDefinition())) as ViewViewComponentResult;
            TextDefinitionViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as TextDefinitionViewModel;

            // Assert
            resultModel!.MinLength.ToString().ShouldBe(uint.MinValue.ToString());
            resultModel!.MaxLength.ToString().ShouldBe(uint.MaxValue.ToString());
        }
    }
}