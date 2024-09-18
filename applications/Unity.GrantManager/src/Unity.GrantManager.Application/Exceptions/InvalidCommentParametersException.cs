using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    [Serializable]
    public class InvalidCommentParametersException : AbpValidationException
    {
        private const string InvalidCommentMessage = "Invalid Comment Parameters";

        public InvalidCommentParametersException(string? message = null)
            : base(new List<ValidationResult> { new(message ?? InvalidCommentMessage) })
        {
        }

        protected InvalidCommentParametersException(SerializationInfo serializationEntries, StreamingContext context) : base()
        {
        }
    }
}
