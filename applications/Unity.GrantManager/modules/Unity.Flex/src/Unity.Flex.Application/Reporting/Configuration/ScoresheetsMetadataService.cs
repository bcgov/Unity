using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.DependencyInjection;

namespace Unity.Flex.Reporting.Configuration
{
    public class ScoresheetsMetadataService(IScoresheetRepository scoresheetRepository)
        : IScoresheetsMetadataService, ITransientDependency
    {
        public async Task<ScoresheetComponentMetaDataDto> GetScoresheetSchemaMetaDataAsync(Guid scoresheetId)
        {
            var scoresheet = await scoresheetRepository.GetAsync(scoresheetId);

            // Use the utility class to parse all fields in the worksheet
            var components = ScoresheetFieldSchemaParser.ParseScoresheet(scoresheet);

            return new ScoresheetComponentMetaDataDto()
            {
                Components = components
            };
        }

        public async Task<ScoresheetComponentMetaDataDto> GetScoresheetSchemaMetaDataItemAsync(Guid scoresheetId, string fieldKey)
        {
            var scoresheet = await scoresheetRepository.GetAsync(scoresheetId);

            // Parse all components and find those matching the key
            var allComponents = ScoresheetFieldSchemaParser.ParseScoresheet(scoresheet);
            var matchingComponents = allComponents.Where(c => c.Key == fieldKey).ToList();

            return new ScoresheetComponentMetaDataDto()
            {
                Components = matchingComponents
            };
        }
    }
}
