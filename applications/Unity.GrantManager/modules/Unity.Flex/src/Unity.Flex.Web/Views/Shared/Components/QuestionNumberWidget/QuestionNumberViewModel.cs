using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionNumberWidget;

public class QuestionNumberViewModel : RequiredFieldViewModel
{
    public Guid QuestionId { get; set; }
    public bool IsDisabled { get; set; }
    public double? Answer {  get; set; }    
    public string? Min {  get; set; }
    public string? Max { get; set; }
    public bool IsHumanConfirmed { get; set; } = true;
    public string? AICitation { get; set; }
    public int? AIConfidence { get; set; }
}
