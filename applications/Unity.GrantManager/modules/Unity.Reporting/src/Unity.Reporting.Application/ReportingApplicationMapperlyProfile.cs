using System.Text.Json;
using Riok.Mapperly.Abstractions;
using Unity.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.Mapperly;

namespace Unity.Reporting;

[Mapper]
public partial class ReportColumnsMapToReportColumnsMapDtoMapper : MapperBase<ReportColumnsMap, ReportColumnsMapDto>
{
    [MapperIgnoreTarget(nameof(ReportColumnsMapDto.DetectedChanges))]
    public override partial ReportColumnsMapDto Map(ReportColumnsMap source);

    [MapperIgnoreTarget(nameof(ReportColumnsMapDto.DetectedChanges))]
    public override partial void Map(ReportColumnsMap source, ReportColumnsMapDto destination);

    [UserMapping]
    private static MappingDto MapMappingJson(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return new MappingDto();
        }

        try
        {
            return JsonSerializer.Deserialize<MappingDto>(source, (JsonSerializerOptions?)null) ?? new MappingDto();
        }
        catch
        {
            return new MappingDto();
        }
    }
}

[Mapper]
public partial class MappingToMappingDtoMapper : MapperBase<Mapping, MappingDto>
{
    public override partial MappingDto Map(Mapping source);

    public override partial void Map(Mapping source, MappingDto destination);
}

[Mapper]
public partial class MapRowToMapRowDtoMapper : MapperBase<MapRow, MapRowDto>
{
    public override partial MapRowDto Map(MapRow source);

    public override partial void Map(MapRow source, MapRowDto destination);
}

[Mapper]
public partial class MapMetadataToMapMetadataDtoMapper : MapperBase<MapMetadata, MapMetadataDto>
{
    public override partial MapMetadataDto Map(MapMetadata source);

    public override partial void Map(MapMetadata source, MapMetadataDto destination);
}
