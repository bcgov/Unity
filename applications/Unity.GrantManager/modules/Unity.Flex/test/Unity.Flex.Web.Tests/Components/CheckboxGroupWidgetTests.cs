using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Unity.Flex.Web.Views.Shared.Components.CheckboxGroupWidget;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    [Collection(ComponentTestCollection.Name)]
    public class CheckboxGroupWidgetTests
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public CheckboxGroupWidgetTests(ComponentTestFixture fixture)
        {
            lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task CheckboxGroupWidgetReturnsCorrectType()
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

            var viewComponent = new CheckboxGroupWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            // Act
            var result = await viewComponent.InvokeAsync(new WorksheetFieldViewModel()
            {
                Type = Worksheets.CustomFieldType.CheckboxGroup
            }
            , string.Empty) as ViewViewComponentResult;

            CheckboxGroupViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as CheckboxGroupViewModel;

            // Assert
            resultModel!.Field?.Type.ShouldBe(Worksheets.CustomFieldType.CheckboxGroup);
        }
    }
}
