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
            : base([new(message ?? InvalidCommentMessage)])
        {
        }

        // Fix: Adjust the constructor to match the base class signature
        protected InvalidCommentParametersException(SerializationInfo serializationEntries, StreamingContext context)
            : base([new(InvalidCommentMessage)])
        {
        }
    }
}
