using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.Configuration
{
    public static class ScoresheetFieldSchemaParser
    {
        public static List<ScoresheetComponentMetaDataItemDto> ParseScoresheet(Scoresheet scoresheet)
        {
            if (scoresheet?.Sections == null)
                return new List<ScoresheetComponentMetaDataItemDto>();

            var allComponents = new List<ScoresheetComponentMetaDataItemDto>();

            foreach (var section in scoresheet.Sections)
            {
                if (section.Fields == null) continue;

                foreach (var question in section.Fields)
                {
                    allComponents.AddRange(ParseQuestion(question, scoresheet));
                }
            }

            return allComponents;
        }

        /// <summary>
        /// Parses a question and returns component metadata items.
        /// For all question types including SelectList, returns a single component representing the question's value.
        /// </summary>
        /// <param name="question">The question to parse</param>
        /// <param name="scoresheet">The scoresheet containing the question (for name context)</param>
        /// <returns>List of component metadata items</returns>
        private static List<ScoresheetComponentMetaDataItemDto> ParseQuestion(Question question, Scoresheet scoresheet)
        {
            if (question == null)
                return new List<ScoresheetComponentMetaDataItemDto>();

            var components = new List<ScoresheetComponentMetaDataItemDto>();

            // For all question types (Number, Text, YesNo, SelectList, TextArea), return a single component
            // SelectList will contain the selected option value, not individual option components
            components.Add(CreateSimpleComponent(question, scoresheet));

            return components;
        }

        /// <summary>
        /// Creates a simple component metadata item for all question types.
        /// For SelectList questions, this represents the selected value, not individual options.
        /// </summary>
        /// <param name="question">The question to create a component for</param>
        /// <param name="scoresheet">The scoresheet containing the question</param>
        /// <returns>Component metadata item</returns>
        private static ScoresheetComponentMetaDataItemDto CreateSimpleComponent(Question question, Scoresheet scoresheet)
        {
            var section = scoresheet.Sections.FirstOrDefault(s => s.Id == question.SectionId);
            var sectionName = SanitizeName(section?.Name ?? "unknown_section");
            var scoresheetName = SanitizeName(scoresheet.Name);
            var questionName = SanitizeName(question.Name);

            return new ScoresheetComponentMetaDataItemDto
            {
                Id = question.Id.ToString(),
                Key = question.Name,
                Label = question.Label,
                Type = question.Type.ToString(),
                Path = $"{scoresheetName}->{sectionName}->{questionName}",
                TypePath = $"scoresheet->section->{question.Type.ToString().ToLowerInvariant()}",
                DataPath = $"{questionName}"
            };
        }

        /// <summary>
        /// Sanitizes names for use in paths by removing special characters and spaces.
        /// </summary>
        /// <param name="name">The name to sanitize</param>
        /// <returns>Sanitized name</returns>
        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "unknown";

            // Keep alphanumeric characters, underscores, and hyphens
            // Replace spaces with underscores
            return Regex.Replace(
                name.Trim().Replace(" ", "_"), 
                @"[^a-zA-Z0-9_\-]", 
                "");
        }
    }
}
