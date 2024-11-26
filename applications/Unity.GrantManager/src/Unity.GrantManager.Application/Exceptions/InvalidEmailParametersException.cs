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

        // Constructor for creating a new exception with a custom message or default message
        public InvalidEmailParametersException(string? message = null)
            : base(new List<ValidationResult> { new ValidationResult(message ?? InvalidCommentMessage) })
        {
            // Validation is performed only when the exception is created (not during deserialization)
        }

        // Constructor for deserialization (restoring state, no validation)
        protected InvalidEmailParametersException(SerializationInfo serializationEntries, StreamingContext context) 
            : base()
        {
            // Do not trigger validation here during deserialization
            // This ensures validation is not re-triggered during deserialization.
        }

        // Override ToString to provide additional details when the exception is serialized
        public override string ToString()
        {
            return base.ToString() + $" | Details: {InvalidCommentMessage}";
        }
    }
}
