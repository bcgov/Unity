using Volo.Abp.Modularity;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerApplicationModule),
    typeof(GrantManagerDomainTestModule)
    )]
public class GrantManagerApplicationTestModule : AbpModule
{

}
