using System;
using System.Net;
using Volo.Abp;

namespace Unity.GrantManager.Integrations.Exceptions
{
    public class IntegrationServiceException : Exception, IUserFriendlyException
    {
        /// <summary>
        /// The HTTP status code from the failed call, when known - lets a caller distinguish
        /// specific transient statuses (e.g. retry only on 422) without parsing the message.
        /// Optional: not every throw site has a response to attribute this to.
        /// </summary>
        public HttpStatusCode? StatusCode { get; init; }

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
