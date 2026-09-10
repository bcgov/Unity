namespace Unity.GrantManager.ApplicationForms.Mapping;

public enum FormMappingReviewPhase
{
    MappingReview = 0,
    WorksheetReview = 1,
    PublishAndAssignWorksheets = 2,
    FinalMappingReview = 3,
    Completed = 4
}

public enum FormGenerationWorkflowState
{
    GenerateInitialMapping = 10,
    ReviewInitialMapping = 20,
    GenerateWorksheets = 30,
    ReviewWorksheets = 40,
    PublishAndAssignWorksheets = 50,
    GenerateFinalMapping = 60,
    ReviewFinalMapping = 70,
    Completed = 80
}

public enum FormGenerationWorkflowAction
{
    GenerateInitialMapping = 10,
    ReviewInitialMapping = 20,
    GenerateWorksheets = 30,
    ReviewWorksheets = 40,
    PublishAndAssignWorksheets = 50,
    GenerateFinalMapping = 60,
    ReviewFinalMapping = 70,
    GenerateMapping = 80,
    GenerateWorksheetsNextCycle = 90
}
