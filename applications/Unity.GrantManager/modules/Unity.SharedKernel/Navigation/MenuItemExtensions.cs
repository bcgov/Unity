using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.Modules.Shared.Specializations;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;

namespace Unity.Modules.Shared.Navigation;

public static class MenuItemExtensions
{
    private const string ExcludeWhenFeaturesKey = "_ExcludeWhenFeatures";
    private const string OnlyWhenFeaturesKey = "_OnlyWhenFeatures";
    private const string ExcludeWhenSpecializationsKey = "_ExcludeWhenSpecializations";
    private const string OnlyWhenSpecializationsKey = "_OnlyWhenSpecializations";

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
    /// Adds the item to the menu, respecting any feature or specialization visibility declarations.
    /// </summary>
    public static async Task AddItemAsync(
        this MenuConfigurationContext context,
        ApplicationMenuItem item)
    {
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();
        var specializationChecker = context.ServiceProvider.GetRequiredService<ISpecializationChecker>();

        if (item.CustomData.TryGetValue(ExcludeWhenFeaturesKey, out var excludeFeatObj)
            && excludeFeatObj is string[] excludeFeatures)
        {
            foreach (var feature in excludeFeatures)
                if (await featureChecker.IsEnabledAsync(feature))
                    return;
        }

        if (item.CustomData.TryGetValue(OnlyWhenFeaturesKey, out var onlyFeatObj)
            && onlyFeatObj is string[] onlyFeatures)
        {
            foreach (var feature in onlyFeatures)
                if (!await featureChecker.IsEnabledAsync(feature))
                    return;
        }

        if (item.CustomData.TryGetValue(ExcludeWhenSpecializationsKey, out var excludeSpecObj)
            && excludeSpecObj is string[] excludeSpecs)
        {
            foreach (var spec in excludeSpecs)
                if (await specializationChecker.IsEnabledAsync(spec))
                    return;
        }

        if (item.CustomData.TryGetValue(OnlyWhenSpecializationsKey, out var onlySpecObj)
            && onlySpecObj is string[] onlySpecs)
        {
            foreach (var spec in onlySpecs)
                if (!await specializationChecker.IsEnabledAsync(spec))
                    return;
        }

        context.Menu.AddItem(item);
    }
}
