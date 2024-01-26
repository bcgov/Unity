using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GrantManager.Web.Filters
{
    public class ApiTokenAuthorizationHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                if (!context.ApiDescription.CustomAttributes().Any((a) => a is ServiceFilterAttribute))
                {                    
                    return;
                }

                operation.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Security.Add(
                    new OpenApiSecurityRequirement{
                    {
                        new OpenApiSecurityScheme
                        {                            
                            In = ParameterLocation.Header,  
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        System.Array.Empty<string>()
                    }
                });
            }
        }
    }
}
