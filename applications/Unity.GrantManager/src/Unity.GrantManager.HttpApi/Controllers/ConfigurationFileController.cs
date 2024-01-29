using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Controllers
{
    // This is designed to download configuration files for the user for app setup
    public class ConfigurationFileController : AbpController
    {
        private readonly IApplicationFormConfigurationAppService _applicationFormConfigurationAppService;
        private readonly ICurrentTenant _currentTenant;

        public ConfigurationFileController(IApplicationFormConfigurationAppService applicationFormConfigurationAppService,
            ICurrentTenant currentTenant)
        {
            _applicationFormConfigurationAppService = applicationFormConfigurationAppService;
            _currentTenant = currentTenant;
        }

        [HttpGet]
        [Route("/api/app/configurationFile/{type}")]
        public async Task<FileResult> DownloadConfiguration(string type)
        {
            // When we add more create a resolver / contributor and interface to resolve this class
            return type.ToLower() switch
            {
                "applicationforms" => await GetApplicationFormsConfigAsync(),
                _ => throw new AbpValidationException(new List<ValidationResult>() { new ValidationResult($"{type} not supported") })
            };
        }

        private async Task<FileResult> GetApplicationFormsConfigAsync()
        {
            byte[]? bytes = default;

            using (var ms = new MemoryStream())
            {
                using TextWriter tw = new StreamWriter(ms);
                tw.Write(JsonSerializer.Serialize(await _applicationFormConfigurationAppService.GetConfiguration(), 
                    options: new JsonSerializerOptions() 
                        { 
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }));
                tw.Flush();
                ms.Position = 0;
                bytes = ms.ToArray();
                ms.Close();
            }

            return File(bytes, "application/json", $"forms-config-{_currentTenant.Id}.txt");
        }
    }
}
