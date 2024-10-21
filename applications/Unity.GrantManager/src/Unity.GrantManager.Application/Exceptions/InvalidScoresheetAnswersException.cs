using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Volo.Abp;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    [Serializable]
    public class InvalidScoresheetAnswersException : AbpValidationException, IUserFriendlyException
    {
        private const string InvalidScoresheetMessage = "Scoresheet invalid";

        public InvalidScoresheetAnswersException(string? message, string[]? validationMessages = null)
            : base(ParseMessage(message), ParseValidationMessages(validationMessages))
        {
        }

        protected InvalidScoresheetAnswersException(SerializationInfo serializationEntries, StreamingContext context)
            : base(ParseMessage(null), ParseValidationMessages([]))
        {
        }

        private static string ParseMessage(string? message)
        {
            return message ?? InvalidScoresheetMessage;

        }
        private static List<ValidationResult> ParseValidationMessages(string[]? validationMessages)
        {
            return validationMessages?.Select(msg => new ValidationResult(msg)).ToList() ?? [];
        }
    }
}
