using System.Linq;
using System.Runtime.CompilerServices;
using Riok.Mapperly.Abstractions;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Scoresheets;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.WorksheetLinks;
using Unity.Flex.Worksheets;
using Volo.Abp.Mapperly;

namespace Unity.Flex;

public class WorksheetToWorksheetDtoMapper : MapperBase<Worksheet, WorksheetDto>
{
    public override WorksheetDto Map(Worksheet source)
    {
        var destination = new WorksheetDto();
        Map(source, destination);
        return destination;
    }

    public override void Map(Worksheet source, WorksheetDto destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.Title = source.Title;
        destination.Version = source.Version;
        destination.Published = source.Published;
        destination.ReportViewName = source.ReportViewName;
        destination.Sections = source.Sections?
            .Select(s => new WorksheetSectionMapper().Map(s))
            .ToList() ?? [];
        destination.TotalSections = (uint)(source.Sections?.Count ?? 0);
        destination.TotalFields = (uint)(source.Sections?.SelectMany(s => s.Fields).Count() ?? 0);
    }
}

public class WorksheetToWorksheetBasicDtoMapper : MapperBase<Worksheet, WorksheetBasicDto>
{
    public override WorksheetBasicDto Map(Worksheet source)
    {
        var destination = new WorksheetBasicDto();
        Map(source, destination);
        return destination;
    }

    public override void Map(Worksheet source, WorksheetBasicDto destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.Title = source.Title;
        destination.Version = source.Version;
        destination.Published = source.Published;
        destination.TotalSections = (uint)(source.Sections?.Count ?? 0);
        destination.TotalFields = (uint)(source.Sections?.SelectMany(s => s.Fields).Count() ?? 0);
    }
}

[Mapper]
public partial class WorksheetLinkMapper : MapperBase<WorksheetLink, WorksheetLinkDto>
{
    public override partial WorksheetLinkDto Map(WorksheetLink source);
    public override partial void Map(WorksheetLink source, WorksheetLinkDto destination);
}

[Mapper]
public partial class WorksheetSectionMapper : MapperBase<WorksheetSection, WorksheetSectionDto>
{
    public override partial WorksheetSectionDto Map(WorksheetSection source);
    public override partial void Map(WorksheetSection source, WorksheetSectionDto destination);
}

[Mapper]
public partial class WorksheetInstanceMapper : MapperBase<WorksheetInstance, WorksheetInstanceDto>
{
    public override partial WorksheetInstanceDto Map(WorksheetInstance source);
    public override partial void Map(WorksheetInstance source, WorksheetInstanceDto destination);
}

[Mapper]
public partial class CustomFieldValueMapper : TwoWayMapperBase<CustomFieldValue, CustomFieldValueDto>
{
    [ObjectFactory]
    private static CustomFieldValue CreateCustomFieldValue() =>
        (CustomFieldValue)RuntimeHelpers.GetUninitializedObject(typeof(CustomFieldValue));

    public override partial CustomFieldValueDto Map(CustomFieldValue source);
    public override partial void Map(CustomFieldValue source, CustomFieldValueDto destination);
    public override partial CustomFieldValue ReverseMap(CustomFieldValueDto source);
    public override partial void ReverseMap(CustomFieldValueDto source, CustomFieldValue destination);
}

[Mapper]
public partial class CustomFieldMapper : MapperBase<CustomField, CustomFieldDto>
{
    public override partial CustomFieldDto Map(CustomField source);
    public override partial void Map(CustomField source, CustomFieldDto destination);
}

[Mapper]
public partial class PersistWorksheetIntanceValuesMapper : MapperBase<PersistWorksheetIntanceValuesDto, PersistWorksheetIntanceValuesEto>
{
    public override partial PersistWorksheetIntanceValuesEto Map(PersistWorksheetIntanceValuesDto source);
    public override partial void Map(PersistWorksheetIntanceValuesDto source, PersistWorksheetIntanceValuesEto destination);
}

[Mapper]
public partial class QuestionToQuestionDtoMapper : MapperBase<Question, QuestionDto>
{
    [MapperIgnoreTarget(nameof(QuestionDto.ExtraProperties))]
    [MapperIgnoreTarget(nameof(QuestionDto.Answer))]
    [MapperIgnoreTarget(nameof(QuestionDto.IsHumanConfirmed))]
    [MapperIgnoreTarget(nameof(QuestionDto.AICitation))]
    [MapperIgnoreTarget(nameof(QuestionDto.AIConfidence))]
    public override partial QuestionDto Map(Question source);

    [MapperIgnoreTarget(nameof(QuestionDto.ExtraProperties))]
    [MapperIgnoreTarget(nameof(QuestionDto.Answer))]
    [MapperIgnoreTarget(nameof(QuestionDto.IsHumanConfirmed))]
    [MapperIgnoreTarget(nameof(QuestionDto.AICitation))]
    [MapperIgnoreTarget(nameof(QuestionDto.AIConfidence))]
    public override partial void Map(Question source, QuestionDto destination);
}

[Mapper]
public partial class ScoresheetSectionMapper : MapperBase<ScoresheetSection, ScoresheetSectionDto>
{
    [MapperIgnoreTarget(nameof(ScoresheetSectionDto.ExtraProperties))]
    public override partial ScoresheetSectionDto Map(ScoresheetSection source);

    [MapperIgnoreTarget(nameof(ScoresheetSectionDto.ExtraProperties))]
    public override partial void Map(ScoresheetSection source, ScoresheetSectionDto destination);
}

[Mapper]
public partial class ScoresheetMapper : MapperBase<Scoresheet, ScoresheetDto>
{
    public override partial ScoresheetDto Map(Scoresheet source);
    public override partial void Map(Scoresheet source, ScoresheetDto destination);
}

[Mapper]
public partial class ScoresheetInstanceMapper : MapperBase<ScoresheetInstance, ScoresheetInstanceDto>
{
    public override partial ScoresheetInstanceDto Map(ScoresheetInstance source);
    public override partial void Map(ScoresheetInstance source, ScoresheetInstanceDto destination);
}

[Mapper]
public partial class AnswerMapper : MapperBase<Answer, AnswerDto>
{
    public override partial AnswerDto Map(Answer source);
    public override partial void Map(Answer source, AnswerDto destination);
}
