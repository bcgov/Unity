using System;
using System.Runtime.Serialization;
using Volo.Abp.Validation;

namespace Unity.Payments.Domain.Exceptions
{
    [Serializable]
    public class ConfigurationDoesntExistException : AbpValidationException
    {        
        public ConfigurationDoesntExistException(string message)
            : base(message, [new(message)])
        {            
        }

        protected ConfigurationDoesntExistException(SerializationInfo serializationEntries, StreamingContext context) : base()
        {
        }
    }
}
