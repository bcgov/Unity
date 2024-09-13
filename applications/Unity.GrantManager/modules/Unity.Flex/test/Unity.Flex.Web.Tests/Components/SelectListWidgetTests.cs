using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Shouldly;
using Unity.Flex.Web.Views.Shared.Components.SelectListWidget;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.GrantManager;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    public class SelectListWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public SelectListWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task SelectListWidgetReturnsCorrectType()
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

            var viewComponent = new SelectListWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            // Act
            var result = await viewComponent.InvokeAsync(new WorksheetFieldViewModel()
            {
                Type = Worksheets.CustomFieldType.SelectList
            }
            , string.Empty) as ViewViewComponentResult;

            SelectListViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as SelectListViewModel;

            // Assert
            resultModel!.Field?.Type.ShouldBe(Worksheets.CustomFieldType.SelectList);
        }
    }
}
