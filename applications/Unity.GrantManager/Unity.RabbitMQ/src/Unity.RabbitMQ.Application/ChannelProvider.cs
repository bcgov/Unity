
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Unity.RabbitMQ.Interfaces;

namespace Unity.RabbitMQ
{
    public sealed class ChannelProvider : IDisposable, IChannelProvider
    {
        private readonly IConnectionProvider _connectionProvider;
        private readonly ILogger<ChannelProvider> _logger;
        private IModel? _model;

        public ChannelProvider(
            IConnectionProvider connectionProvider,
            ILogger<ChannelProvider> logger)
        {
            _connectionProvider = connectionProvider;
            _logger = logger;
        }

        public IModel GetChannel()
        {
            if (_model == null || !_model.IsOpen)
            {
                _model = _connectionProvider.GetConnection().CreateModel();
            }

            return _model;
        }

        public void Dispose()
        {
            try
            {
                if (_model != null)
                {
                    _model?.Close();
                    _model?.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot dispose RabbitMq channel or connection");
            }
        }
    }
}