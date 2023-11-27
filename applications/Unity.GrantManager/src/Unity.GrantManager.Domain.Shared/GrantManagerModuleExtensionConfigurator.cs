using System.ComponentModel;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace Unity.GrantManager;

public static class GrantManagerModuleExtensionConfigurator
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        OneTimeRunner.Run(() =>
        {
            ConfigureExistingProperties();
            ConfigureExtraProperties();
        });
    }

    private static void ConfigureExistingProperties()
    {
        /* Intentionally left empty */
    }

    private static void ConfigureExtraProperties()
    {
        ObjectExtensionManager.Instance.Modules()
           .ConfigureIdentity(identity =>
           {
               identity.ConfigureUser(user =>
               {
                   user.AddOrUpdateProperty<string>( //property type: string
                       "OidcSub", //property name
                       property =>
                       {
                           property.Attributes.Add(new ReadOnlyAttribute(true));
                           property.Configuration[IdentityModuleExtensionConsts.ConfigurationNames.AllowUserToEdit] = false;
                       }
                   );

                   user.AddOrUpdateProperty<string>( //property type: string
                      "DisplayName", //property name
                      property =>
                      {
                          property.Attributes.Add(new ReadOnlyAttribute(true));
                          property.Configuration[IdentityModuleExtensionConsts.ConfigurationNames.AllowUserToEdit] = false;
                      }
                  );
               });
           });

    }
}
