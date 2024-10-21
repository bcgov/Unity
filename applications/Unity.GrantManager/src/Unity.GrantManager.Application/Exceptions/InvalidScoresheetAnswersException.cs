using System;
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

        public InvalidScoresheetAnswersException(string[]? validationMessages = null)
            : base(InvalidScoresheetMessage, validationMessages?.Select(msg => new ValidationResult(msg)).ToList() ?? [])
        {
        }

        protected InvalidScoresheetAnswersException(SerializationInfo serializationEntries, StreamingContext context)
            : base(InvalidScoresheetMessage, [])
        {
        }
    }
}
