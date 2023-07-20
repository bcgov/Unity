using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerEntityFrameworkCoreTestModule)
    )]
public class GrantManagerDomainTestModule : AbpModule
{

}
