using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Unity.Flex.Web.Views.Shared.Components.CheckboxWidget;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    [Collection(ComponentTestCollection.Name)]
    public class CheckboxWidgetTests
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public CheckboxWidgetTests(ComponentTestFixture fixture)
        {
            lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task CheckboxWidgetReturnsCorrectType()
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

            var viewComponent = new CheckboxWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            // Act
            var result = await viewComponent.InvokeAsync(new WorksheetFieldViewModel()
            {
                Type = Worksheets.CustomFieldType.Checkbox
            }
            , string.Empty) as ViewViewComponentResult;

            CheckboxViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as CheckboxViewModel;

            // Assert
            resultModel!.Field?.Type.ShouldBe(Worksheets.CustomFieldType.Checkbox);
        }
    }
}
