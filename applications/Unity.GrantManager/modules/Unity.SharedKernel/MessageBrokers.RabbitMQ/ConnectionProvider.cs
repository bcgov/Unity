
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public sealed class ConnectionProvider : IAsyncDisposable, IDisposable, IConnectionProvider
    {
        private readonly ILogger<ConnectionProvider> _logger;
        private readonly IConnectionFactory _connectionFactory;
        private IConnection? _connection;

        public ConnectionProvider(ILogger<ConnectionProvider> logger, IConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection == null) return;

            try
            {
                if (_connection.IsOpen)
                {
                    _logger.LogDebug("Closing the connection");
                    await _connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot close RabbitMq connection");
            }
            finally
            {
                // Always dispose, even if the connection was already closed or faulted.
                await _connection.DisposeAsync();
            }
        }

        // Implemented alongside IAsyncDisposable so the DI container can dispose this
        // singleton whether it is torn down synchronously or asynchronously.
        public void Dispose()
        {
            try
            {
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot dispose RabbitMq channel or connection");
            }
        }

        public async Task<IConnection?> GetConnectionAsync()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _logger.LogDebug("Open RabbitMQ connection");
                try
                {
                    _connection = await _connectionFactory.CreateConnectionAsync();
                }
                catch (Exception ex)
                {
                    var ExceptionMessage = ex.Message;
                    _logger.LogError(ex, "ConnectionProvider - Exception: {ConnectionProvider}", ExceptionMessage);
                }
            }

            return _connection;
        }
    }
}
