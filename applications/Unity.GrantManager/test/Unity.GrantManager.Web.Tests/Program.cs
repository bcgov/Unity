using Microsoft.AspNetCore.Builder;
using Unity.GrantManager;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
await builder.RunAbpModuleAsync<GrantManagerWebTestModule>();

#pragma warning disable S1118 // Utility classes should not have public constructors
public partial class Program
#pragma warning restore S1118 // Utility classes should not have public constructors
{
}
