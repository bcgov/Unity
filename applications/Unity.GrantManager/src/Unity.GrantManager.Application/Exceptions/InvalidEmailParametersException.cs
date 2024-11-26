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

        // Constructor for creating a new exception with a message
        public InvalidEmailParametersException(string? message = null)
            : base(new List<ValidationResult> { new ValidationResult(message ?? InvalidCommentMessage) })
        {
        }

        protected InvalidEmailParametersException(SerializationInfo serializationEntries, StreamingContext context) : base()
        {
            // After deserialization, do not trigger validation
            // The base constructor takes care of restoring the exception state.
        }

        // Optionally, override ToString to provide more detail when the exception is logged
        public override string ToString()
        {
            var baseStr = base.ToString();
            return $"{baseStr}\nDetails: {InvalidCommentMessage}";
        }
    }
}
