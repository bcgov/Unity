namespace Unity.Notifications.EmailNotifications
{
    public class EmailBackgroundJobsOptions
    {
        public bool IsJobExecutionEnabled { get; set; }
        public EmailQuartzBackgroundJobOptions Quartz { get; set; } = new();
        public EmailResendOptions EmailResend { get; set; } = new();
    }

    public class EmailQuartzBackgroundJobOptions
    {
        public bool IsAutoRegisterEnabled { get; set; }
    }

    public class EmailResendOptions
    {
        public int RetryAttemptsMaximum { get; set; } = 0;
    }
}
