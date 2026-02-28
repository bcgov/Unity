using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Unity.Flex.Web.Views.Shared.Components.TextAreaWidget;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    [Collection(ComponentTestCollection.Name)]
    public class TextAreaWidgetTests
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public TextAreaWidgetTests(ComponentTestFixture fixture)
        {
            lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task TextAreaWidgetReturnsCorrectType()
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

            var viewComponent = new TextAreaWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            // Act
            var result = await viewComponent.InvokeAsync(new WorksheetFieldViewModel()
            {
                Type = Worksheets.CustomFieldType.TextArea
            }
            , string.Empty) as ViewViewComponentResult;

            TextAreaViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as TextAreaViewModel;

            // Assert
            resultModel!.Field?.Type.ShouldBe(Worksheets.CustomFieldType.TextArea);
        }
    }
}
