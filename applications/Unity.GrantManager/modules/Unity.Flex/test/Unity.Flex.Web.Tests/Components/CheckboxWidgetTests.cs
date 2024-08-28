using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Shouldly;
using Unity.Flex.Web.Views.Shared.Components.CheckboxWidget;
using Unity.Flex.Web.Views.Shared.Components.CurrencyWidget;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.GrantManager;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    public class CheckboxWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public CheckboxWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
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
