using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GrantManager.Web.Filters
{
    public class ApiTokenAuthorizationHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor)
            {
                if (!context.ApiDescription.CustomAttributes().Any((a) => a is ServiceFilterAttribute))
                {
                    return;
                }

                operation.Security ??= [];
                operation.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        { new OpenApiSecuritySchemeReference("ApiKey"), new List<string>() }
                    });
            }
        }
    }
}
