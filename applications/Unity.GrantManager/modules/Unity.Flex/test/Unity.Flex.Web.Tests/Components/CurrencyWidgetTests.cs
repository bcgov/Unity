using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Unity.Flex.Web.Views.Shared.Components.CurrencyWidget;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Web.Tests.Components
{
    [Collection(ComponentTestCollection.Name)]
    public class CurrencyWidgetTests
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public CurrencyWidgetTests(ComponentTestFixture fixture)
        {
            lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task CurrencyWidgetReturnsCorrectType()
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

            var viewComponent = new CurrencyWidget()
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            // Act
            var result = await viewComponent.InvokeAsync(new WorksheetFieldViewModel() 
            {
                Type = Worksheets.CustomFieldType.Currency
            }
            , string.Empty) as ViewViewComponentResult;

            CurrencyViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as CurrencyViewModel;

            // Assert
            resultModel!.Field?.Type.ShouldBe(Worksheets.CustomFieldType.Currency);            
        }
    }
}
