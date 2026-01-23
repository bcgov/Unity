// This file is used to configure SonarQube analysis suppressions for this assembly.
// See https://docs.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers
using System.Diagnostics.CodeAnalysis;

// S4507: Debug features in non-production environments
// Justification: UseDeveloperExceptionPage and ShowPII are properly guarded by ShouldEnableDebugFeatures()
// which checks the environment. These features are only enabled in Development, Test, and Staging (UAT) environments.
// Production environment uses custom error pages without PII exposure.
[assembly: SuppressMessage(
    "Security", 
    "S4507:Make sure this debug feature is deactivated before delivering the code in production.",
    Justification = "Debug features are properly guarded by environment checks in ShouldEnableDebugFeatures() method and only enabled in non-production environments.",
    Scope = "member",
    Target = "~M:Unity.GrantManager.Web.GrantManagerWebModule.OnApplicationInitialization(Volo.Abp.ApplicationInitializationContext)")]
