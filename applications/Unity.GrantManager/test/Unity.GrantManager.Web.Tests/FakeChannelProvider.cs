using System;
using System.Threading.Tasks;
using NSubstitute;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.GrantManager
{
    /// <summary>
    /// Test double for <see cref="IChannelProvider"/> that hands out a substitute
    /// <see cref="IChannel"/>, so the app can boot in tests without a real RabbitMQ broker.
    /// The substitute auto-returns completed tasks for the async channel operations.
    /// </summary>
    public class FakeChannelProvider : IChannelProvider
    {
        private bool _disposed;

        public Task<IChannel?> GetChannelAsync()
        {
            var channel = Substitute.For<IChannel>();
            channel.IsOpen.Returns(true);
            return Task.FromResult<IChannel?>(channel);
        }

        public void ReturnChannel(IChannel channel) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // No unmanaged resources to release; this fake exists only for tests.
            _disposed = true;
        }
    }
}
