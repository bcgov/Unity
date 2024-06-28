using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionNumberWidget;

public class QuestionNumberViewModel 
{
    public Guid QuestionId { get; set; }
    public bool IsDisabled { get; set; }
    public double? Answer {  get; set; }
    
}
