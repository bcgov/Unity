﻿using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;

namespace Unity.Payments.Web.Menus;

public class PaymentsMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        if (await featureChecker.IsEnabledAsync("Unity.Payments") && context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

#pragma warning disable CA1822 // Mark members as static
    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        //Add main menu items.
        context.Menu.AddItem(
            new ApplicationMenuItem(
                    PaymentsMenus.Prefix, 
                    displayName: "Payments",
                    "~/Payments", 
                    icon: "fa fa-money"
                )
                .AddItem(
                    new ApplicationMenuItem(
                            PaymentsMenus.Prefix,
                            displayName: "Payments",
                            url: "Payments"
                            )
                )                
                .AddItem(
                    new ApplicationMenuItem(
                            PaymentsMenus.PaymentService,
                            "Payment Settings",
                            url: "PaymentSettings"
                            )
                )
        );
        

        return Task.CompletedTask;
    }
#pragma warning restore CA1822 // Mark members as static
}