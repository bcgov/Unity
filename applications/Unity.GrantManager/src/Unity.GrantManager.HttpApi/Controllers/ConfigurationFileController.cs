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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Controllers
{
    // This is designed to download configuration files for the user for app setup
    [Route("/api/app/configurationFile")]
    public class ConfigurationFileController : AbpController
    {
        private readonly IApplicationFormConfigurationAppService _applicationFormConfigurationAppService;
        private readonly ICurrentTenant _currentTenant;
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        public ConfigurationFileController(IApplicationFormConfigurationAppService applicationFormConfigurationAppService,
            ICurrentTenant currentTenant)
        {
            _applicationFormConfigurationAppService = applicationFormConfigurationAppService;
            _currentTenant = currentTenant;
        }

        [HttpGet("{type}")]
        public async Task<IActionResult> DownloadConfiguration(string type)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest("Configuration type must be provided.");
            }

            try
            {
                return type.ToLower() switch
                {
                    "applicationforms" => await GetApplicationFormsConfigAsync(),
                    _ => throw new AbpValidationException(new List<ValidationResult> { new ValidationResult($"{type} not supported") })
                };
            }
            catch (AbpValidationException ex)
            {
                return BadRequest(ex.ValidationErrors);
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                logger.LogError(ex, "ConfigurationFileController->DownloadConfiguration: {ExceptionMessage}", ExceptionMessage);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        private async Task<FileResult> GetApplicationFormsConfigAsync()
        {
            byte[]? bytes = default;

            using (var ms = new MemoryStream())
            {
                using TextWriter tw = new StreamWriter(ms);
                await tw.WriteAsync(JsonSerializer.Serialize(await _applicationFormConfigurationAppService.GetConfiguration(),
                    options: new JsonSerializerOptions()
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }));
                await tw.FlushAsync();
                ms.Position = 0;
                bytes = ms.ToArray();
                ms.Close();
            }

            return File(bytes, "application/json", $"forms-config-{_currentTenant.Id}.txt");
        }
    }
}
