using Volo.Abp.Modularity;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerApplicationModule),
    typeof(GrantManagerHttpApiClientModule),
    typeof(GrantManagerDomainTestModule)
    )]
public class GrantManagerApplicationTestModule : AbpModule
{

}
