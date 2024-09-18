using System;
using System.Runtime.Serialization;
using Volo.Abp.Validation;

namespace Unity.Payments.Domain.Exceptions
{
    [Serializable]
    public class ConfigurationExistsException : AbpValidationException
    {
        public ConfigurationExistsException(string message)
            : base(message, [new(message)])
        {
        }

        protected ConfigurationExistsException(SerializationInfo serializationEntries, StreamingContext context) : base()
        {
        }
    }
}
