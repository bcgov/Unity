#nullable enable
using System.Threading.Tasks;

namespace Unity.TenantManagement;

/// <summary>Thin seam over the CSS directory API for onboarding use cases.</summary>
public interface IOnboardingUserLookup
{
    /// <summary>Returns the IDIR user GUID if the email resolves in the directory, null otherwise.</summary>
    Task<string?> FindUserGuidByEmailAsync(string email);
}
