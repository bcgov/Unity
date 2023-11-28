using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace Unity.GrantManager.EntityFrameworkCore;

public static class GrantManagerEfCoreEntityExtensionMappings
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        GrantManagerGlobalFeatureConfigurator.Configure();
        GrantManagerModuleExtensionConfigurator.Configure();

        OneTimeRunner.Run(() =>
        {
            ObjectExtensionManager.Instance
                .MapEfCoreProperty<IdentityUser, string>(
                    "OidcSub",
                    (entityBuilder, propertyBuilder) => 
                    { 
                        propertyBuilder.HasDefaultValue(null); 
                    }
                );

            ObjectExtensionManager.Instance
                .MapEfCoreProperty<IdentityUser, string>(
                    "DisplayName",
                    (entityBuilder, propertyBuilder) => 
                    { 
                        propertyBuilder.HasDefaultValue(null); 
                    }
                );
        });
    }
}
