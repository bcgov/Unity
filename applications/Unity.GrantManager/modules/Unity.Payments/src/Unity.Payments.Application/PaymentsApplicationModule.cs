﻿using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Localization.ExceptionHandling;
using Unity.Payments.Localization;

namespace Unity.Payments;

[DependsOn(
    typeof(PaymentsDomainModule),
    typeof(PaymentsApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule)    
    )]
public class PaymentsApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<PaymentsApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<PaymentsApplicationModule>(validate: true);
        });

        context.Services.Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace("Unity.Payments", typeof(PaymentsResource));
        });
    }
}