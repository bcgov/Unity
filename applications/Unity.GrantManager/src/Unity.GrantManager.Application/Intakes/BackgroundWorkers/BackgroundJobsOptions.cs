namespace Unity.GrantManager.Intakes.BackgroundWorkers
{
    public class BackgroundJobsOptions
    {
        public bool IsJobExecutionEnabled { get; set; }
        public QuartzBackgroundJobOptions Quartz { get; set; } = new();
        public IntakeResyncOptions IntakeResync { get; set; } = new();
    }

    public class QuartzBackgroundJobOptions
    {
        public bool IsAutoRegisterEnabled { get; set; }
    }
    public class IntakeResyncOptions
    {
        public string Expression { get; set; } = string.Empty;
        public string NumDaysToCheck { get; set; } = string.Empty;
    }
}
