using System;
using Volo.Abp;

namespace Unity.GrantManager.Integrations.Exceptions
{    
    public class IntegrationServiceException : Exception, IUserFriendlyException
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
    }
}
