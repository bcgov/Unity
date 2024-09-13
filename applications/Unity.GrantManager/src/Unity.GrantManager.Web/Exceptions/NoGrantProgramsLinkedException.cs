using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Serialization;
using Volo.Abp;

namespace Unity.GrantManager.Web.Exceptions
{
    [Serializable]
    public class NoGrantProgramsLinkedException : UserFriendlyException
    {
        public NoGrantProgramsLinkedException(string message) : base(message)
        {
            LogLevel = LogLevel.Error;
        }

        protected NoGrantProgramsLinkedException(SerializationInfo serializationEntries, StreamingContext context) : base("No Grant Programs Linked")
        {
            LogLevel = LogLevel.Error;
        }
    }
}


