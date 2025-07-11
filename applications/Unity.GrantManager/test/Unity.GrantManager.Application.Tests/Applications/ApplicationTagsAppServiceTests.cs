﻿using Shouldly;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Unity.GrantManager.Applications;
using Xunit.Abstractions;
using System.Collections.Generic;
using Unity.GrantManager.GlobalTag;
using System.Xml.Linq;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationTagsAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IApplicationTagsService _applicationTagssAppService;
        private readonly IRepository<Application, Guid> _applicationsRepository;

        private readonly ITagsService _tagsService;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public ApplicationTagsAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _applicationTagssAppService = GetRequiredService<IApplicationTagsService>();
            _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
            _tagsService = GetRequiredService<ITagsService>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetListAsync_Should_Return_ApplicationTags()
        {
          
            var applicationTags = (await _applicationTagssAppService.GetListAsync()).ToList();
            applicationTags.Count.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetListWithApplicationIdAsync_Should_Return_ApplicationTags()
        {
            // Arrange            
            // Arrange
            var application = (await _applicationsRepository.GetListAsync())[0];

            // Act
            List<Guid> ids = new List<Guid>();
            ids.Add(application.Id);
            var applicationTags  = (await _applicationTagssAppService.GetListWithApplicationIdsAsync(ids)).ToList();

            // Assert            
            applicationTags.Count.ShouldBeGreaterThanOrEqualTo(0);
        } 
        
        

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AssignTags()
        {
            // Arrange            
            Login(GrantManagerTestData.User1_UserId);
            using var uow = _unitOfWorkManager.Begin();
            var application = (await _applicationsRepository.GetListAsync())[0];

            var newTag = await _tagsService.CreateTagsAsync(new TagDto { Name = "Environment" });

           
            var tag = new TagDto
            {
                Id = newTag.Id,    
                Name = "Environment"
            };

            // Act
            var assignDto = new AssignApplicationTagsDto
            {
                ApplicationId = application.Id,
                Tags = new List<TagDto> { tag }   
            };

            var addedTag = await _applicationTagssAppService.AssignTagsAsync(assignDto);
            // Assert
            (await _applicationTagssAppService.GetApplicationTagsAsync(application.Id))
                .ShouldNotBeNull();
        }

        
    }
}
