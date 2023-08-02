using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Intake;

/// <summary>
/// Represents a collection of functions to interact with the CHEFS Form API endpoints
/// </summary>
public interface IFormAppService : IApplicationService
{
    /// <summary>
    /// Get details of a form (and metadata for versions) 
    /// </summary>
    /// <param name="formId">ID of the form</param>
    /// <returns>Form</returns>
    Task<object> GetForm(Guid? formId);

    /// <summary>
    /// List all forms 
    /// </summary>
    /// <param name="active">filter forms by active status</param>
    /// <returns>List&lt;Form&gt;</returns>
    Task<List<object>> ListForms(bool? active);
}
