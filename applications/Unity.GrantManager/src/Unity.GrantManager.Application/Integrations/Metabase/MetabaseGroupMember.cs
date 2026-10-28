namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// One member of a Metabase permissions group, as returned by <c>GET /api/permissions/group/{id}</c>.
/// <see cref="MembershipId"/> is what <see cref="IMetabaseApiClient.RemoveGroupMembershipAsync"/> takes.
/// </summary>
public record MetabaseGroupMember(int UserId, int MembershipId, string Email, string? Name);
