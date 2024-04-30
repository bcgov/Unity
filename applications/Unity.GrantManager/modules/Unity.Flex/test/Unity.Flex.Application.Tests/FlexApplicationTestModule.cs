using Volo.Abp.Modularity;

namespace Unity.Flex;

[DependsOn(
    typeof(FlexApplicationModule)    
    )]
public class FlexApplicationTestModule : AbpModule
{

}
