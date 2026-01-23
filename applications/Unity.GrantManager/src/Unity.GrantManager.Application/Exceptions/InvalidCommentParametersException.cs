using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    public class InvalidCommentParametersException(string? message = null) 
        : AbpValidationException([new(message ?? InvalidCommentMessage)])
    {
        private const string InvalidCommentMessage = "Invalid Comment Parameters";
    }
}
