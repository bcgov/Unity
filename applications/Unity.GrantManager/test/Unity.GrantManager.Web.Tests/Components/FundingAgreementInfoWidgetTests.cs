﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.FundingAgreementInfo;
using Unity.GrantManager.Locality;
using Volo.Abp.DependencyInjection;
using Xunit;
using Microsoft.AspNetCore.Authorization;

namespace Unity.GrantManager.Components
{
    public class FundingAgreementInfoWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public FundingAgreementInfoWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();                
        }

        [Fact]
        public async Task ContactInfoReturnsStatus()
        {
            DateTime executionDateVal = DateTime.UtcNow;
            var applicationDto = new GrantApplicationDto()
            {
                ContractNumber = "123456789",
                ContractExecutionDate = executionDateVal,
            };

            // Arrange
            var appService = Substitute.For<IGrantApplicationAppService>();
            appService.GetAsync(Arg.Any<Guid>()).Returns(applicationDto);
            var economicRegionService = Substitute.For<IEconomicRegionService>();
            var electoralDistrictService = Substitute.For<IElectoralDistrictService>();
            var regionalDistrictService = Substitute.For<IRegionalDistrictService>();
            var communitiesService = Substitute.For<ICommunityService>();
            var authorizationService = GetRequiredService<IAuthorizationService>();
            

            var viewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new FundingAgreementInfoViewComponent(appService, economicRegionService, electoralDistrictService, regionalDistrictService, communitiesService, authorizationService)
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            //Act
            var result = await viewComponent.InvokeAsync(Guid.NewGuid(), Guid.NewGuid()) as ViewViewComponentResult;
            FundingAgreementInfoViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as FundingAgreementInfoViewModel;

            //Assert

            var expectedContractNumber = "123456789";
            var expectedContractExecutionDate = executionDateVal;


            resultModel!.FundingAgreementInfo!.ContractNumber.ShouldBe(expectedContractNumber);
            resultModel!.FundingAgreementInfo!.ContractExecutionDate.ShouldBe(expectedContractExecutionDate);

        }
    }
}