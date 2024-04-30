using System;

namespace Unity.Modules.Shared.Correlation
{
    public interface ICorrelationIdEntity
    {
        /// <summary>
        /// The external system / module Id that this relates to
        /// </summary>
        public Guid CorrelationId { get; }
    }
}
