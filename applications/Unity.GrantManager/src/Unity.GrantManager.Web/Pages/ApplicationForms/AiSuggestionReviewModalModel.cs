namespace Unity.GrantManager.Web.Pages.ApplicationForms;

public sealed class AiSuggestionReviewModalModel
{
    public string ModalId { get; init; } = string.Empty;
    public string ModalLabelId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string SourceColumnTitle { get; init; } = string.Empty;
    public string TargetColumnTitle { get; init; } = string.Empty;
    public string FieldsId { get; init; } = string.Empty;
    public string EmptyId { get; init; } = string.Empty;
    public string EmptyText { get; init; } = string.Empty;
    public string SelectAllId { get; init; } = string.Empty;
    public string? TitleInputId { get; init; }
    public string? TitleInputLabel { get; init; }
    public string? TitleInputPlaceholder { get; init; }
    public string PrimaryActionId { get; init; } = string.Empty;
    public string PrimaryActionText { get; init; } = string.Empty;
    public bool PrimaryActionDisabled { get; init; }
    public string ReviewLaterActionId { get; init; } = string.Empty;
    public string DiscardActionId { get; init; } = string.Empty;
}
