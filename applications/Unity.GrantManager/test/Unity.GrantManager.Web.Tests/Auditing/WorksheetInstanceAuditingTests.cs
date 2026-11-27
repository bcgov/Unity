using System;
using Shouldly;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared.Auditing;
using Unity.Modules.Shared.Utils;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;
using Xunit;

namespace Unity.GrantManager.Auditing;

public class WorksheetInstanceAuditingTests : AbpIntegratedTest<WorksheetInstanceAuditingTestModule>
{
    [Theory]
    [InlineData(typeof(WorksheetInstance), false)]
    [InlineData(typeof(WorksheetInstance), true)]
    [InlineData(typeof(CustomFieldValue), false)]
    [InlineData(typeof(CustomFieldValue), true)]
    public void Instance_EntityHistory_Should_Be_Disabled_Even_When_All_Entities_Are_Selected(
        Type entityType, bool backgroundJob)
    {
        var helper = GetRequiredService<IAuditingHelper>();
        using var jobContext = backgroundJob ? BackgroundJobExecutionContext.Use() : null;

        helper.IsEntityHistoryEnabled(entityType).ShouldBeFalse();
    }

    [Fact]
    public void Background_EntityHistory_Should_Still_Be_Enabled_For_Other_Entities()
    {
        var helper = GetRequiredService<IAuditingHelper>();
        using var jobContext = BackgroundJobExecutionContext.Use();

        helper.IsEntityHistoryEnabled(typeof(Application)).ShouldBeTrue();
    }
}

[DependsOn(typeof(AbpAutofacModule), typeof(UnityAuditingOverrideModule))]
public class WorksheetInstanceAuditingTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.EntityHistorySelectors.Add(new NamedTypeSelector("Test.AllEntities", _ => true));
        });
    }
}
