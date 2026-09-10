using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Unity.AI.Attachments;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications.Automation;
using Unity.GrantManager.Intakes;
using Xunit;

namespace Unity.GrantManager.AI;

public class AttachmentProviderCompositionTests
{
    [Fact]
    public void GrantManager_Adapters_Should_Implement_AI_Attachment_Contracts()
    {
        typeof(ChefsFileAttachmentStreamProvider)
            .GetInterfaces()
            .ShouldContain(typeof(IAttachmentContentProvider));
        typeof(AttachmentSummaryDataProvider)
            .GetInterfaces()
            .ShouldContain(typeof(IAttachmentSummaryDataProvider));
    }

    [Fact]
    public void AI_Attachment_Contracts_Should_Map_To_GrantManager_Adapters()
    {
        var services = new ServiceCollection()
            .AddTransient<IAttachmentContentProvider, ChefsFileAttachmentStreamProvider>()
            .AddTransient<IAttachmentSummaryDataProvider, AttachmentSummaryDataProvider>();

        services.ShouldContain(x =>
            x.ServiceType == typeof(IAttachmentContentProvider) &&
            x.ImplementationType == typeof(ChefsFileAttachmentStreamProvider));
        services.ShouldContain(x =>
            x.ServiceType == typeof(IAttachmentSummaryDataProvider) &&
            x.ImplementationType == typeof(AttachmentSummaryDataProvider));
    }
}
