using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    [Serializable]
    public class InvalidEmailParametersException : AbpValidationException
    {
        private const string InvalidCommentMessage = "Invalid Email Parameters";

        public InvalidEmailParametersException(string? message = null)
            : base(new List<ValidationResult> { new(message ?? InvalidCommentMessage) })
        {
        }

        protected InvalidEmailParametersException(SerializationInfo serializationEntries, StreamingContext context) : base()
        {
        }
    }
}
