using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;
using Unity.Flex.Domain.Worksheets;


namespace Unity.Flex.Domain.Settings;

public class WorksheetContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        property.Readable = true;
        property.Writable = true;

        if ((property.DeclaringType == typeof(WorksheetSection) && (property.PropertyName == "Worksheet" || property.PropertyName == "WorksheetId")) ||
            (property.DeclaringType == typeof(CustomField) && (property.PropertyName == "Section" || property.PropertyName == "SectionId")) ||
            property.PropertyName == "TenantId" ||
            property.PropertyName == "IsDeleted" ||
            property.PropertyName == "DeleterId" ||
            property.PropertyName == "DeletionTime" ||
            property.PropertyName == "LastModificationTime" ||
            property.PropertyName == "LastModifierId" ||
            property.PropertyName == "CreationTime" ||
            property.PropertyName == "CreatorId" ||
            property.PropertyName == "Id" ||
            property.PropertyName == "ExtraProperties" ||
            property.PropertyName == "ConcurrencyStamp")
        {
            property.ShouldSerialize = instance => false;
        }
        return property;
    }
}
