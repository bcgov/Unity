using Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;
using Volo.Abp.Mapperly;
using static Unity.TenantManagement.Web.Pages.TenantManagement.Tenants.AssignManagerModalModel;
using static Unity.TenantManagement.Web.Pages.TenantManagement.Tenants.ConfigurationModalModel;

namespace Unity.TenantManagement.Web;

internal static class TenantExtraPropertiesCopier
{
    public static void Copy(Volo.Abp.Data.IHasExtraProperties source, Volo.Abp.Data.IHasExtraProperties destination)
    {
        foreach (var kvp in source.ExtraProperties)
        {
            destination.ExtraProperties[kvp.Key] = kvp.Value;
        }
    }
}

public class TenantDtoToEditTenantInfoMapper : MapperBase<TenantDto, EditModalModel.TenantInfoModel>
{
    public override EditModalModel.TenantInfoModel Map(TenantDto source)
    {
        var destination = new EditModalModel.TenantInfoModel();
        Map(source, destination);
        return destination;
    }

    public override void Map(TenantDto source, EditModalModel.TenantInfoModel destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.Division = source.Division;
        destination.Branch = source.Branch;
        destination.Description = source.Description;
        destination.CasClientCode = source.CasClientCode;
        destination.ConcurrencyStamp = source.ConcurrencyStamp;
        TenantExtraPropertiesCopier.Copy(source, destination);
    }
}

public class CreateTenantInfoToTenantCreateDtoMapper : MapperBase<CreateModalModel.TenantInfoModel, TenantCreateDto>
{
    public override TenantCreateDto Map(CreateModalModel.TenantInfoModel source)
    {
        var destination = new TenantCreateDto();
        Map(source, destination);
        return destination;
    }

    public override void Map(CreateModalModel.TenantInfoModel source, TenantCreateDto destination)
    {
        destination.Name = source.Name;
        destination.Division = source.Division;
        destination.Branch = source.Branch;
        destination.Description = source.Description;
        destination.CasClientCode = source.CasClientCode;
        destination.UserIdentifier = source.UserIdentifier;
        TenantExtraPropertiesCopier.Copy(source, destination);
    }
}

public class EditTenantInfoToTenantUpdateDtoMapper : MapperBase<EditModalModel.TenantInfoModel, TenantUpdateDto>
{
    public override TenantUpdateDto Map(EditModalModel.TenantInfoModel source)
    {
        var destination = new TenantUpdateDto();
        Map(source, destination);
        return destination;
    }

    public override void Map(EditModalModel.TenantInfoModel source, TenantUpdateDto destination)
    {
        destination.Name = source.Name;
        destination.Division = source.Division;
        destination.Branch = source.Branch;
        destination.Description = source.Description;
        destination.CasClientCode = source.CasClientCode;
        destination.ConcurrencyStamp = source.ConcurrencyStamp;
        TenantExtraPropertiesCopier.Copy(source, destination);
    }
}

public class TenantDtoToConfigurationTenantInfoMapper : MapperBase<TenantDto, TenantInfoModel>
{
    public override TenantInfoModel Map(TenantDto source)
    {
        var destination = new TenantInfoModel();
        Map(source, destination);
        return destination;
    }

    public override void Map(TenantDto source, TenantInfoModel destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.Division = source.Division;
        destination.Branch = source.Branch;
        destination.Description = source.Description;
        destination.CasClientCode = source.CasClientCode;
        destination.ConcurrencyStamp = source.ConcurrencyStamp;
        TenantExtraPropertiesCopier.Copy(source, destination);
    }
}

public class ConfigurationTenantInfoToTenantUpdateDtoMapper : MapperBase<TenantInfoModel, TenantUpdateDto>
{
    public override TenantUpdateDto Map(TenantInfoModel source)
    {
        var destination = new TenantUpdateDto();
        Map(source, destination);
        return destination;
    }

    public override void Map(TenantInfoModel source, TenantUpdateDto destination)
    {
        destination.Name = source.Name;
        destination.Division = source.Division;
        destination.Branch = source.Branch;
        destination.Description = source.Description;
        destination.CasClientCode = source.CasClientCode ?? string.Empty;
        destination.ConcurrencyStamp = source.ConcurrencyStamp;
        TenantExtraPropertiesCopier.Copy(source, destination);
    }
}

public class TenantDtoToConfigurationManagerInfoMapper : MapperBase<TenantDto, ManagerInfoModel>
{
    public override ManagerInfoModel Map(TenantDto source)
    {
        var destination = new ManagerInfoModel();
        Map(source, destination);
        return destination;
    }

    public override void Map(TenantDto source, ManagerInfoModel destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        TenantExtraPropertiesCopier.Copy(source, destination);
    }
}

public class TenantDtoToAssignManagerInfoMapper : MapperBase<TenantDto, AssignManagerInfoModel>
{
    public override AssignManagerInfoModel Map(TenantDto source)
    {
        var destination = new AssignManagerInfoModel();
        Map(source, destination);
        return destination;
    }

    public override void Map(TenantDto source, AssignManagerInfoModel destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.ConcurrencyStamp = source.ConcurrencyStamp;
        TenantExtraPropertiesCopier.Copy(source, destination);
    }
}
