using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    public class InvalidCommentParametersException : AbpValidationException
    {
        private const string InvalidCommentMessage = "Invalid Comment Parameters";

        public InvalidCommentParametersException(string? message = null) 
            : base (new List<ValidationResult> { new ValidationResult(message ?? InvalidCommentMessage) })
        {                  
        }
    }
}
