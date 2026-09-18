using System;

namespace Unity.Flex.Worksheets
{
    public enum WorksheetAvailabilityStatus
    {
        Published,
        All,
        Archived
    }

    public static class WorksheetAvailabilityFilter
    {
        public static bool Matches(
            WorksheetBasicDto worksheet,
            string? searchText,
            WorksheetAvailabilityStatus status)
        {
            if (!worksheet.Published)
            {
                return false;
            }

            var normalizedSearchText = searchText?.Trim();
            var matchesSearch = string.IsNullOrEmpty(normalizedSearchText)
                || worksheet.Title.Contains(normalizedSearchText, StringComparison.OrdinalIgnoreCase)
                || worksheet.Name.Contains(normalizedSearchText, StringComparison.OrdinalIgnoreCase);

            if (!matchesSearch)
            {
                return false;
            }

            return status switch
            {
                WorksheetAvailabilityStatus.Archived => worksheet.IsArchived,
                WorksheetAvailabilityStatus.Published => !worksheet.IsArchived,
                _ => true
            };
        }
    }
}