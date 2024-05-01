namespace Unity.Modules.Shared.Correlation
{
    public interface ICorrelationProviderEntity
    {
        /// <summary>
        /// The external system / module correlation provider
        /// </summary>
        public string CorrelationProvider { get; }
    }
}
