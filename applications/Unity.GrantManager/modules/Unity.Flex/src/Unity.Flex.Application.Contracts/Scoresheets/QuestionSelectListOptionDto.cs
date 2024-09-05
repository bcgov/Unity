using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class QuestionSelectListOptionDto
    {
        public string Text { get; set; } = string.Empty;
        public long Score { get; set; } = 0;
    }
}
