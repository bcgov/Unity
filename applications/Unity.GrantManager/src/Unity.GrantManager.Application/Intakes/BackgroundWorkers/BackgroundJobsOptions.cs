namespace Unity.GrantManager.Intakes.BackgroundWorkers
{
    public class BackgroundJobsOptions
    {
        public bool IsJobExecutionEnabled { get; set; }
        public QuartzBackgroundJobOptions Quartz { get; set; } = new();
    }

    public class QuartzBackgroundJobOptions
    {
        public bool IsAutoRegisterEnabled { get; set; }
    }
}
