using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using Unity.GrantManager.ApplicantProfile.ProfileData;

namespace Unity.GrantManager.Swagger
{
    public class ApplicantProfileDataSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type != typeof(ApplicantProfileDataDto))
                return;

            // OpenAPI.NET v2 exposes IOpenApiSchema (read-only) in filter signatures,
            // but the underlying instance is OpenApiSchema. Cast to mutate.
            if (schema is not OpenApiSchema mutableSchema)
                return;

            var subTypes = new Dictionary<string, System.Type>
            {
                ["CONTACTINFO"] = typeof(ApplicantContactInfoDto),
                ["ORGINFO"] = typeof(ApplicantOrgInfoDto),
                ["ADDRESSINFO"] = typeof(ApplicantAddressInfoDto),
                ["SUBMISSIONINFO"] = typeof(ApplicantSubmissionInfoDto),
                ["PAYMENTINFO"] = typeof(ApplicantPaymentInfoDto)
            };

            var oneOfSchemas = new List<IOpenApiSchema>();
            foreach (var (_, subType) in subTypes)
            {
                var subSchema = context.SchemaGenerator.GenerateSchema(subType, context.SchemaRepository);
                oneOfSchemas.Add(subSchema);
            }

            mutableSchema.OneOf = oneOfSchemas;
            mutableSchema.Discriminator = new OpenApiDiscriminator
            {
                PropertyName = "dataType",
                Mapping = new Dictionary<string, OpenApiSchemaReference>()
            };

            foreach (var (discriminatorValue, subType) in subTypes)
            {
                var schemaId = context.SchemaRepository.Schemas.ContainsKey(subType.FullName!)
                    ? subType.FullName!
                    : subType.Name;
                mutableSchema.Discriminator.Mapping[discriminatorValue] = new OpenApiSchemaReference(schemaId);
            }

            mutableSchema.Description = "Polymorphic data payload. The shape depends on the 'dataType' discriminator (key parameter).";
        }
    }
}
