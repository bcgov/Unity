using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Serialization;
using Volo.Abp;

namespace Unity.GrantManager.Web.Exceptions
{

    [Serializable]
    public class AutoRegisterUserException : UserFriendlyException
    {
        public AutoRegisterUserException(string message) : base(message)
        {
            LogLevel = LogLevel.Error;
        }

        protected AutoRegisterUserException(SerializationInfo serializationEntries, StreamingContext context) : base("Auto Register User Error")
        {
            LogLevel = LogLevel.Error;
        }
    }
}
