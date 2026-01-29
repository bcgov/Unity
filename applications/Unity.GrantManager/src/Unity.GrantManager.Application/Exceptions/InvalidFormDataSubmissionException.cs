using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    public class InvalidFormDataSubmissionException(string? message = null)
        : AbpValidationException([new(message ?? InvalidCommentMessage)])
    {
        private const string InvalidCommentMessage = "Invalid Form Submission Data";
    }
}