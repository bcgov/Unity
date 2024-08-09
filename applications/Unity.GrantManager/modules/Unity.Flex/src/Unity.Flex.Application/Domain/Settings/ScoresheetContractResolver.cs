using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;
using Unity.Flex.Domain.Scoresheets;


namespace Unity.Flex.Domain.Settings;

public class ScoresheetContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        property.Readable = true;
        property.Writable = true;

        if ((property.DeclaringType == typeof(Scoresheet) && property.PropertyName == "Instances") ||
            (property.DeclaringType == typeof(ScoresheetSection) && (property.PropertyName == "Scoresheet" || property.PropertyName == "ScoresheetId")) ||
            (property.DeclaringType == typeof(Question) && (property.PropertyName == "Section" || property.PropertyName == "SectionId" || property.PropertyName == "Answers")) ||
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
