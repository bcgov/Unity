using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class EditScoresheetsDto
    {
        public string Name { get; set; } = string.Empty;
        public string ActionType {  get; set; } = string.Empty;
    }
}
