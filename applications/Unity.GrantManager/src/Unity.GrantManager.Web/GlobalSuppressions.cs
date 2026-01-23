// This file is used to configure SonarQube analysis suppressions for this assembly.
// See https://docs.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers
using System.Diagnostics.CodeAnalysis;

// S4507: Debug features in non-production environments
// Justification: UseDeveloperExceptionPage and ShowPII are guarded by an environment check
// in GrantManagerWebModule.OnApplicationInitialization that only enables these features when
// the hosting environment is not Production (i.e., in all non-production environments). In Production,
// the application uses custom error pages without PII exposure.
[assembly: SuppressMessage(
    "Security", 
    "S4507:Make sure this debug feature is deactivated before delivering the code in production.",
    Justification = "Debug features are guarded by an env.IsProduction() check in GrantManagerWebModule.OnApplicationInitialization, which disables them in Production so they are only enabled in non-production environments.",
    Scope = "member",
    Target = "~M:Unity.GrantManager.Web.GrantManagerWebModule.OnApplicationInitialization(Volo.Abp.ApplicationInitializationContext)")]
