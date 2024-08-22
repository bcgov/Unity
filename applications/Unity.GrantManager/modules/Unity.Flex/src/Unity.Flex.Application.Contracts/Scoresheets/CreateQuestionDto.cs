using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class CreateQuestionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Description {  get; set; }
        public bool Enabled { get; set; }
        public uint QuestionType { get; set; }
        public object? Definition { get; set; }
    }
}
