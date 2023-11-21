using System;
using System.Runtime.Serialization;

namespace Unity.GrantManager.Integrations.Exceptions
{
    [Serializable]
    public class IntegrationServiceException : Exception
    {
        public IntegrationServiceException()
        {
        }

        public IntegrationServiceException(string? message) : base(message)
        {
        }

        public IntegrationServiceException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected IntegrationServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
