using System;

namespace Unity.Payments.Correlation
{
    public interface ICorrelationIdEntity
    {
        /// <summary>
        /// The external system / module Id that this relates to
        /// </summary>
        public Guid CorrelationId { get; }
    }
}
