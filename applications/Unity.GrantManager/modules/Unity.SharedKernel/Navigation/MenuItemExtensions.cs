using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.Modules.Shared.Specializations;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Users;

namespace Unity.Modules.Shared.Navigation;

public static class MenuItemExtensions
{
    private const string ExcludeWhenFeaturesKey = "_ExcludeWhenFeatures";
    private const string OnlyWhenFeaturesKey = "_OnlyWhenFeatures";
    private const string ExcludeWhenSpecializationsKey = "_ExcludeWhenSpecializations";
    private const string OnlyWhenSpecializationsKey = "_OnlyWhenSpecializations";
    private const string OnlyWhenInRoleKey = "_OnlyWhenInRole";
    private const string RequiredPermissionOrRolePermissionKey = "_RequiredPermissionOrRolePermission";
    private const string RequiredPermissionOrRoleRolesKey = "_RequiredPermissionOrRoleRoles";

    /// <summary>
    /// Hides this menu item when any of the given features are enabled.
    /// </summary>
    public static ApplicationMenuItem ExcludeWhenFeatures(
        this ApplicationMenuItem item,
        params string[] featureNames)
    {
        item.CustomData[ExcludeWhenFeaturesKey] = featureNames;
        return item;
    }

    /// <summary>
    /// Shows this menu item only when all of the given features are enabled.
    /// </summary>
    public static ApplicationMenuItem OnlyWhenFeatures(
        this ApplicationMenuItem item,
        params string[] featureNames)
    {
        item.CustomData[OnlyWhenFeaturesKey] = featureNames;
        return item;
    }

    /// <summary>
    /// Hides this menu item when any of the given specializations are enabled.
    /// </summary>
    public static ApplicationMenuItem ExcludeWhenSpecializations(
        this ApplicationMenuItem item,
        params string[] specializationNames)
    {
        item.CustomData[ExcludeWhenSpecializationsKey] = specializationNames;
        return item;
    }

    /// <summary>
    /// Shows this menu item only when all of the given specializations are enabled.
    /// </summary>
    public static ApplicationMenuItem OnlyWhenSpecializations(
        this ApplicationMenuItem item,
        params string[] specializationNames)
    {
        item.CustomData[OnlyWhenSpecializationsKey] = specializationNames;
        return item;
    }

    /// <summary>
    /// Shows this menu item only when the current user is in any of the given roles
    /// (checked via ICurrentUser.IsInRole, e.g. Keycloak-issued client roles).
    /// </summary>
    public static ApplicationMenuItem OnlyWhenInRole(
        this ApplicationMenuItem item,
        params string[] roleNames)
    {
        item.CustomData[OnlyWhenInRoleKey] = roleNames;
        return item;
    }

    /// <summary>
    /// Shows this menu item when the current user either has the given permission granted
    /// (checked via IPermissionChecker, e.g. permissions granted through the DB/UI) or is in
    /// any of the given roles (checked via ICurrentUser.IsInRole, e.g. Keycloak-issued client
    /// roles).
    /// </summary>
    /// <remarks>
    /// Use this instead of the <c>ApplicationMenuItem(..., requiredPermissionName: ...)</c>
    /// constructor argument when a role (typically ITAdministrator/ITOperations) should also be
    /// able to see the item even without the permission explicitly granted. The constructor arg
    /// is checked entirely inside ABP's own menu-rendering pipeline via IPermissionChecker and
    /// has no awareness of roles or of authorization policies - it will NOT be satisfied by a
    /// role-based RoleOrPermissionRequirement authorization policy registered for the same
    /// permission name, even if that policy protects the page itself.
    /// (This is exactly what caused the TestingPermissions menu item to stay hidden for
    /// ITOperations/ITAdministrator users despite the page being reachable via a
    /// RoleOrPermissionRequirement policy of the same name - the menu check and the page's
    /// [Authorize] check are two unrelated code paths.)
    /// Use <see cref="OnlyWhenInRole"/> instead when the item should be role-gated ONLY, with no
    /// permission fallback.
    /// </remarks>
    public static ApplicationMenuItem RequirePermissionOrRole(
        this ApplicationMenuItem item,
        string permissionName,
        params string[] roleNames)
    {
        item.CustomData[RequiredPermissionOrRolePermissionKey] = permissionName;
        item.CustomData[RequiredPermissionOrRoleRolesKey] = roleNames;
        return item;
    }

    /// <summary>
    /// Adds the item to the menu, respecting any feature, specialization or role visibility declarations.
    /// </summary>
    public static async Task AddItemAsync(
        this MenuConfigurationContext context,
        ApplicationMenuItem item)
    {
        if (await IsVisibleAsync(item, context.ServiceProvider))
        {
            context.Menu.AddItem(item);
        }
    }

    /// <summary>
    /// Adds the item as a child of the given parent menu item, respecting any feature,
    /// specialization or role visibility declarations.
    /// </summary>
    public static async Task AddItemAsync(
        this ApplicationMenuItem parent,
        IServiceProvider serviceProvider,
        ApplicationMenuItem item)
    {
        if (await IsVisibleAsync(item, serviceProvider))
        {
            parent.AddItem(item);
        }
    }

    private static async Task<bool> IsVisibleAsync(ApplicationMenuItem item, IServiceProvider serviceProvider)
    {
        var featureChecker = serviceProvider.GetRequiredService<IFeatureChecker>();
        var specializationChecker = serviceProvider.GetRequiredService<ISpecializationChecker>();

        if (item.CustomData.TryGetValue(ExcludeWhenFeaturesKey, out var excludeFeatObj)
            && excludeFeatObj is string[] excludeFeatures)
        {
            foreach (var feature in excludeFeatures)
                if (await featureChecker.IsEnabledAsync(feature))
                    return false;
        }

        if (item.CustomData.TryGetValue(OnlyWhenFeaturesKey, out var onlyFeatObj)
            && onlyFeatObj is string[] onlyFeatures)
        {
            foreach (var feature in onlyFeatures)
                if (!await featureChecker.IsEnabledAsync(feature))
                    return false;
        }

        if (item.CustomData.TryGetValue(ExcludeWhenSpecializationsKey, out var excludeSpecObj)
            && excludeSpecObj is string[] excludeSpecs)
        {
            foreach (var spec in excludeSpecs)
                if (await specializationChecker.IsEnabledAsync(spec))
                    return false;
        }

        if (item.CustomData.TryGetValue(OnlyWhenSpecializationsKey, out var onlySpecObj)
            && onlySpecObj is string[] onlySpecs)
        {
            foreach (var spec in onlySpecs)
                if (!await specializationChecker.IsEnabledAsync(spec))
                    return false;
        }

        if (item.CustomData.TryGetValue(OnlyWhenInRoleKey, out var onlyRoleObj)
            && onlyRoleObj is string[] onlyRoles)
        {
            var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
            if (!Array.Exists(onlyRoles, currentUser.IsInRole))
                return false;
        }

        if (item.CustomData.TryGetValue(RequiredPermissionOrRolePermissionKey, out var permObj)
            && permObj is string permissionName)
        {
            var roles = item.CustomData.TryGetValue(RequiredPermissionOrRoleRolesKey, out var rolesObj)
                && rolesObj is string[] requiredRoles
                    ? requiredRoles
                    : [];

            var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
            if (!Array.Exists(roles, currentUser.IsInRole))
            {
                var permissionChecker = serviceProvider.GetRequiredService<IPermissionChecker>();
                if (!await permissionChecker.IsGrantedAsync(permissionName))
                    return false;
            }
        }

        return true;
    }
}
