using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using Unity.GrantManager.ApplicantProfile.ProfileData;

namespace Unity.GrantManager.Swagger
{
    public class ApplicantProfileDataSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type != typeof(ApplicantProfileDataDto))
                return;

            var subTypes = new Dictionary<string, System.Type>
            {
                ["CONTACTINFO"] = typeof(ApplicantContactInfoDto),
                ["ORGINFO"] = typeof(ApplicantOrgInfoDto),
                ["ADDRESSINFO"] = typeof(ApplicantAddressInfoDto),
                ["SUBMISSIONINFO"] = typeof(ApplicantSubmissionInfoDto),
                ["PAYMENTINFO"] = typeof(ApplicantPaymentInfoDto)
            };

            var oneOfSchemas = new List<OpenApiSchema>();
            foreach (var (discriminatorValue, subType) in subTypes)
            {
                var subSchema = context.SchemaGenerator.GenerateSchema(subType, context.SchemaRepository);
                oneOfSchemas.Add(subSchema);
            }

            schema.OneOf = oneOfSchemas;
            schema.Discriminator = new OpenApiDiscriminator
            {
                PropertyName = "dataType",
                Mapping = new Dictionary<string, string>()
            };

            foreach (var (discriminatorValue, subType) in subTypes)
            {
                var schemaId = context.SchemaRepository.Schemas.ContainsKey(subType.FullName!)
                    ? subType.FullName!
                    : subType.Name;
                schema.Discriminator.Mapping[discriminatorValue] = $"#/components/schemas/{schemaId}";
            }

            schema.Description = "Polymorphic data payload. The shape depends on the 'dataType' discriminator (key parameter).";
        }
    }
}
